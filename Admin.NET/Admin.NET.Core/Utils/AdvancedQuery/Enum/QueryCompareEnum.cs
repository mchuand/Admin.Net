
namespace Admin.NET.Core;

/// <summary>
/// 查询比对方式枚举
/// 定义高级查询支持的所有比对操作类型
/// </summary>
[Description("查询比对方式")]
public enum QueryCompareEnum
{
    /// <summary>
    /// 等于（=）
    /// </summary>
    [Description("等于")]
    Eq = 0,

    /// <summary>
    /// 不等于（&lt;&gt;）
    /// </summary>
    [Description("不等于")]
    Ne = 1,

    /// <summary>
    /// 小于（&lt;）
    /// </summary>
    [Description("小于")]
    Lt = 2,

    /// <summary>
    /// 小于等于（&lt;=）
    /// </summary>
    [Description("小于等于")]
    Le = 3,

    /// <summary>
    /// 大于（&gt;）
    /// </summary>
    [Description("大于")]
    Gt = 4,

    /// <summary>
    /// 大于等于（&gt;=）
    /// </summary>
    [Description("大于等于")]
    Ge = 5,

    /// <summary>
    /// 模糊匹配（LIKE %value%）
    /// </summary>
    [Description("模糊匹配")]
    Like = 6,

    /// <summary>
    /// 不匹配（NOT LIKE %value%）
    /// </summary>
    [Description("不匹配")]
    NotLike = 7,

    /// <summary>
    /// 在范围内（IN）
    /// 值为数组类型
    /// </summary>
    [Description("在范围内")]
    In = 8,

    /// <summary>
    /// 不在范围内（NOT IN）
    /// 值为数组类型
    /// </summary>
    [Description("不在范围内")]
    NotIn = 9,

    /// <summary>
    /// 在...之间（BETWEEN）
    /// 值为包含两个元素的数组：[最小值, 最大值]
    /// </summary>
    [Description("在...之间")]
    Between = 10,

    /// <summary>
    /// 为空（IS NULL）
    /// 不需要传入 Value 值
    /// </summary>
    [Description("为空")]
    IsNull = 11,

    /// <summary>
    /// 不为空（IS NOT NULL）
    /// 不需要传入 Value 值
    /// </summary>
    [Description("不为空")]
    IsNotNull = 12
}
