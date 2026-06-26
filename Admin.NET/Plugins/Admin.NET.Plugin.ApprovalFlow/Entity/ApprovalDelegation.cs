namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 审批委托表(用户身后)
/// </summary>
[SugarTable(null, "审批委托表")]
public class ApprovalDelegation : EntityBase
{
    /// <summary>
    /// 委托人Id
    /// </summary>
    [SugarColumn(ColumnDescription = "委托人Id")]
    public long DelegatorId { get; set; }

    /// <summary>
    /// 委托人姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "委托人姓名", Length = 32)]
    public string? DelegatorName { get; set; }

    /// <summary>
    /// 被委托人Id
    /// </summary>
    [SugarColumn(ColumnDescription = "被委托人Id")]
    public long TargetUserId { get; set; }

    /// <summary>
    /// 被委托人姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "被委托人姓名", Length = 32)]
    public string? TargetUserName { get; set; }

    /// <summary>
    /// 流程定义Id(为空表示所有流程)
    /// </summary>
    [SugarColumn(ColumnDescription = "流程定义Id")]
    public long? WorkflowId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [SugarColumn(ColumnDescription = "开始时间")]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [SugarColumn(ColumnDescription = "结束时间")]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 状态(0禁用 1启用)
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public int Status { get; set; } = 1;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 256)]
    [MaxLength(256)]
    public string? Remark { get; set; }
}