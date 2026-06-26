// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Admin.NET.Core;
using System.ComponentModel.DataAnnotations;
using Magicodes.ExporterAndImporter.Core;
using Magicodes.ExporterAndImporter.Excel;

namespace Admin.NET.Application;

/// <summary>
/// 物料信息基础输入参数
/// </summary>
public class MaterielBaseInput
{
    /// <summary>
    /// 主键Id
    /// </summary>
    public virtual long? Id { get; set; }
    
    /// <summary>
    /// 物料类型
    /// </summary>
    [Dict("org_type", AllowNullValue=true)]
    [Required(ErrorMessage = "物料类型不能为空")]
    public virtual int? Type { get; set; }
    
    /// <summary>
    /// 物料名称
    /// </summary>
    [Required(ErrorMessage = "物料名称不能为空")]
    public virtual string Name { get; set; }
    
    /// <summary>
    /// 物料全称
    /// </summary>
    public virtual string? FullName { get; set; }
    
    /// <summary>
    /// 物料规格
    /// </summary>
    [Required(ErrorMessage = "物料规格不能为空")]
    public virtual string Spec { get; set; }
    
    /// <summary>
    /// 单位
    /// </summary>
    [Required(ErrorMessage = "单位不能为空")]
    public virtual string UNIT { get; set; }
    
    /// <summary>
    /// 状态（是否禁用）
    /// </summary>
    [Required(ErrorMessage = "状态（是否禁用）不能为空")]
    public virtual bool? IsEnable { get; set; }
    
    /// <summary>
    /// 禁用人id
    /// </summary>
    public virtual long? EnablerId { get; set; }
    
    /// <summary>
    /// 禁用人
    /// </summary>
    public virtual string? Enabler { get; set; }
    
    /// <summary>
    /// 禁用时间
    /// </summary>
    public virtual DateTime? EnableTime { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public virtual string? Remark { get; set; }
    
}

/// <summary>
/// 物料信息分页查询输入参数
/// </summary>
public class PageMaterielInput : BasePageInput
{
    /// <summary>
    /// 物料类型
    /// </summary>
    [Dict("org_type", AllowNullValue=true)]
    public int? Type { get; set; }
    
    /// <summary>
    /// 物料名称
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// 物料全称
    /// </summary>
    public string? FullName { get; set; }
    
    /// <summary>
    /// 物料规格
    /// </summary>
    public string Spec { get; set; }
    
    /// <summary>
    /// 单位
    /// </summary>
    public string UNIT { get; set; }
    
    /// <summary>
    /// 状态（是否禁用）
    /// </summary>
    public bool? IsEnable { get; set; }
    
    /// <summary>
    /// 禁用人id
    /// </summary>
    public long? EnablerId { get; set; }
    
    /// <summary>
    /// 禁用人
    /// </summary>
    public string? Enabler { get; set; }
    
    /// <summary>
    /// 禁用时间范围
    /// </summary>
     public DateTime?[] EnableTimeRange { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
    
    /// <summary>
    /// 选中主键列表
    /// </summary>
     public List<long> SelectKeyList { get; set; }
}

/// <summary>
/// 物料信息增加输入参数
/// </summary>
public class AddMaterielInput
{
    /// <summary>
    /// 物料类型
    /// </summary>
    [Dict("org_type", AllowNullValue=true)]
    [Required(ErrorMessage = "物料类型不能为空")]
    public int? Type { get; set; }
    
    /// <summary>
    /// 物料名称
    /// </summary>
    [Required(ErrorMessage = "物料名称不能为空")]
    [MaxLength(32, ErrorMessage = "物料名称字符长度不能超过32")]
    public string Name { get; set; }
    
    /// <summary>
    /// 物料全称
    /// </summary>
    [MaxLength(64, ErrorMessage = "物料全称字符长度不能超过64")]
    public string? FullName { get; set; }
    
    /// <summary>
    /// 物料规格
    /// </summary>
    [Required(ErrorMessage = "物料规格不能为空")]
    [MaxLength(32, ErrorMessage = "物料规格字符长度不能超过32")]
    public string Spec { get; set; }
    
    /// <summary>
    /// 单位
    /// </summary>
    [Required(ErrorMessage = "单位不能为空")]
    [MaxLength(16, ErrorMessage = "单位字符长度不能超过16")]
    public string UNIT { get; set; }
    
    /// <summary>
    /// 状态（是否禁用）
    /// </summary>
    [Required(ErrorMessage = "状态（是否禁用）不能为空")]
    public bool? IsEnable { get; set; }
    
    /// <summary>
    /// 禁用人id
    /// </summary>
    public long? EnablerId { get; set; }
    
    /// <summary>
    /// 禁用人
    /// </summary>
    [MaxLength(32, ErrorMessage = "禁用人字符长度不能超过32")]
    public string? Enabler { get; set; }
    
    /// <summary>
    /// 禁用时间
    /// </summary>
    public DateTime? EnableTime { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    [MaxLength(128, ErrorMessage = "备注字符长度不能超过128")]
    public string? Remark { get; set; }
    
}

/// <summary>
/// 物料信息删除输入参数
/// </summary>
public class DeleteMaterielInput
{
    /// <summary>
    /// 主键Id
    /// </summary>
    [Required(ErrorMessage = "主键Id不能为空")]
    public long? Id { get; set; }
    
}

/// <summary>
/// 物料信息更新输入参数
/// </summary>
public class UpdateMaterielInput
{
    /// <summary>
    /// 主键Id
    /// </summary>    
    [Required(ErrorMessage = "主键Id不能为空")]
    public long? Id { get; set; }
    
    /// <summary>
    /// 物料类型
    /// </summary>    
    [Dict("org_type", AllowNullValue=true)]
    [Required(ErrorMessage = "物料类型不能为空")]
    public int? Type { get; set; }
    
    /// <summary>
    /// 物料名称
    /// </summary>    
    [Required(ErrorMessage = "物料名称不能为空")]
    [MaxLength(32, ErrorMessage = "物料名称字符长度不能超过32")]
    public string Name { get; set; }
    
    /// <summary>
    /// 物料全称
    /// </summary>    
    [MaxLength(64, ErrorMessage = "物料全称字符长度不能超过64")]
    public string? FullName { get; set; }
    
    /// <summary>
    /// 物料规格
    /// </summary>    
    [Required(ErrorMessage = "物料规格不能为空")]
    [MaxLength(32, ErrorMessage = "物料规格字符长度不能超过32")]
    public string Spec { get; set; }
    
    /// <summary>
    /// 单位
    /// </summary>    
    [Required(ErrorMessage = "单位不能为空")]
    [MaxLength(16, ErrorMessage = "单位字符长度不能超过16")]
    public string UNIT { get; set; }
    
    /// <summary>
    /// 状态（是否禁用）
    /// </summary>    
    [Required(ErrorMessage = "状态（是否禁用）不能为空")]
    public bool? IsEnable { get; set; }
    
    /// <summary>
    /// 禁用人id
    /// </summary>    
    public long? EnablerId { get; set; }
    
    /// <summary>
    /// 禁用人
    /// </summary>    
    [MaxLength(32, ErrorMessage = "禁用人字符长度不能超过32")]
    public string? Enabler { get; set; }
    
    /// <summary>
    /// 禁用时间
    /// </summary>    
    public DateTime? EnableTime { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>    
    [MaxLength(128, ErrorMessage = "备注字符长度不能超过128")]
    public string? Remark { get; set; }
    
}

/// <summary>
/// 物料信息主键查询输入参数
/// </summary>
public class QueryByIdMaterielInput : DeleteMaterielInput
{
}

/// <summary>
/// 下拉数据输入参数
/// </summary>
public class DropdownDataMaterielInput
{
    /// <summary>
    /// 是否用于分页查询
    /// </summary>
    public bool FromPage { get; set; }
}

/// <summary>
/// 物料信息数据导入实体
/// </summary>
[ExcelImporter(SheetIndex = 1, IsOnlyErrorRows = true)]
public class ImportMaterielInput : BaseImportInput
{
    /// <summary>
    /// 物料类型 关联值
    /// </summary>
    [ImporterHeader(IsIgnore = true)]
    [ExporterHeader(IsIgnore = true)]
    public int? Type { get; set; }
    
    /// <summary>
    /// 物料类型 文本
    /// </summary>
    [Dict("org_type")]
    [ImporterHeader(Name = "*物料类型")]
    [ExporterHeader("*物料类型", Format = "", Width = 25, IsBold = true)]
    public string TypeDictLabel { get; set; }
    
    /// <summary>
    /// 物料名称
    /// </summary>
    [ImporterHeader(Name = "*物料名称")]
    [ExporterHeader("*物料名称", Format = "", Width = 25, IsBold = true)]
    public string Name { get; set; }
    
    /// <summary>
    /// 物料全称
    /// </summary>
    [ImporterHeader(Name = "物料全称")]
    [ExporterHeader("物料全称", Format = "", Width = 25, IsBold = true)]
    public string? FullName { get; set; }
    
    /// <summary>
    /// 物料规格
    /// </summary>
    [ImporterHeader(Name = "*物料规格")]
    [ExporterHeader("*物料规格", Format = "", Width = 25, IsBold = true)]
    public string Spec { get; set; }
    
    /// <summary>
    /// 单位
    /// </summary>
    [ImporterHeader(Name = "*单位")]
    [ExporterHeader("*单位", Format = "", Width = 25, IsBold = true)]
    public string UNIT { get; set; }
    
    /// <summary>
    /// 状态（是否禁用）
    /// </summary>
    [ImporterHeader(Name = "*状态（是否禁用）")]
    [ExporterHeader("*状态（是否禁用）", Format = "", Width = 25, IsBold = true)]
    public bool? IsEnable { get; set; }
    
    /// <summary>
    /// 禁用人id 关联值
    /// </summary>
    [ImporterHeader(IsIgnore = true)]
    [ExporterHeader(IsIgnore = true)]
    public long? EnablerId { get; set; }
    
    /// <summary>
    /// 禁用人id 文本
    /// </summary>
    [ImporterHeader(Name = "禁用人id")]
    [ExporterHeader("禁用人id", Format = "", Width = 25, IsBold = true)]
    public string EnablerFkDisplayName { get; set; }
    
    /// <summary>
    /// 禁用人
    /// </summary>
    [ImporterHeader(Name = "禁用人")]
    [ExporterHeader("禁用人", Format = "", Width = 25, IsBold = true)]
    public string? Enabler { get; set; }
    
    /// <summary>
    /// 禁用时间
    /// </summary>
    [ImporterHeader(Name = "禁用时间")]
    [ExporterHeader("禁用时间", Format = "", Width = 25, IsBold = true)]
    public DateTime? EnableTime { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    [ImporterHeader(Name = "备注")]
    [ExporterHeader("备注", Format = "", Width = 25, IsBold = true)]
    public string? Remark { get; set; }
    
}
