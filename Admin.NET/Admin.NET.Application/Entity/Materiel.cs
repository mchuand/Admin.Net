// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using System.ComponentModel.DataAnnotations;
using Admin.NET.Core;
using SqlSugar;
namespace Admin.NET.Application.Entity;

/// <summary>
/// 物料信息
/// </summary>
[Tenant("1300000000001")]
[SugarTable("materiel", "物料信息")]
public partial class Materiel : EntityBase
{
    /// <summary>
    /// 租户Id
    /// </summary>
    [SugarColumn(ColumnName = "TenantId", ColumnDescription = "租户Id")]
    public virtual long? TenantId { get; set; }
    
    /// <summary>
    /// 物料类型
    /// </summary>
    [Required]
    [SugarColumn(ColumnName = "Type", ColumnDescription = "物料类型")]
    public virtual int Type { get; set; }
    
    /// <summary>
    /// 物料名称
    /// </summary>
    [Required]
    [SugarColumn(ColumnName = "Name", ColumnDescription = "物料名称", Length = 32)]
    public virtual string Name { get; set; }
    
    /// <summary>
    /// 物料全称
    /// </summary>
    [SugarColumn(ColumnName = "FullName", ColumnDescription = "物料全称", Length = 64)]
    public virtual string? FullName { get; set; }
    
    /// <summary>
    /// 物料规格
    /// </summary>
    [Required]
    [SugarColumn(ColumnName = "Spec", ColumnDescription = "物料规格", Length = 32)]
    public virtual string Spec { get; set; }
    
    /// <summary>
    /// 单位
    /// </summary>
    [Required]
    [SugarColumn(ColumnName = "UNIT", ColumnDescription = "单位", Length = 16)]
    public virtual string UNIT { get; set; }
    
    /// <summary>
    /// 状态（是否禁用）
    /// </summary>
    [Required]
    [SugarColumn(ColumnName = "IsEnable", ColumnDescription = "状态（是否禁用）")]
    public virtual bool IsEnable { get; set; }
    
    /// <summary>
    /// 禁用人id
    /// </summary>
    [SugarColumn(ColumnName = "EnablerId", ColumnDescription = "禁用人id")]
    public virtual long? EnablerId { get; set; }
    
    /// <summary>
    /// 禁用人
    /// </summary>
    [SugarColumn(ColumnName = "Enabler", ColumnDescription = "禁用人", Length = 32)]
    public virtual string? Enabler { get; set; }
    
    /// <summary>
    /// 禁用时间
    /// </summary>
    [SugarColumn(ColumnName = "EnableTime", ColumnDescription = "禁用时间")]
    public virtual DateTime? EnableTime { get; set; }
    
    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 128)]
    public virtual string? Remark { get; set; }
    
}
