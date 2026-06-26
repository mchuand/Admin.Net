namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 审批委托服务(用户身后)
/// </summary>
[ApiDescriptionSettings(ApprovalFlowConst.DelegationGroupName, Order = 103, Description = "委托管理")]
public class ApprovalDelegationService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<ApprovalDelegation> _delegationRep;
    private readonly SqlSugarRepository<ApprovalFlow> _flowRep;
    private readonly UserManager _userManager;

    public ApprovalDelegationService(
        SqlSugarRepository<ApprovalDelegation> delegationRep,
        SqlSugarRepository<ApprovalFlow> flowRep,
        UserManager userManager)
    {
        _delegationRep = delegationRep;
        _flowRep = flowRep;
        _userManager = userManager;
    }

    /// <summary>
    /// 添加委托规则
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddDelegationInput input)
    {
        var userId = _userManager.UserId;

        var entity = new ApprovalDelegation
        {
            DelegatorId = userId,
            DelegatorName = await GetUserName(userId),
            TargetUserId = input.TargetUserId,
            TargetUserName = await GetUserName(input.TargetUserId),
            WorkflowId = input.WorkflowId,
            StartTime = input.StartTime,
            EndTime = input.EndTime,
            Status = 1,
            Remark = input.Remark
        };

        await _delegationRep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 更新委托规则
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Update")]
    public async Task Update(UpdateDelegationInput input)
    {
        var entity = await _delegationRep.GetByIdAsync(input.Id)
            ?? throw Oops.Oh("委托规则不存在");

        entity.TargetUserId = input.TargetUserId;
        entity.TargetUserName = await GetUserName(input.TargetUserId);
        entity.WorkflowId = input.WorkflowId;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.Remark = input.Remark;

        await _delegationRep.UpdateAsync(entity);
    }

    /// <summary>
    /// 删除委托规则
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(BaseIdInput input)
    {
        var entity = await _delegationRep.GetByIdAsync(input.Id)
            ?? throw Oops.Oh("委托规则不存在");

        await _delegationRep.DeleteAsync(entity);
    }

    /// <summary>
    /// 启用/禁用委托
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "ToggleStatus")]
    public async Task ToggleStatus(BaseIdInput input)
    {
        var entity = await _delegationRep.GetByIdAsync(input.Id)
            ?? throw Oops.Oh("委托规则不存在");

        entity.Status = entity.Status == 1 ? 0 : 1;
        await _delegationRep.UpdateAsync(entity);
    }

    /// <summary>
    /// 获取我的委托列表
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<DelegationOutput>> Page(DelegationInput input)
    {
        var userId = _userManager.UserId;

        return await _delegationRep.AsQueryable()
            .LeftJoin<ApprovalFlow>((d, f) => d.WorkflowId == f.Id)
            .Where((d, f) => d.DelegatorId == userId)
            .OrderByDescending((d, f) => d.CreateTime)
            .Select((d, f) => new DelegationOutput
            {
                Id = d.Id,
                DelegatorId = d.DelegatorId,
                DelegatorName = d.DelegatorName,
                TargetUserId = d.TargetUserId,
                TargetUserName = d.TargetUserName,
                WorkflowId = d.WorkflowId,
                WorkflowName = f.Name,
                StartTime = d.StartTime,
                EndTime = d.EndTime,
                Status = d.Status,
                Remark = d.Remark,
                CreateTime = d.CreateTime
            })
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 获取委托详情
    /// </summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "GetDetail")]
    public async Task<DelegationOutput> GetDetail([FromQuery] long id)
    {
        var entity = await _delegationRep.GetByIdAsync(id)
            ?? throw Oops.Oh("委托规则不存在");

        var output = entity.Adapt<DelegationOutput>();
        if (entity.WorkflowId.HasValue)
        {
            var flow = await _flowRep.GetByIdAsync(entity.WorkflowId.Value);
            output.WorkflowName = flow?.Name;
        }
        return output;
    }

    private async Task<string> GetUserName(long userId)
    {
        var user = await App.GetRequiredService<SqlSugarRepository<SysUser>>().GetByIdAsync(userId);
        return user?.RealName ?? user?.NickName ?? "未知用户";
    }
}
