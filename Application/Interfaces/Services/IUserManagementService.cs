using Application.Models.UserManagement;

namespace Application.Interfaces.Services;

public interface IUserManagementService
{
    Task<List<ManagedUser>> GetUsersAsync(UserManagementQuery query);
    Task<ManagedUser> GetUserAsync(string userId);
    Task<UserManagementFormOptions> GetFormOptionsAsync();
    Task<CreateUserResult> CreateUserAsync(CreateUserRequest request, string actorUserId);
    Task<ManagedUser> UpdateUserAsync(UpdateUserRequest request, string actorUserId);
}
