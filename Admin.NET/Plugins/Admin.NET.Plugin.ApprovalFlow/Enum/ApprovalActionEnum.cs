namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 审批操作枚举
/// </summary>
[Description("审批操作")]
public enum ApprovalActionEnum
{
    /// <summary>
    /// 提交
    /// </summary>
    [Description("提交")]
    Submit = 0,

    /// <summary>
    /// 通过
    /// </summary>
    [Description("通过")]
    Approve = 1,

    /// <summary>
    /// 驳回
    /// </summary>
    [Description("驳回")]
    Reject = 2,

    /// <summary>
    /// 退回(退回到指定节点)
    /// </summary>
    [Description("退回")]
    Return = 3,

    /// <summary>
    /// 转办
    /// </summary>
    [Description("转办")]
    Transfer = 4,

    /// <summary>
    /// 委托
    /// </summary>
    [Description("委托")]
    Delegate = 5,

    /// <summary>
    /// 加签
    /// </summary>
    [Description("加签")]
    AddSign = 6,

    /// <summary>
    /// 撤回
    /// </summary>
    [Description("撤回")]
    Withdraw = 7,

    /// <summary>
    /// 终止
    /// </summary>
    [Description("终止")]
    Terminate = 8
}