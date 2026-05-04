using Application.Models.UserManagement;

namespace Application.Interfaces.Services;

public interface IUserManagementService
{
    Task<List<ManagedUser>> GetUsersAsync();
    Task<ManagedUser> GetUserAsync(string userId);
    Task<CreateUserResult> CreateUserAsync(CreateUserRequest request, string actorUserId);
    Task<ManagedUser> UpdateUserAsync(UpdateUserRequest request, string actorUserId);
    Task DisableUserAsync(string userId, string actorUserId);
}
