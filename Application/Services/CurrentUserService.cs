using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class CurrentUserService(UserManager<ApplicationUser> userManager) : ICurrentUserService
{
    public async Task<(ApplicationUser User, string Role)> GetUserWithRoleAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("Uw account kon niet worden gevonden.");

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault()
            ?? throw new InvalidOperationException("Aan uw account is nog geen rol gekoppeld.");

        return (user, role);
    }
}
