namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 任务状态枚举
/// </summary>
[Description("任务状态")]
public enum TaskStatusEnum
{
    /// <summary>
    /// 待处理
    /// </summary>
    [Description("待处理")]
    Pending = 0,

    /// <summary>
    /// 已处理
    /// </summary>
    [Description("已处理")]
    Completed = 1,

    /// <summary>
    /// 已转办
    /// </summary>
    [Description("已转办")]
    Transferred = 2,

    /// <summary>
    /// 已委托
    /// </summary>
    [Description("已委托")]
    Delegated = 3,

    /// <summary>
    /// 已退回
    /// </summary>
    [Description("已退回")]
    Returned = 4
}