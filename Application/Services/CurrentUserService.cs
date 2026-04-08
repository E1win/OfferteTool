using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class CurrentUserService(UserManager<ApplicationUser> userManager) : ICurrentUserService
{
    public async Task<(ApplicationUser User, string Role)> GetUserWithRoleAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault()
            ?? throw new InvalidOperationException("User has no role assigned.");

        return (user, role);
    }
}
