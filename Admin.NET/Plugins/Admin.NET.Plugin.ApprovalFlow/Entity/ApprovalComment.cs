namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 审批评论表
/// </summary>
[SugarTable(null, "审批评论表")]
public class ApprovalComment : EntityBase
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
    public string? NodeId { get; set; }

    /// <summary>
    /// 评论人Id
    /// </summary>
    [SugarColumn(ColumnDescription = "评论人Id")]
    public long UserId { get; set; }

    /// <summary>
    /// 评论人姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "评论人姓名", Length = 32)]
    public string? UserName { get; set; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [SugarColumn(ColumnDescription = "评论内容", Length = 2048)]
    [MaxLength(2048)]
    public string Content { get; set; }

    /// <summary>
    /// 附件JSON
    /// </summary>
    [SugarColumn(ColumnDescription = "附件JSON", ColumnDataType = StaticConfig.CodeFirst_BigString)]
    public string? Attachments { get; set; }
}