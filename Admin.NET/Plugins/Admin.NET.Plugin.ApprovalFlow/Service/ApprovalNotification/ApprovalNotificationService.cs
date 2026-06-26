namespace Admin.NET.Plugin.ApprovalFlow.Service;

/// <summary>
/// 审批通知服务
/// </summary>
[ApiDescriptionSettings(ApprovalFlowConst.NotificationGroupName, Order = 104, Description = "审批通知")]
public class ApprovalNotificationService : IDynamicApiController, ITransient
{
    private readonly SqlSugarRepository<ApprovalNotification> _notificationRep;
    private readonly UserManager _userManager;

    public ApprovalNotificationService(SqlSugarRepository<ApprovalNotification> notificationRep, UserManager userManager)
    {
        _notificationRep = notificationRep;
        _userManager = userManager;
    }

    /// <summary>
    /// 发送待办通知
    /// </summary>
    [NonAction]
    public async Task SendTodoNotification(ApprovalTask task, string instanceTitle)
    {
        var notification = new ApprovalNotification
        {
            ReceiverId = task.ApproverId,
            InstanceId = task.InstanceId,
            TaskId = task.Id,
            Type = "待办",
            Title = "您有一条新的审批待办",
            Content = $"请审批: {instanceTitle}",
            IsRead = false
        };
        await _notificationRep.InsertAsync(notification);

        // 通过SignalR推送实时通知
        await PushSignalRNotification(task.ApproverId, notification);
    }

    /// <summary>
    /// 发送抄送通知
    /// </summary>
    [NonAction]
    public async Task SendCcNotification(long userId, long instanceId, string instanceTitle)
    {
        var notification = new ApprovalNotification
        {
            ReceiverId = userId,
            InstanceId = instanceId,
            Type = "抄送",
            Title = "您有一条审批抄送",
            Content = $"审批单已抄送给您: {instanceTitle}",
            IsRead = false
        };
        await _notificationRep.InsertAsync(notification);

        await PushSignalRNotification(userId, notification);
    }

    /// <summary>
    /// 发送流程完成通知
    /// </summary>
    [NonAction]
    public async Task SendFinishNotification(long userId, long instanceId, string instanceTitle, bool isApproved)
    {
        var notification = new ApprovalNotification
        {
            ReceiverId = userId,
            InstanceId = instanceId,
            Type = "系统",
            Title = isApproved ? "您的审批单已通过" : "您的审批单已被驳回",
            Content = $"{instanceTitle} - {(isApproved ? "已通过" : "已被驳回")}",
            IsRead = false
        };
        await _notificationRep.InsertAsync(notification);

        await PushSignalRNotification(userId, notification);
    }

    /// <summary>
    /// 推送SignalR通知
    /// </summary>
    private async Task PushSignalRNotification(long userId, ApprovalNotification notification)
    {
        try
        {
            var hubContext = App.GetService<IHubContext<OnlineUserHub, IOnlineUserHub>>();
            if (hubContext != null)
            {
                await hubContext.Clients.User(userId.ToString()).ReceiveMessage(
                    new { success = true, type = 1, message = notification.Title });
            }
        }
        catch
        {
            // SignalR推送失败不影响主流程
        }
    }

    /// <summary>
    /// 获取我的通知列表
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "Page")]
    public async Task<SqlSugarPagedList<NotificationOutput>> Page(NotificationInput input)
    {
        var userId = _userManager.UserId;

        return await _notificationRep.AsQueryable()
            .Where(n => n.ReceiverId == userId)
            .WhereIF(input.IsRead.HasValue, n => n.IsRead == input.IsRead)
            .WhereIF(!string.IsNullOrWhiteSpace(input.Type), n => n.Type == input.Type)
            .OrderByDescending(n => n.CreateTime)
            .Select<NotificationOutput>()
            .ToPagedListAsync(input.Page, input.PageSize);
    }

    /// <summary>
    /// 标记已读
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "MarkRead")]
    public async Task MarkRead(BaseIdInput input)
    {
        var notification = await _notificationRep.GetByIdAsync(input.Id)
            ?? throw Oops.Oh("通知不存在");

        notification.IsRead = true;
        notification.ReadTime = DateTime.Now;
        await _notificationRep.UpdateAsync(notification);
    }

    /// <summary>
    /// 全部已读
    /// </summary>
    [HttpPost]
    [ApiDescriptionSettings(Name = "MarkAllRead")]
    public async Task MarkAllRead()
    {
        var userId = _userManager.UserId;
        await _notificationRep.AsUpdateable()
            .SetColumns(n => n.IsRead == true)
            .SetColumns(n => n.ReadTime == DateTime.Now)
            .Where(n => n.ReceiverId == userId && !n.IsRead)
            .ExecuteCommandAsync();
    }

    /// <summary>
    /// 获取未读通知数量
    /// </summary>
    [HttpGet]
    [ApiDescriptionSettings(Name = "GetUnreadCount")]
    public async Task<int> GetUnreadCount()
    {
        var userId = _userManager.UserId;
        return await _notificationRep.AsQueryable()
            .Where(n => n.ReceiverId == userId && !n.IsRead)
            .CountAsync();
    }
}
