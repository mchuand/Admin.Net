namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 节点类型枚举
/// </summary>
[Description("节点类型")]
public enum NodeTypeEnum
{
    /// <summary>
    /// 开始节点
    /// </summary>
    [Description("开始")]
    Start = 0,

    /// <summary>
    /// 审批节点
    /// </summary>
    [Description("审批")]
    Approval = 1,

    /// <summary>
    /// 条件网关
    /// </summary>
    [Description("条件网关")]
    Gateway = 2,

    /// <summary>
    /// 抄送节点
    /// </summary>
    [Description("抄送")]
    Cc = 3,

    /// <summary>
    /// 结束节点
    /// </summary>
    [Description("结束")]
    End = 4
}