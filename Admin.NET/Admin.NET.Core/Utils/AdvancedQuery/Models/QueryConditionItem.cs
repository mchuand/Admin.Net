using Admin.NET.Core.Utils.AdvancedQuery.Enum;

namespace Admin.NET.Core.Utils.AdvancedQuery.Models;

/// <summary>
/// 查询条件项
/// 表示单个查询条件，包含字段名、字段值和比对方式
/// </summary>
public class QueryConditionItem
{
    /// <summary>
    /// 字段名
    /// 要查询的实体属性名称，支持自动识别所属表
    /// </summary>
    public string Field { get; set; }

    /// <summary>
    /// 查询值
    /// 比对的值，支持基本类型、数组（用于 Between 和 In 查询）
    /// </summary>
    public object Value { get; set; }

    /// <summary>
    /// 比对方式
    /// 见 QueryCompareEnum 枚举定义
    /// </summary>
    public QueryCompareEnum Compare { get; set; }
}
