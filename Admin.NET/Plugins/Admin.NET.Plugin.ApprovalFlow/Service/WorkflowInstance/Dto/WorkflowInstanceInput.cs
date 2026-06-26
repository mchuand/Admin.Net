namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 流程实例分页查询
/// </summary>
public class WorkflowInstanceInput : BasePageInput
{
    /// <summary>
    /// 标题
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 流程定义Id
    /// </summary>
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ApprovalStatusEnum? Status { get; set; }
}

/// <summary>
/// 发起审批输入
/// </summary>
public class StartWorkflowInput
{
    /// <summary>
    /// 流程定义Id
    /// </summary>
    [Required(ErrorMessage = "流程定义Id不能为空")]
    public long WorkflowId { get; set; }

    /// <summary>
    /// 实例标题
    /// </summary>
    [Required(ErrorMessage = "标题不能为空")]
    [MaxLength(256)]
    public string Title { get; set; }

    /// <summary>
    /// 业务数据Id
    /// </summary>
    public string? BizId { get; set; }

    /// <summary>
    /// 业务数据类型
    /// </summary>
    public string? BizType { get; set; }

    /// <summary>
    /// 表单数据(JSON)
    /// </summary>
    public string? FormData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(512)]
    public string? Remark { get; set; }
}

/// <summary>
/// 审批操作输入
/// </summary>
public class ApprovalActionInput
{
    /// <summary>
    /// 任务Id
    /// </summary>
    [Required(ErrorMessage = "任务Id不能为空")]
    public long TaskId { get; set; }

    /// <summary>
    /// 审批操作
    /// </summary>
    [Required(ErrorMessage = "审批操作不能为空")]
    public ApprovalActionEnum Action { get; set; }

    /// <summary>
    /// 审批意见
    /// </summary>
    [MaxLength(1024)]
    public string? Comment { get; set; }

    /// <summary>
    /// 转办目标用户Id(转办时必填)
    /// </summary>
    public long? TargetUserId { get; set; }

    /// <summary>
    /// 退回目标节点Id(退回时必填)
    /// </summary>
    public string? TargetNodeId { get; set; }
}

/// <summary>
/// 撤回输入
/// </summary>
public class WithdrawInput
{
    /// <summary>
    /// 流程实例Id
    /// </summary>
    [Required(ErrorMessage = "流程实例Id不能为空")]
    public long InstanceId { get; set; }
}

/// <summary>
/// 查询我的待办输入
/// </summary>
public class MyTodoInput : BasePageInput
{
    /// <summary>
    /// 标题搜索
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// 查询我的已办输入
/// </summary>
public class MyDoneInput : BasePageInput
{
    /// <summary>
    /// 标题搜索
    /// </summary>
    public string? Title { get; set; }
}

/// <summary>
/// 查询我发起的输入
/// </summary>
public class MyStartInput : BasePageInput
{
    /// <summary>
    /// 标题搜索
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public ApprovalStatusEnum? Status { get; set; }
}
