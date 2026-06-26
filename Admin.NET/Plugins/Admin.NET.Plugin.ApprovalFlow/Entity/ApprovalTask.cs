namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 审批任务表
/// </summary>
[SugarTable(null, "审批任务表")]
public class ApprovalTask : EntityBaseOrg
{
    /// <summary>
    /// 流程实例Id
    /// </summary>
    [SugarColumn(ColumnDescription = "流程实例Id")]
    public long InstanceId { get; set; }

    /// <summary>
    /// 节点Id
    /// </summary>
    [SugarColumn(ColumnDescription = "节点Id", Length = 64)]
    public string NodeId { get; set; }

    /// <summary>
    /// 节点名称
    /// </summary>
    [SugarColumn(ColumnDescription = "节点名称", Length = 128)]
    public string? NodeName { get; set; }

    /// <summary>
    /// 审批人Id
    /// </summary>
    [SugarColumn(ColumnDescription = "审批人Id")]
    public long ApproverId { get; set; }

    /// <summary>
    /// 审批人姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "审批人姓名", Length = 32)]
    public string? ApproverName { get; set; }

    /// <summary>
    /// 委托人Id(如果是委托审批)
    /// </summary>
    [SugarColumn(ColumnDescription = "委托人Id")]
    public long? DelegatorId { get; set; }

    /// <summary>
    /// 委托人姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "委托人姓名", Length = 32)]
    public string? DelegatorName { get; set; }

    /// <summary>
    /// 任务状态(0待处理 1已处理 2已转办 3已委托 4已退回)
    /// </summary>
    [SugarColumn(ColumnDescription = "任务状态")]
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Pending;

    /// <summary>
    /// 审批操作(1通过 2驳回 3退回 4转办 5委托)
    /// </summary>
    [SugarColumn(ColumnDescription = "审批操作")]
    public ApprovalActionEnum? Action { get; set; }

    /// <summary>
    /// 审批意见
    /// </summary>
    [SugarColumn(ColumnDescription = "审批意见", Length = 1024)]
    [MaxLength(1024)]
    public string? Comment { get; set; }

    /// <summary>
    /// 处理时间
    /// </summary>
    [SugarColumn(ColumnDescription = "处理时间")]
    public DateTime? HandleTime { get; set; }

    /// <summary>
    /// 到期时间
    /// </summary>
    [SugarColumn(ColumnDescription = "到期时间")]
    public DateTime? DueTime { get; set; }
}