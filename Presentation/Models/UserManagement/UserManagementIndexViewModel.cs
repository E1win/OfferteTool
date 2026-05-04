using Application.Models.UserManagement;

namespace Presentation.Models.UserManagement;

public class UserManagementIndexViewModel
{
    public string Search { get; set; } = string.Empty;
    public List<ManagedUser> Users { get; set; } = [];
}
