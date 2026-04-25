using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ApplicationUserRepository(
    AppDbContext dbContext,
    UserManager<ApplicationUser> userManager) : IApplicationUserRepository
{
    public async Task<ApplicationUser?> GetByIdAsync(string userId) =>
        await userManager.FindByIdAsync(userId);

    public async Task<List<ApplicationUser>> GetByOrganisationAsync(Guid organisationId) =>
        await dbContext.Users
            .Where(user => user.OrganisationId == organisationId)
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Email)
            .ToListAsync();

    public async Task<List<ApplicationUser>> GetByIdsAsync(IEnumerable<string> userIds)
    {
        var ids = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return [];

        return await dbContext.Users
            .Where(user => ids.Contains(user.Id))
            .OrderBy(user => user.LastName)
            .ThenBy(user => user.FirstName)
            .ThenBy(user => user.Email)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);
        var roles = await userManager.GetRolesAsync(user);
        return roles.ToList();
    }
}
