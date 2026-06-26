namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 审批通知表
/// </summary>
[SugarTable(null, "审批通知表")]
public class ApprovalNotification : EntityBase
{
    /// <summary>
    /// 接收人Id
    /// </summary>
    [SugarColumn(ColumnDescription = "接收人Id")]
    public long ReceiverId { get; set; }

    /// <summary>
    /// 流程实例Id
    /// </summary>
    [SugarColumn(ColumnDescription = "流程实例Id")]
    public long? InstanceId { get; set; }

    /// <summary>
    /// 任务Id
    /// </summary>
    [SugarColumn(ColumnDescription = "任务Id")]
    public long? TaskId { get; set; }

    /// <summary>
    /// 通知类型(待办/已办/抄送/委托/系统)
    /// </summary>
    [SugarColumn(ColumnDescription = "通知类型", Length = 32)]
    public string Type { get; set; }

    /// <summary>
    /// 通知标题
    /// </summary>
    [SugarColumn(ColumnDescription = "通知标题", Length = 256)]
    [MaxLength(256)]
    public string Title { get; set; }

    /// <summary>
    /// 通知内容
    /// </summary>
    [SugarColumn(ColumnDescription = "通知内容", Length = 1024)]
    [MaxLength(1024)]
    public string Content { get; set; }

    /// <summary>
    /// 是否已读
    /// </summary>
    [SugarColumn(ColumnDescription = "是否已读")]
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// 读取时间
    /// </summary>
    [SugarColumn(ColumnDescription = "读取时间")]
    public DateTime? ReadTime { get; set; }
}