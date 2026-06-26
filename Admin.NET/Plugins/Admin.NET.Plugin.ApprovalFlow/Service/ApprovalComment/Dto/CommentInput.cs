namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 添加评论输入
/// </summary>
public class AddCommentInput
{
    /// <summary>
    /// 流程实例Id
    /// </summary>
    [Required(ErrorMessage = "流程实例Id不能为空")]
    public long InstanceId { get; set; }

    /// <summary>
    /// 节点Id
    /// </summary>
    public string? NodeId { get; set; }

    /// <summary>
    /// 评论内容
    /// </summary>
    [Required(ErrorMessage = "评论内容不能为空")]
    [MaxLength(2048)]
    public string Content { get; set; }

    /// <summary>
    /// 附件JSON
    /// </summary>
    public string? Attachments { get; set; }
}

/// <summary>
/// 评论输出
/// </summary>
public class CommentOutput
{
    public long Id { get; set; }
    public long InstanceId { get; set; }
    public string? NodeId { get; set; }
    public long UserId { get; set; }
    public string? UserName { get; set; }
    public string Content { get; set; }
    public string? Attachments { get; set; }
    public DateTime? CreateTime { get; set; }
}
