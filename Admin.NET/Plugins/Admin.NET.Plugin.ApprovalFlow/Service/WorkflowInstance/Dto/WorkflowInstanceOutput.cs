namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 流程实例输出
/// </summary>
public class WorkflowInstanceOutput
{
    public long Id { get; set; }
    public long WorkflowId { get; set; }
    public string? WorkflowName { get; set; }
    public string Title { get; set; }
    public string? BizId { get; set; }
    public string? BizType { get; set; }
    public string? FormData { get; set; }
    public string? CurrentNodeId { get; set; }
    public string? CurrentNodeName { get; set; }
    public ApprovalStatusEnum Status { get; set; }
    public string? FlowSnapshot { get; set; }
    public DateTime? FinishTime { get; set; }
    public string? Remark { get; set; }
    public DateTime? CreateTime { get; set; }
    public long? CreateUserId { get; set; }
    public string? CreateUserName { get; set; }
    public long? CreateOrgId { get; set; }
    public string? CreateOrgName { get; set; }
}

/// <summary>
/// 审批任务输出
/// </summary>
public class ApprovalTaskOutput
{
    public long Id { get; set; }
    public long InstanceId { get; set; }
    public string? InstanceTitle { get; set; }
    public string NodeId { get; set; }
    public string? NodeName { get; set; }
    public long ApproverId { get; set; }
    public string? ApproverName { get; set; }
    public long? DelegatorId { get; set; }
    public string? DelegatorName { get; set; }
    public TaskStatusEnum Status { get; set; }
    public ApprovalActionEnum? Action { get; set; }
    public string? Comment { get; set; }
    public DateTime? HandleTime { get; set; }
    public DateTime? DueTime { get; set; }
    public DateTime? CreateTime { get; set; }

    /// <summary>
    /// 发起人姓名
    /// </summary>
    public string? StarterName { get; set; }

    /// <summary>
    /// 流程定义名称
    /// </summary>
    public string? WorkflowName { get; set; }
}

/// <summary>
/// 审批记录时间线输出
/// </summary>
public class ApprovalTimelineOutput
{
    /// <summary>
    /// 节点名称
    /// </summary>
    public string? NodeName { get; set; }

    /// <summary>
    /// 操作人姓名
    /// </summary>
    public string? OperatorName { get; set; }

    /// <summary>
    /// 审批操作
    /// </summary>
    public ApprovalActionEnum? Action { get; set; }

    /// <summary>
    /// 审批意见
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    public DateTime? OperateTime { get; set; }

    /// <summary>
    /// 节点类型
    /// </summary>
    public string? NodeType { get; set; }
}
