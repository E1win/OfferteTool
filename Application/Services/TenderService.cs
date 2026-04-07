using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class TenderService(ITenderRepository tenderRepository, UserManager<ApplicationUser> userManager) : ITenderService
{
    public async Task<List<Tender>> GetAccessibleTendersAsync(string userId)
    {
        var (user, role) = await GetUserWithRoleAsync(userId);

        return role switch
        {
            Roles.Inkoper or Roles.Beoordelaar when user.OrganisationId is not null
                => await tenderRepository.GetByOrganisationAsync(user.OrganisationId.Value),
            Roles.Leverancier
                => await tenderRepository.GetPublicOpenAsync(),
            _ => []
        };
    }

    public async Task<Tender> GetAccessibleTenderByIdAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Tender not found.");

        var (user, role) = await GetUserWithRoleAsync(userId);

        var hasAccess = role switch
        {
            Roles.Inkoper or Roles.Beoordelaar
                => user.OrganisationId is not null && tender.OrganisationId == user.OrganisationId.Value,
            Roles.Leverancier
                => tender.IsPublic && tender.Status == TenderStatus.Open,
            _ => false
        };

        return hasAccess
            ? tender
            : throw new UnauthorizedAccessException("You do not have access to this tender.");
    }

    private async Task<(ApplicationUser User, string Role)> GetUserWithRoleAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault()
            ?? throw new InvalidOperationException("User has no role assigned.");

        return (user, role);
    }
}
