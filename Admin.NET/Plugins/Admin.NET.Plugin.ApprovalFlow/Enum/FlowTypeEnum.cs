namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 流程类型枚举
/// </summary>
[Description("流程类型")]
public enum FlowTypeEnum
{
    /// <summary>
    /// 普通审批
    /// </summary>
    [Description("普通审批")]
    Normal = 0,

    /// <summary>
    /// 会签审批
    /// </summary>
    [Description("会签审批")]
    Countersign = 1,

    /// <summary>
    /// 或签审批
    /// </summary>
    [Description("或签审批")]
    OrSign = 2
}