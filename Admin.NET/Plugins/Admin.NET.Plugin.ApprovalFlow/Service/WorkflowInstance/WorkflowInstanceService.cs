using System.Text.Json;

namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 审批流程实例服务
/// </summary>
[ApiDescriptionSettings(ApprovalFlowConst.GroupName, Order = 101, Description = "审批流程实例")]
public class WorkflowInstanceService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<WorkflowInstance> _instanceRep;
    private readonly SqlSugarRepository<ApprovalFlow> _flowRep;
    private readonly SqlSugarRepository<ApprovalTask> _taskRep;
    private readonly SqlSugarRepository<ApprovalComment> _commentRep;
    private readonly WorkflowEngineService _engineService;
    private readonly ApprovalNotificationService _notificationService;
    private readonly UserManager _userManager;

    public WorkflowInstanceService(
        SqlSugarRepository<WorkflowInstance> instanceRep,
        SqlSugarRepository<ApprovalFlow> flowRep,
        SqlSugarRepository<ApprovalTask> taskRep,
        SqlSugarRepository<ApprovalComment> commentRep,
        WorkflowEngineService engineService,
        ApprovalNotificationService notificationService,
        UserManager userManager)
    {
        _instanceRep = instanceRep;
        _flowRep = flowRep;
        _taskRep = taskRep;
        _commentRep = commentRep;
        _engineService = engineService;
        _notificationService = notificationService;
        _userManager = userManager;
    }

    /// <summary>
    /// 发起审批
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Start")]
    public async Task<long> Start(StartWorkflowInput input)
    {
        var flow = await _flowRep.GetByIdAsync(input.WorkflowId)
            ?? throw Oops.Oh("审批流程不存在");

        if (string.IsNullOrWhiteSpace(flow.FlowJson))
            throw Oops.Oh("请先设计审批流程");

        // 创建流程实例
        var instance = new WorkflowInstance
        {
            WorkflowId = input.WorkflowId,
            Title = input.Title,
            BizId = input.BizId,
            BizType = input.BizType,
            FormData = input.FormData,
            Status = ApprovalStatusEnum.Processing,
            FlowSnapshot = flow.FlowJson,
            Remark = input.Remark
        };
        await _instanceRep.InsertAsync(instance);

        // 启动流程引擎
        await _engineService.StartInstance(instance, flow);

        return instance.Id;
    }

    /// <summary>
    /// 处理审批操作
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "HandleAction")]
    public async Task HandleAction(ApprovalActionInput input)
    {
        var task = await _taskRep.GetByIdAsync(input.TaskId)
            ?? throw Oops.Oh("审批任务不存在");

        if (task.Status != TaskStatusEnum.Pending)
            throw Oops.Oh("该任务已处理");

        var instance = await _instanceRep.GetByIdAsync(task.InstanceId)
            ?? throw Oops.Oh("流程实例不存在");

        if (instance.Status != ApprovalStatusEnum.Processing)
            throw Oops.Oh("该流程不在审批中");

        // 执行审批操作
        await _engineService.HandleApproval(instance, task, input);
    }

    /// <summary>
    /// 撤回审批
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Withdraw")]
    public async Task Withdraw(WithdrawInput input)
    {
        var instance = await _instanceRep.GetByIdAsync(input.InstanceId)
            ?? throw Oops.Oh("流程实例不存在");

        if (instance.Status != ApprovalStatusEnum.Processing)
            throw Oops.Oh("该流程不在审批中");

        // 只有发起人可以撤回
        var userId = _userManager.UserId;
        if (instance.CreateUserId != userId)
            throw Oops.Oh("只有发起人可以撤回");

        // 检查是否有已处理的任务
        var hasHandled = await _taskRep.AsQueryable()
            .Where(t => t.InstanceId == instance.Id && t.Status == TaskStatusEnum.Completed)
            .AnyAsync();
        if (hasHandled)
            throw Oops.Oh("已有审批人处理，无法撤回");

        // 撤回流程
        instance.Status = ApprovalStatusEnum.Withdrawn;
        instance.FinishTime = DateTime.Now;
        await _instanceRep.UpdateAsync(instance);

        // 删除待办任务
        await _taskRep.AsUpdateable()
            .SetColumns(t => t.Status == TaskStatusEnum.Returned)
            .Where(t => t.InstanceId == instance.Id && t.Status == TaskStatusEnum.Pending)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取流程实例详情
    /// </summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "GetDetail")]
    public async Task<WorkflowInstanceOutput> GetDetail([FromQuery] long id)
    {
        var instance = await _instanceRep.GetByIdAsync(id)
            ?? throw Oops.Oh("流程实例不存在");

        var output = instance.Adapt<WorkflowInstanceOutput>();
        var flow = await _flowRep.GetByIdAsync(instance.WorkflowId);
        output.WorkflowName = flow?.Name;
        return output;
    }

    /// <summary>
    /// 获取审批时间线
    /// </summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "GetTimeline")]
    public async Task<List<ApprovalTimelineOutput>> GetTimeline([FromQuery] long instanceId)
    {
        var tasks = await _taskRep.AsQueryable()
            .Where(t => t.InstanceId == instanceId)
            .OrderBy(t => t.CreateTime)
            .ToListAsync();

        var timeline = tasks.Select(t => new ApprovalTimelineOutput
        {
            NodeName = t.NodeName,
            OperatorName = t.DelegatorId.HasValue ? $"{t.DelegatorName}(代{t.ApproverName})" : t.ApproverName,
            Action = t.Action,
            Comment = t.Comment,
            OperateTime = t.HandleTime ?? t.CreateTime,
            NodeType = "approval"
        }).ToList();

        // 添加发起节点
        var instance = await _instanceRep.GetByIdAsync(instanceId);
        if (instance != null)
        {
            timeline.Insert(0, new ApprovalTimelineOutput
            {
                NodeName = "发起审批",
                OperatorName = instance.CreateUserName,
                Action = ApprovalActionEnum.Submit,
                OperateTime = instance.CreateTime,
                NodeType = "start"
            });
        }

        return timeline;
    }

    /// <summary>
    /// 分页查询流程实例
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<WorkflowInstanceOutput>> Page(WorkflowInstanceInput input)
    {
        return await _instanceRep.AsQueryable()
            .LeftJoin<ApprovalFlow>((i, f) => i.WorkflowId == f.Id)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Title), (i, f) => i.Title.Contains(input.Title))
            .WhereIF(input.WorkflowId.HasValue, (i, f) => i.WorkflowId == input.WorkflowId)
            .WhereIF(input.Status.HasValue, (i, f) => i.Status == input.Status)
            .OrderByDescending((i, f) => i.CreateTime)
            .Select((i, f) => new WorkflowInstanceOutput
            {
                Id = i.Id,
                WorkflowId = i.WorkflowId,
                WorkflowName = f.Name,
                Title = i.Title,
                BizId = i.BizId,
                BizType = i.BizType,
                CurrentNodeId = i.CurrentNodeId,
                CurrentNodeName = i.CurrentNodeName,
                Status = i.Status,
                FinishTime = i.FinishTime,
                Remark = i.Remark,
                CreateTime = i.CreateTime,
                CreateUserId = i.CreateUserId,
                CreateUserName = i.CreateUserName
            })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取我的待办列表
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "MyTodo")]
    public async Task<SqlSugarPagedList<ApprovalTaskOutput>> MyTodo(MyTodoInput input)
    {
        var userId = _userManager.UserId;
        var now = DateTime.Now;

        // 查询有效的委托
        var delegations = await App.GetRequiredService<SqlSugarRepository<ApprovalDelegation>>().AsQueryable()
            .Where(d => d.DelegatorId == userId && d.Status == 1 && d.StartTime <= now && d.EndTime >= now)
            .ToListAsync();

        var delegatorIds = delegations.Select(d => d.TargetUserId).ToList();

        return await _taskRep.AsQueryable()
            .LeftJoin<WorkflowInstance>((t, i) => t.InstanceId == i.Id)
            .LeftJoin<ApprovalFlow>((t, i, f) => i.WorkflowId == f.Id)
            .Where((t, i) => t.Status == TaskStatusEnum.Pending)
            .Where((t, i) => t.ApproverId == userId || delegatorIds.Contains(t.ApproverId))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Title), (t, i) => i.Title.Contains(input.Title))
            .OrderByDescending((t, i) => t.CreateTime)
            .Select((t, i, f) => new ApprovalTaskOutput
            {
                Id = t.Id,
                InstanceId = t.InstanceId,
                InstanceTitle = i.Title,
                NodeId = t.NodeId,
                NodeName = t.NodeName,
                ApproverId = t.ApproverId,
                ApproverName = t.ApproverName,
                DelegatorId = t.DelegatorId,
                DelegatorName = t.DelegatorName,
                Status = t.Status,
                Action = t.Action,
                Comment = t.Comment,
                HandleTime = t.HandleTime,
                DueTime = t.DueTime,
                CreateTime = t.CreateTime,
                StarterName = i.CreateUserName,
                WorkflowName = f.Name
            })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取我的已办列表
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "MyDone")]
    public async Task<SqlSugarPagedList<ApprovalTaskOutput>> MyDone(MyDoneInput input)
    {
        var userId = _userManager.UserId;

        return await _taskRep.AsQueryable()
            .LeftJoin<WorkflowInstance>((t, i) => t.InstanceId == i.Id)
            .LeftJoin<ApprovalFlow>((t, i, f) => i.WorkflowId == f.Id)
            .Where((t, i) => t.Status == TaskStatusEnum.Completed && t.ApproverId == userId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Title), (t, i) => i.Title.Contains(input.Title))
            .OrderByDescending((t, i) => t.HandleTime)
            .Select((t, i, f) => new ApprovalTaskOutput
            {
                Id = t.Id,
                InstanceId = t.InstanceId,
                InstanceTitle = i.Title,
                NodeId = t.NodeId,
                NodeName = t.NodeName,
                ApproverId = t.ApproverId,
                ApproverName = t.ApproverName,
                DelegatorId = t.DelegatorId,
                DelegatorName = t.DelegatorName,
                Status = t.Status,
                Action = t.Action,
                Comment = t.Comment,
                HandleTime = t.HandleTime,
                DueTime = t.DueTime,
                CreateTime = t.CreateTime,
                StarterName = i.CreateUserName,
                WorkflowName = f.Name
            })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取我发起的列表
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "MyStart")]
    public async Task<SqlSugarPagedList<WorkflowInstanceOutput>> MyStart(MyStartInput input)
    {
        var userId = _userManager.UserId;

        return await _instanceRep.AsQueryable()
            .LeftJoin<ApprovalFlow>((i, f) => i.WorkflowId == f.Id)
            .Where((i, f) => i.CreateUserId == userId)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Title), (i, f) => i.Title.Contains(input.Title))
            .WhereIF(input.Status.HasValue, (i, f) => i.Status == input.Status)
            .OrderByDescending((i, f) => i.CreateTime)
            .Select((i, f) => new WorkflowInstanceOutput
            {
                Id = i.Id,
                WorkflowId = i.WorkflowId,
                WorkflowName = f.Name,
                Title = i.Title,
                BizId = i.BizId,
                BizType = i.BizType,
                CurrentNodeId = i.CurrentNodeId,
                CurrentNodeName = i.CurrentNodeName,
                Status = i.Status,
                FinishTime = i.FinishTime,
                Remark = i.Remark,
                CreateTime = i.CreateTime,
                CreateUserId = i.CreateUserId,
                CreateUserName = i.CreateUserName
            })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取待办数量
    /// </summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "GetTodoCount")]
    public async Task<int> GetTodoCount()
    {
        var userId = _userManager.UserId;
        return await _taskRep.AsQueryable()
            .Where(t => t.ApproverId == userId && t.Status == TaskStatusEnum.Pending)
            .CountAsync();
    }
}
