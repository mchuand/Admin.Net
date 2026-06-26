using System.Text.Json;

namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 审批流程引擎服务
/// </summary>
public class WorkflowEngineService : ITransient
{
    private readonly SqlSugarRepository<WorkflowInstance> _instanceRep;
    private readonly SqlSugarRepository<ApprovalTask> _taskRep;
    private readonly SqlSugarRepository<ApprovalFlow> _flowRep;
    private readonly SqlSugarRepository<ApprovalDelegation> _delegationRep;
    private readonly ApprovalNotificationService _notificationService;

    public WorkflowEngineService(
        SqlSugarRepository<WorkflowInstance> instanceRep,
        SqlSugarRepository<ApprovalTask> taskRep,
        SqlSugarRepository<ApprovalFlow> flowRep,
        SqlSugarRepository<ApprovalDelegation> delegationRep,
        ApprovalNotificationService notificationService)
    {
        _instanceRep = instanceRep;
        _taskRep = taskRep;
        _flowRep = flowRep;
        _delegationRep = delegationRep;
        _notificationService = notificationService;
    }

    /// <summary>
    /// 启动流程实例
    /// </summary>
    public async Task StartInstance(WorkflowInstance instance, ApprovalFlow flow)
    {
        var flowData = JsonSerializer.Deserialize<ApprovalFlowItem>(flow.FlowJson);
        if (flowData?.Nodes == null || flowData.Nodes.Count == 0)
            throw Oops.Oh("流程定义为空");

        // 找到开始节点
        var startNode = flowData.Nodes.FirstOrDefault(n => n.Type == "start");
        if (startNode == null)
            throw Oops.Oh("流程缺少开始节点");

        // 找到开始节点的下一个节点
        var nextEdge = flowData.Edges.FirstOrDefault(e => e.SourceNodeId == startNode.Id);
        if (nextEdge == null)
            throw Oops.Oh("开始节点没有连线");

        var nextNode = flowData.Nodes.FirstOrDefault(n => n.Id == nextEdge.TargetNodeId);
        if (nextNode == null)
            throw Oops.Oh("找不到下一个节点");

        // 处理下一个节点
        await ProcessNode(instance, nextNode, flowData);
    }

    /// <summary>
    /// 处理节点
    /// </summary>
    private async Task ProcessNode(WorkflowInstance instance, ApprovalFlowNodeItem node, ApprovalFlowItem flowData)
    {
        switch (node.Type)
        {
            case "approval":
            case "user":
                await ProcessApprovalNode(instance, node, flowData);
                break;
            case "gateway":
                await ProcessGatewayNode(instance, node, flowData);
                break;
            case "cc":
                await ProcessCcNode(instance, node, flowData);
                await MoveToNextNodes(instance, node, flowData);
                break;
            case "end":
                await ProcessEndNode(instance);
                break;
            default:
                // 未知节点类型，尝试移动到下一个
                await MoveToNextNodes(instance, node, flowData);
                break;
        }
    }

    /// <summary>
    /// 处理审批节点
    /// </summary>
    private async Task ProcessApprovalNode(WorkflowInstance instance, ApprovalFlowNodeItem node, ApprovalFlowItem flowData)
    {
        // 更新当前节点
        instance.CurrentNodeId = node.Id;
        instance.CurrentNodeName = node.Text?.Value ?? "审批节点";
        await _instanceRep.UpdateAsync(instance);

        // 获取审批人列表
        var approverIds = GetApproverIds(node);
        if (approverIds.Count == 0)
            throw Oops.Oh($"审批节点 [{node.Text?.Value}] 未配置审批人");

        var now = DateTime.Now;

        // 检查委托关系
        var delegations = await _delegationRep.AsQueryable()
            .Where(d => d.Status == 1 && d.StartTime <= now && d.EndTime >= now)
            .ToListAsync();

        // 为每个审批人创建任务
        foreach (var approverId in approverIds)
        {
            var task = new ApprovalTask
            {
                InstanceId = instance.Id,
                NodeId = node.Id,
                NodeName = node.Text?.Value ?? "审批节点",
                ApproverId = approverId,
                ApproverName = await GetUserName(approverId),
                Status = TaskStatusEnum.Pending
            };

            // 检查是否有委托
            var delegation = delegations.FirstOrDefault(d => d.DelegatorId == approverId);
            if (delegation != null)
            {
                task.ApproverId = delegation.TargetUserId;
                task.ApproverName = await GetUserName(delegation.TargetUserId);
                task.DelegatorId = approverId;
                task.DelegatorName = delegation.DelegatorName;
            }

            await _taskRep.InsertAsync(task);

            // 发送待办通知
            await _notificationService.SendTodoNotification(task, instance.Title);
        }
    }

    /// <summary>
    /// 处理网关节点(条件路由)
    /// </summary>
    private async Task ProcessGatewayNode(WorkflowInstance instance, ApprovalFlowNodeItem node, ApprovalFlowItem flowData)
    {
        // 获取所有出口边
        var outEdges = flowData.Edges.Where(e => e.SourceNodeId == node.Id).ToList();
        if (outEdges.Count == 0)
            throw Oops.Oh("网关节点没有出口");

        // 默认走第一个出口(简化实现，后续可扩展条件表达式)
        var nextEdge = outEdges.First();
        var nextNode = flowData.Nodes.FirstOrDefault(n => n.Id == nextEdge.TargetNodeId);
        if (nextNode != null)
        {
            await ProcessNode(instance, nextNode, flowData);
        }
    }

    /// <summary>
    /// 处理抄送节点
    /// </summary>
    private async Task ProcessCcNode(WorkflowInstance instance, ApprovalFlowNodeItem node, ApprovalFlowItem flowData)
    {
        var ccUserIds = GetApproverIds(node);
        foreach (var userId in ccUserIds)
        {
            await _notificationService.SendCcNotification(userId, instance.Id, instance.Title);
        }
    }

    /// <summary>
    /// 处理结束节点
    /// </summary>
    private async Task ProcessEndNode(WorkflowInstance instance)
    {
        instance.Status = ApprovalStatusEnum.Approved;
        instance.CurrentNodeId = "end";
        instance.CurrentNodeName = "结束";
        instance.FinishTime = DateTime.Now;
        await _instanceRep.UpdateAsync(instance);

        // 通知发起人
        await _notificationService.SendFinishNotification(
            instance.CreateUserId ?? 0, instance.Id, instance.Title, true);
    }

    /// <summary>
    /// 处理审批操作
    /// </summary>
    public async Task HandleApproval(WorkflowInstance instance, ApprovalTask task, ApprovalActionInput input)
    {
        // 更新任务状态
        task.Status = TaskStatusEnum.Completed;
        task.Action = input.Action;
        task.Comment = input.Comment;
        task.HandleTime = DateTime.Now;

        switch (input.Action)
        {
            case ApprovalActionEnum.Approve:
                await HandleApprove(instance, task, input);
                break;
            case ApprovalActionEnum.Reject:
                await HandleReject(instance, task, input);
                break;
            case ApprovalActionEnum.Return:
                await HandleReturn(instance, task, input);
                break;
            case ApprovalActionEnum.Transfer:
                await HandleTransfer(instance, task, input);
                break;
            default:
                await _taskRep.UpdateAsync(task);
                break;
        }
    }

    /// <summary>
    /// 处理通过
    /// </summary>
    private async Task HandleApprove(WorkflowInstance instance, ApprovalTask task, ApprovalActionInput input)
    {
        await _taskRep.UpdateAsync(task);

        // 获取流程快照
        var flowData = JsonSerializer.Deserialize<ApprovalFlowItem>(instance.FlowSnapshot);
        if (flowData == null) return;

        var currentNode = flowData.Nodes.FirstOrDefault(n => n.Id == task.NodeId);
        if (currentNode == null) return;

        // 检查是否是会签模式
        var properties = currentNode.Properties;
        var isCountersign = properties?.ApprovalType == "countersign";

        if (isCountersign)
        {
            // 会签：检查同节点所有任务是否都已完成
            var pendingCount = await _taskRep.AsQueryable()
                .Where(t => t.InstanceId == instance.Id && t.NodeId == task.NodeId && t.Status == TaskStatusEnum.Pending)
                .CountAsync();

            if (pendingCount > 0) return; // 还有人未审批
        }

        // 移动到下一个节点
        await MoveToNextNodes(instance, currentNode, flowData);
    }

    /// <summary>
    /// 处理驳回
    /// </summary>
    private async Task HandleReject(WorkflowInstance instance, ApprovalTask task, ApprovalActionInput input)
    {
        await _taskRep.UpdateAsync(task);

        // 驳回到发起人
        instance.Status = ApprovalStatusEnum.Rejected;
        instance.FinishTime = DateTime.Now;
        await _instanceRep.UpdateAsync(instance);

        // 清除待办任务
        await _taskRep.AsUpdateable()
            .SetColumns(t => t.Status == TaskStatusEnum.Returned)
            .Where(t => t.InstanceId == instance.Id && t.Status == TaskStatusEnum.Pending && t.Id != task.Id)
            .ExecuteCommandAsync();

        // 通知发起人
        await _notificationService.SendFinishNotification(
            instance.CreateUserId ?? 0, instance.Id, instance.Title, false);
    }

    /// <summary>
    /// 处理退回
    /// </summary>
    private async Task HandleReturn(WorkflowInstance instance, ApprovalTask task, ApprovalActionInput input)
    {
        await _taskRep.UpdateAsync(task);

        if (string.IsNullOrWhiteSpace(input.TargetNodeId))
            throw Oops.Oh("退回目标节点不能为空");

        var flowData = JsonSerializer.Deserialize<ApprovalFlowItem>(instance.FlowSnapshot);
        if (flowData == null) return;

        var targetNode = flowData.Nodes.FirstOrDefault(n => n.Id == input.TargetNodeId);
        if (targetNode == null)
            throw Oops.Oh("退回目标节点不存在");

        // 清除当前节点的待办任务
        await _taskRep.AsUpdateable()
            .SetColumns(t => t.Status == TaskStatusEnum.Returned)
            .Where(t => t.InstanceId == instance.Id && t.NodeId == task.NodeId && t.Status == TaskStatusEnum.Pending)
            .ExecuteCommandAsync();

        // 处理目标节点
        await ProcessNode(instance, targetNode, flowData);
    }

    /// <summary>
    /// 处理转办
    /// </summary>
    private async Task HandleTransfer(WorkflowInstance instance, ApprovalTask task, ApprovalActionInput input)
    {
        if (!input.TargetUserId.HasValue)
            throw Oops.Oh("转办目标用户不能为空");

        // 更新当前任务为已转办
        task.Status = TaskStatusEnum.Transferred;
        await _taskRep.UpdateAsync(task);

        // 创建新任务给目标用户
        var newTask = new ApprovalTask
        {
            InstanceId = instance.Id,
            NodeId = task.NodeId,
            NodeName = task.NodeName,
            ApproverId = input.TargetUserId.Value,
            ApproverName = await GetUserName(input.TargetUserId.Value),
            Status = TaskStatusEnum.Pending
        };
        await _taskRep.InsertAsync(newTask);

        // 发送待办通知
        await _notificationService.SendTodoNotification(newTask, instance.Title);
    }

    /// <summary>
    /// 移动到下一个节点
    /// </summary>
    private async Task MoveToNextNodes(WorkflowInstance instance, ApprovalFlowNodeItem currentNode, ApprovalFlowItem flowData)
    {
        var nextEdges = flowData.Edges.Where(e => e.SourceNodeId == currentNode.Id).ToList();
        if (nextEdges.Count == 0)
        {
            // 没有下一个节点，流程结束
            await ProcessEndNode(instance);
            return;
        }

        foreach (var edge in nextEdges)
        {
            var nextNode = flowData.Nodes.FirstOrDefault(n => n.Id == edge.TargetNodeId);
            if (nextNode != null)
            {
                await ProcessNode(instance, nextNode, flowData);
            }
        }
    }

    /// <summary>
    /// 获取审批人Id列表
    /// </summary>
    private List<long> GetApproverIds(ApprovalFlowNodeItem node)
    {
        var ids = new List<long>();
        if (node.Properties?.ApproverIds != null)
        {
            ids.AddRange(node.Properties.ApproverIds);
        }
        return ids;
    }

    /// <summary>
    /// 获取用户姓名
    /// </summary>
    private async Task<string> GetUserName(long userId)
    {
        var user = await App.GetRequiredService<SqlSugarRepository<SysUser>>().GetByIdAsync(userId);
        return user?.RealName ?? user?.NickName ?? "未知用户";
    }
}
