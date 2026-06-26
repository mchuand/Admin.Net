namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 委托规则分页查询
/// </summary>
public class DelegationInput : BasePageInput
{
    /// <summary>
    /// 委托人Id
    /// </summary>
    public long? DelegatorId { get; set; }
}

/// <summary>
/// 添加委托规则输入
/// </summary>
public class AddDelegationInput
{
    /// <summary>
    /// 被委托人Id
    /// </summary>
    [Required(ErrorMessage = "被委托人不能为空")]
    public long TargetUserId { get; set; }

    /// <summary>
    /// 流程定义Id(为空表示所有流程)
    /// </summary>
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [Required(ErrorMessage = "开始时间不能为空")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [Required(ErrorMessage = "结束时间不能为空")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(256)]
    public string? Remark { get; set; }
}

/// <summary>
/// 更新委托规则输入
/// </summary>
public class UpdateDelegationInput : AddDelegationInput
{
    /// <summary>
    /// 主键Id
    /// </summary>
    [Required(ErrorMessage = "主键Id不能为空")]
    public long Id { get; set; }
}

/// <summary>
/// 委托规则输出
/// </summary>
public class DelegationOutput
{
    public long Id { get; set; }
    public long DelegatorId { get; set; }
    public string? DelegatorName { get; set; }
    public long TargetUserId { get; set; }
    public string? TargetUserName { get; set; }
    public long? WorkflowId { get; set; }
    public string? WorkflowName { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Status { get; set; }
    public string? Remark { get; set; }
    public DateTime? CreateTime { get; set; }
}
