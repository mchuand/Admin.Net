namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 审批流程实例表
/// </summary>
[SugarTable(null, "审批流程实例表")]
public class WorkflowInstance : EntityBaseOrg
{
    /// <summary>
    /// 流程定义Id
    /// </summary>
    [SugarColumn(ColumnDescription = "流程定义Id")]
    public long WorkflowId { get; set; }

    /// <summary>
    /// 实例标题
    /// </summary>
    [SugarColumn(ColumnDescription = "实例标题", Length = 256)]
    [MaxLength(256)]
    public string Title { get; set; }

    /// <summary>
    /// 业务数据Id
    /// </summary>
    [SugarColumn(ColumnDescription = "业务数据Id", Length = 64)]
    public string? BizId { get; set; }

    /// <summary>
    /// 业务数据类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务数据类型", Length = 64)]
    public string? BizType { get; set; }

    /// <summary>
    /// 业务数据(JSON)
    /// </summary>
    [SugarColumn(ColumnDescription = "业务数据", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? FormData { get; set; }

    /// <summary>
    /// 当前节点Id
    /// </summary>
    [SugarColumn(ColumnDescription = "当前节点Id", Length = 64)]
    public string? CurrentNodeId { get; set; }

    /// <summary>
    /// 当前节点名称
    /// </summary>
    [SugarColumn(ColumnDescription = "当前节点名称", Length = 128)]
    public string? CurrentNodeName { get; set; }

    /// <summary>
    /// 流程状态(0草稿 1审批中 2已通过 3已驳回 4已撤回 5已终止)
    /// </summary>
    [SugarColumn(ColumnDescription = "流程状态")]
    public ApprovalStatusEnum Status { get; set; } = ApprovalStatusEnum.Draft;

    /// <summary>
    /// 流程快照JSON(存储发起时的流程定义快照)
    /// </summary>
    [SugarColumn(ColumnDescription = "流程快照", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? FlowSnapshot { get; set; }

    /// <summary>
    /// 完成时间
    /// </summary>
    [SugarColumn(ColumnDescription = "完成时间")]
    public DateTime? FinishTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 512)]
    [MaxLength(512)]
    public string? Remark { get; set; }
}