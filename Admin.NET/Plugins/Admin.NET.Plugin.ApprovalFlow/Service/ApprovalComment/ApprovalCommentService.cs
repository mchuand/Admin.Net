namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 审批评论服务
/// </summary>
[ApiDescriptionSettings(ApprovalFlowConst.GroupName, Order = 102, Description = "审批评论")]
public class ApprovalCommentService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<ApprovalComment> _commentRep;
    private readonly UserManager _userManager;

    public ApprovalCommentService(SqlSugarRepository<ApprovalComment> commentRep, UserManager userManager)
    {
        _commentRep = commentRep;
        _userManager = userManager;
    }

    /// <summary>
    /// 添加评论
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Add")]
    public async Task<long> Add(AddCommentInput input)
    {
        var userId = _userManager.UserId;
        var user = await App.GetRequiredService<SqlSugarRepository<SysUser>>().GetByIdAsync(userId);

        var entity = new ApprovalComment
        {
            InstanceId = input.InstanceId,
            NodeId = input.NodeId,
            UserId = userId,
            UserName = user?.RealName ?? user?.NickName,
            Content = input.Content,
            Attachments = input.Attachments
        };

        await _commentRep.InsertAsync(entity);
        return entity.Id;
    }

    /// <summary>
    /// 删除评论
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Delete")]
    public async Task Delete(BaseIdInput input)
    {
        var entity = await _commentRep.GetByIdAsync(input.Id)
            ?? throw Oops.Oh("评论不存在");

        if (entity.UserId != _userManager.UserId)
            throw Oops.Oh("只能删除自己的评论");

        await _commentRep.DeleteAsync(entity);
    }

    /// <summary>
    /// 获取实例评论列表
    /// </summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "GetList")]
    public async Task<List<CommentOutput>> GetList([FromQuery] long instanceId)
    {
        return await _commentRep.AsQueryable()
            .Where(c => c.InstanceId == instanceId)
            .OrderBy(c => c.CreateTime)
            .Select<CommentOutput>()
            .ToListAsync();
    }
}
