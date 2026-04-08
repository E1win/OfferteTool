using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ICurrentUserService
{
    public Task<(ApplicationUser User, string Role)> GetUserWithRoleAsync(string userId);
}
