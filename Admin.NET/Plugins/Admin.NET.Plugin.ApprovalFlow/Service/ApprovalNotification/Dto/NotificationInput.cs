namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 通知分页查询
/// </summary>
public class NotificationInput : BasePageInput
{
    /// <summary>
    /// 是否已读
    /// </summary>
    public bool? IsRead { get; set; }

    /// <summary>
    /// 通知类型
    /// </summary>
    public string? Type { get; set; }
}

/// <summary>
/// 通知输出
/// </summary>
public class NotificationOutput
{
    public long Id { get; set; }
    public long ReceiverId { get; set; }
    public long? InstanceId { get; set; }
    public long? TaskId { get; set; }
    public string Type { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadTime { get; set; }
    public DateTime? CreateTime { get; set; }
}
