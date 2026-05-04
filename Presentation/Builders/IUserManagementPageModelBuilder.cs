using Presentation.Models.UserManagement;

namespace Presentation.Builders;

public interface IUserManagementPageModelBuilder
{
    Task<UserManagementIndexViewModel> BuildIndexAsync(
        string? search = null,
        UserFormViewModel? createUser = null,
        bool openCreateUserModal = false,
        string? createErrorMessage = null,
        UserFormViewModel? editUser = null,
        bool openEditUserModal = false,
        string? editErrorMessage = null);
}
