// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

using Admin.NET.Core.Service;
using Microsoft.AspNetCore.Http;
using Furion.DatabaseAccessor;
using Furion.FriendlyException;
using Mapster;
using SqlSugar;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Admin.NET.Application.Entity;
namespace Admin.NET.Application;

/// <summary>
/// 物料信息服务 🧩
/// </summary>
[ApiDescriptionSettings(ApplicationConst.GroupName, Order = 100)]
public class MaterielService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<Materiel> _materielRep;
    private readonly ISqlSugarClient _sqlSugarClient;
    private readonly SysDictTypeService _sysDictTypeService;

    public MaterielService(SqlSugarRepository<Materiel> materielRep, ISqlSugarClient sqlSugarClient, SysDictTypeService sysDictTypeService)
    {
        _materielRep = materielRep;
        _sqlSugarClient = sqlSugarClient;
        _sysDictTypeService = sysDictTypeService;
    }

    /// <summary>
    /// 分页查询物料信息 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("分页查询物料信息")]
    [ApiDescriptionSettings(Name = "Page"), HttpPost]
    public async Task<SqlSugarPagedList<MaterielOutput>> Page(PageMaterielInput input)
    {
        input.Keyword = input.Keyword?.Trim();
        var query = _materielRep.AsQueryable()
            .WhereIF(!string.IsNullOrWhiteSpace(input.Keyword), u => u.Name.Contains(input.Keyword) || u.FullName.Contains(input.Keyword) || u.Spec.Contains(input.Keyword) || u.UNIT.Contains(input.Keyword) || u.Enabler.Contains(input.Keyword) || u.Remark.Contains(input.Keyword))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name), u => u.Name.Contains(input.Name.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.FullName), u => u.FullName.Contains(input.FullName.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Spec), u => u.Spec.Contains(input.Spec.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.UNIT), u => u.UNIT.Contains(input.UNIT.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Enabler), u => u.Enabler.Contains(input.Enabler.Trim()))
            .WhereIF(!string.IsNullOrWhiteSpace(input.Remark), u => u.Remark.Contains(input.Remark.Trim()))
            .WhereIF(input.Type != null, u => u.Type == input.Type)
            .WhereIF(input.EnablerId != null, u => u.EnablerId == input.EnablerId)
            .WhereIF(input.EnableTimeRange?.Length == 2, u => u.EnableTime >= input.EnableTimeRange[0] && u.EnableTime <= input.EnableTimeRange[1])
            .LeftJoin<SysUser>((u, enabler) => u.EnablerId == enabler.Id)
            .Select((u, enabler) => new MaterielOutput
            {
                Id = u.Id,
                CreateTime = u.CreateTime,
                UpdateTime = u.UpdateTime,
                CreateUserId = u.CreateUserId,
                CreateUserName = u.CreateUserName,
                UpdateUserId = u.UpdateUserId,
                UpdateUserName = u.UpdateUserName,
                TenantId = u.TenantId,
                Type = u.Type,
                Name = u.Name,
                FullName = u.FullName,
                Spec = u.Spec,
                UNIT = u.UNIT,
                IsEnable = u.IsEnable,
                EnablerId = u.EnablerId,
                EnablerFkDisplayName = $"{enabler.Account}-{enabler.RealName}",
                Enabler = u.Enabler,
                EnableTime = u.EnableTime,
                Remark = u.Remark,
            });
		return await query.OrderBuilder(input).ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取物料信息详情 ℹ️
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("获取物料信息详情")]
    [ApiDescriptionSettings(Name = "Detail"), HttpGet]
    public async Task<Materiel> Detail([FromQuery] QueryByIdMaterielInput input)
    {
        return await _materielRep.GetFirstAsync(u => u.Id == input.Id);
    }

    /// <summary>
    /// 增加物料信息 ➕
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("增加物料信息")]
    [ApiDescriptionSettings(Name = "Add"), HttpPost]
    public async Task<long> Add(AddMaterielInput input)
    {
        var entity = input.Adapt<Materiel>();
        return await _materielRep.InsertAsync(entity) ? entity.Id : 0;
    }

    /// <summary>
    /// 更新物料信息 ✏️
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("更新物料信息")]
    [ApiDescriptionSettings(Name = "Update"), HttpPost]
    public async Task Update(UpdateMaterielInput input)
    {
        var entity = input.Adapt<Materiel>();
        await _materielRep.AsUpdateable(entity)
        .ExecuteCommandAsync();
    }

    /// <summary>
    /// 删除物料信息 ❌
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("删除物料信息")]
    [ApiDescriptionSettings(Name = "Delete"), HttpPost]
    public async Task Delete(DeleteMaterielInput input)
    {
        var entity = await _materielRep.GetFirstAsync(u => u.Id == input.Id) ?? throw Oops.Oh(ErrorCodeEnum.D1002);
        await _materielRep.FakeDeleteAsync(entity);   //假删除
        //await _materielRep.DeleteAsync(entity);   //真删除
    }

    /// <summary>
    /// 批量删除物料信息 ❌
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("批量删除物料信息")]
    [ApiDescriptionSettings(Name = "BatchDelete"), HttpPost]
    public async Task<int> BatchDelete([Required(ErrorMessage = "主键列表不能为空")]List<DeleteMaterielInput> input)
    {
        var exp = Expressionable.Create<Materiel>();
        foreach (var row in input) exp = exp.Or(it => it.Id == row.Id);
        var list = await _materielRep.AsQueryable().Where(exp.ToExpression()).ToListAsync();
   
        return await _materielRep.FakeDeleteAsync(list);   //假删除
        //return await _materielRep.DeleteAsync(list);   //真删除
    }
    
    /// <summary>
    /// 获取下拉列表数据 🔖
    /// </summary>
    /// <returns></returns>
    [DisplayName("获取下拉列表数据")]
    [ApiDescriptionSettings(Name = "DropdownData"), HttpPost]
    public async Task<Dictionary<string, dynamic>> DropdownData(DropdownDataMaterielInput input)
    {
        var enablerIdData = await _materielRep.Context.Queryable<SysUser>()
            .InnerJoinIF<Materiel>(input.FromPage, (u, r) => u.Id == r.EnablerId)
            .Select(u => new {
                Value = u.Id,
                Label = $"{u.Account}-{u.RealName}"
            }).ToListAsync();
        return new Dictionary<string, dynamic>
        {
            { "enablerId", enablerIdData },
        };
    }
    
    /// <summary>
    /// 导出物料信息记录 🔖
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    [DisplayName("导出物料信息记录")]
    [ApiDescriptionSettings(Name = "Export"), HttpPost, NonUnify]
    public async Task<IActionResult> Export(PageMaterielInput input)
    {
        var list = (await Page(input)).Items?.Adapt<List<ExportMaterielOutput>>() ?? new();
        if (input.SelectKeyList?.Count > 0) list = list.Where(x => input.SelectKeyList.Contains(x.Id)).ToList();
        var typeDictMap = _sysDictTypeService.GetDataList(new GetDataDictTypeInput { Code = "org_type" }).Result.ToDictionary(x => x.Value, x => x.Label);
        list.ForEach(e => {
            e.TypeDictLabel = typeDictMap.GetValueOrDefault(e.Type.ToString() ?? "");
        });
        return ExcelHelper.ExportTemplate(list, "物料信息导出记录");
    }
    
    /// <summary>
    /// 下载物料信息数据导入模板 ⬇️
    /// </summary>
    /// <returns></returns>
    [DisplayName("下载物料信息数据导入模板")]
    [ApiDescriptionSettings(Name = "Import"), HttpGet, NonUnify]
    public IActionResult DownloadTemplate()
    {
        return ExcelHelper.ExportTemplate(new List<ExportMaterielOutput>(), "物料信息导入模板", (_, info) =>
        {
            if (nameof(ExportMaterielOutput.EnablerFkDisplayName) == info.Name) return _materielRep.Context.Queryable<SysUser>().Select(u => $"{u.Account}-{u.RealName}").Distinct().ToList();
            return null;
        });
    }
    
    private static readonly object _materielImportLock = new object();
    /// <summary>
    /// 导入物料信息记录 💾
    /// </summary>
    /// <returns></returns>
    [DisplayName("导入物料信息记录")]
    [ApiDescriptionSettings(Name = "Import"), HttpPost, NonUnify, UnitOfWork]
    public IActionResult ImportData([Required] IFormFile file)
    {
        lock (_materielImportLock)
        {
            var typeDictMap = _sysDictTypeService.GetDataList(new GetDataDictTypeInput { Code = "org_type" }).Result.ToDictionary(x => x.Label!, x => x.Value);
            var stream = ExcelHelper.ImportData<ImportMaterielInput, Materiel>(file, (list, markerErrorAction) =>
            {
                _sqlSugarClient.Utilities.PageEach(list, 2048, pageItems =>
                {
                    // 链接 禁用人id
                    var enablerIdLabelList = pageItems.Where(x => x.EnablerFkDisplayName != null).Select(x => x.EnablerFkDisplayName).Distinct().ToList();
                    if (enablerIdLabelList.Any()) {
                        var enablerIdLinkMap = _materielRep.Context.Queryable<SysUser>().Where(u => enablerIdLabelList.Contains($"{u.Account}-{u.RealName}")).ToList().ToDictionary(u => $"{u.Account}-{u.RealName}", u => u.Id);
                        pageItems.ForEach(e => {
                            e.EnablerId = enablerIdLinkMap.GetValueOrDefault(e.EnablerFkDisplayName ?? "");
                            if (e.EnablerId == null) e.Error = "禁用人id链接失败";
                        });
                    }
                    
                    // 映射字典值
                    foreach(var item in pageItems) {
                        if (string.IsNullOrWhiteSpace(item.TypeDictLabel)) continue;
                        item.Type = typeDictMap.GetValueOrDefault(item.TypeDictLabel);
                        if (item.Type == null) item.Error = "物料类型字典映射失败";
                    }
                    
                    // 校验并过滤必填基本类型为null的字段
                    var rows = pageItems.Where(x => {
                        if (!string.IsNullOrWhiteSpace(x.Error)) return false;
                        if (x.Type == null){
                            x.Error = "物料类型不能为空";
                            return false;
                        }
                        if (!string.IsNullOrWhiteSpace(x.Error)) return false;
                        if (x.IsEnable == null){
                            x.Error = "状态（是否禁用）不能为空";
                            return false;
                        }
                        return true;
                    }).Adapt<List<Materiel>>();
                    
                    var storageable = _materielRep.Context.Storageable(rows)
                        .SplitError(it => string.IsNullOrWhiteSpace(it.Item.Name), "物料名称不能为空")
                        .SplitError(it => it.Item.Name?.Length > 32, "物料名称长度不能超过32个字符")
                        .SplitError(it => it.Item.FullName?.Length > 64, "物料全称长度不能超过64个字符")
                        .SplitError(it => string.IsNullOrWhiteSpace(it.Item.Spec), "物料规格不能为空")
                        .SplitError(it => it.Item.Spec?.Length > 32, "物料规格长度不能超过32个字符")
                        .SplitError(it => string.IsNullOrWhiteSpace(it.Item.UNIT), "单位不能为空")
                        .SplitError(it => it.Item.UNIT?.Length > 16, "单位长度不能超过16个字符")
                        .SplitError(it => it.Item.Enabler?.Length > 32, "禁用人长度不能超过32个字符")
                        .SplitError(it => it.Item.Remark?.Length > 128, "备注长度不能超过128个字符")
                        .SplitInsert(_=> true) // 没有设置唯一键代表插入所有数据
                        .ToStorage();
                    
                    storageable.AsInsertable.ExecuteCommand();// 不存在插入
                    storageable.AsUpdateable.UpdateColumns(it => new
                    {
                        it.Type,
                        it.Name,
                        it.FullName,
                        it.Spec,
                        it.UNIT,
                        it.IsEnable,
                        it.EnablerId,
                        it.Enabler,
                        it.EnableTime,
                        it.Remark,
                    }).ExecuteCommand();// 存在更新
                    
                    // 标记错误信息
                    markerErrorAction.Invoke(storageable, pageItems, rows);
                });
            });
            
            return stream;
        }
    }
}
