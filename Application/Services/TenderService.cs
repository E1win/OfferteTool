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

        return tender.IsAccessibleBy(user, role)
            ? tender
            : throw new UnauthorizedAccessException("You do not have access to this tender.");
    }

    public async Task<Tender> CreateTenderAsync(Tender tender, string userId)
    {
        var (user, role) = await GetUserWithRoleAsync(userId);

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Only inkopers can create tenders.");

        if (user.OrganisationId is null)
            throw new InvalidOperationException("User has no organisation.");

        if (!tender.HasValidDateRange())
            throw new InvalidOperationException("EndDate must be later than StartDate.");

        // For security, these values are set by the system, not the client
        tender.OrganisationId = user.OrganisationId.Value;
        tender.Status = TenderStatus.Design;

        return await tenderRepository.AddAsync(tender);
    }

    public async Task<Tender> UpdateTenderAsync(Guid tenderId, Tender updatedTender, string userId)
    {
        var existingTender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Tender not found.");

        var (user, role) = await GetUserWithRoleAsync(userId);

        if (!existingTender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("You do not have access to this tender.");

        if (!existingTender.CanBeEdited())
            throw new InvalidOperationException("Only tenders with the Design status can be updated.");

        if (!updatedTender.HasValidDateRange())
            throw new InvalidOperationException("EndDate must be later than StartDate.");

        // Update only allowed fields
        existingTender.Title = updatedTender.Title;
        existingTender.Description = updatedTender.Description;
        existingTender.StartDate = updatedTender.StartDate;
        existingTender.EndDate = updatedTender.EndDate;
        existingTender.IsPublic = updatedTender.IsPublic;

        await tenderRepository.UpdateAsync();

        return existingTender;
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
