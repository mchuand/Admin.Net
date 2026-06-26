// Admin.NET 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//
// 本项目主要遵循 MIT 许可证和 Apache 许可证（版本 2.0）进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 和 LICENSE-APACHE 文件。
//
// 不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目二次开发而产生的一切法律纠纷和责任，我们不承担任何责任！

namespace Admin.NET.Plugin.ApprovalFlow;

/// <summary>
/// 审批流程菜单表种子数据
/// </summary>
public class SysMenuSeedData : ISqlSugarEntitySeedData<SysMenu>
{
    /// <summary>
    /// 种子数据
    /// </summary>
    /// <returns></returns>
    public IEnumerable<SysMenu> HasData()
    {
        return new[]
        {
            // 审批流程目录（不可点击，仅作为分组）
            new SysMenu{ Id=1310300010101, Pid=1300300000101, Title="审批流程", Path="", Name="approvalFlowGroup", Component="", Icon="ele-Help", Type=MenuTypeEnum.Dir, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=2000 },
            // 流程设计（设计流程、创建节点、分配审核人、配置分支等）
            new SysMenu{ Id=1310300010102, Pid=1310300010101, Title="流程设计", Path="/platform/approvalFlow", Name="approvalFlow", Component="/approvalFlow/index", Icon="ele-SetUp", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=10 },
            // 待办审批
            new SysMenu{ Id=1310300010103, Pid=1310300010101, Title="待办审批", Path="/platform/approvalFlow/todo", Name="approvalFlowTodo", Component="/approvalFlow/todo", Icon="ele-Bell", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=20 },
            // 已办审批
            new SysMenu{ Id=1310300010104, Pid=1310300010101, Title="已办审批", Path="/platform/approvalFlow/done", Name="approvalFlowDone", Component="/approvalFlow/done", Icon="ele-DocumentChecked", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=30 },
            // 我发起的
            new SysMenu{ Id=1310300010105, Pid=1310300010101, Title="我发起的", Path="/platform/approvalFlow/mine", Name="approvalFlowMine", Component="/approvalFlow/mine", Icon="ele-Promotion", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=40 },
            // 委托管理
            new SysMenu{ Id=1310300010106, Pid=1310300010101, Title="委托管理", Path="/platform/approvalFlow/delegation", Name="approvalFlowDelegation", Component="/approvalFlow/delegation", Icon="ele-Switch", Type=MenuTypeEnum.Menu, CreateTime=DateTime.Parse("2022-02-10 00:00:00"), OrderNo=50 },
        };
    }
}