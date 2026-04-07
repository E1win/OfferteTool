using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Application.Services;

public class TenderService(ITenderRepository tenderRepository, UserManager<ApplicationUser> userManager) : ITenderService
{
    public async Task<List<Tender>> GetAccessibleTendersAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        var roles = await userManager.GetRolesAsync(user);
        var role = roles.FirstOrDefault()
            ?? throw new InvalidOperationException("User has no role assigned.");

        return role switch
        {
            "Inkoper" or "Beoordelaar" when user.OrganisationId is not null
                => await tenderRepository.GetByOrganisationAsync(user.OrganisationId.Value),
            "Leverancier"
                => await tenderRepository.GetPublicOpenAsync(),
            _ => []
        };
    }
}
