using MyThuatShop.ViewModels;

namespace MyThuatShop.ViewModels.Admin;

public class AdminUsersIndexVm
{
    public string? Q { get; set; }
    public PagedResultVm<AdminUserRowVm> Paged { get; set; } = new();
}
