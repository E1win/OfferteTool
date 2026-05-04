namespace Application.Models.UserManagement;

public class UserManagementFormOptions
{
    public List<UserRoleOption> Roles { get; set; } = [];
    public List<UserOrganisationOption> Organisations { get; set; } = [];
}
