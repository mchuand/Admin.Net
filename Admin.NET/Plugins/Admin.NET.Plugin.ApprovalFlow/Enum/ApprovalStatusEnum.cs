namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 审批状态枚举
/// </summary>
[Description("审批状态")]
public enum ApprovalStatusEnum
{
    /// <summary>
    /// 草稿
    /// </summary>
    [Description("草稿")]
    Draft = 0,

    /// <summary>
    /// 审批中
    /// </summary>
    [Description("审批中")]
    Processing = 1,

    /// <summary>
    /// 已通过
    /// </summary>
    [Description("已通过")]
    Approved = 2,

    /// <summary>
    /// 已驳回
    /// </summary>
    [Description("已驳回")]
    Rejected = 3,

    /// <summary>
    /// 已撤回
    /// </summary>
    [Description("已撤回")]
    Withdrawn = 4,

    /// <summary>
    /// 已终止
    /// </summary>
    [Description("已终止")]
    Terminated = 5
}