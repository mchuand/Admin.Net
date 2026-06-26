using System.Collections.Generic;

namespace Admin.NET.Core;

/// <summary>
/// 分页高级查询输入参数
/// 继承自 BasePageInput，包含高级查询条件列表
/// </summary>
public class PageAdvancedInput : BasePageInput
{
    /// <summary>
    /// 关键字
    /// 用于模糊搜索的关键字，可选
    /// </summary>
    public new string Keyword { get; set; }

    /// <summary>
    /// 关键字匹配的字段列表
    /// 当设置了此字段时，会自动添加关键字模糊匹配条件
    /// </summary>
    public List<string> KeywordFields { get; set; }

    /// <summary>
    /// 高级查询条件列表
    /// 多个条件之间为 AND 关系
    /// </summary>
    public List<QueryConditionItem> Conditions { get; set; } = new();

    /// <summary>
    /// 排序字段
    /// 要排序的字段名，可选
    /// </summary>
    public string OrderField { get; set; }

    /// <summary>
    /// 排序方式
    /// desc：降序，asc：升序，默认降序
    /// </summary>
    public string OrderType { get; set; } = "desc";
}
