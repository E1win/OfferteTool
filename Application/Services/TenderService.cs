using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services;

public class TenderService(ITenderRepository tenderRepository, ICurrentUserService currentUserService) : ITenderService
{
    public async Task<List<Tender>> GetAccessibleTendersAsync(string userId)
    {
        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

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
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        return tender.IsAccessibleBy(user, role)
            ? tender
            : throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");
    }

    public async Task<bool> CanManageTenderAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        return tender.CanBeManagedBy(user, role);
    }

    public async Task<Tender> CreateTenderAsync(Tender tender, string userId)
    {
        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (role != Roles.Inkoper)
            throw new UnauthorizedAccessException("Alleen inkopers kunnen offertetrajecten aanmaken.");

        if (user.OrganisationId is null)
            throw new BusinessRuleViolationException("Uw account is nog niet gekoppeld aan een organisatie.");

        tender.ValidateDates();

        // For security, these values are set by the system, not the client
        tender.OrganisationId = user.OrganisationId.Value;
        tender.Status = TenderStatus.Design;

        return await tenderRepository.AddAsync(tender);
    }

    public async Task<Tender> UpdateTenderAsync(Guid tenderId, Tender updatedTender, string userId)
    {
        var existingTender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!existingTender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        if (!existingTender.CanBeEdited())
            throw new BusinessRuleViolationException("Alleen offertetrajecten met de status Ontwerp kunnen worden gewijzigd.");

        updatedTender.ValidateDates();

        // Update only allowed fields
        existingTender.Title = updatedTender.Title;
        existingTender.Description = updatedTender.Description;
        existingTender.StartDate = updatedTender.StartDate;
        existingTender.EndDate = updatedTender.EndDate;
        existingTender.IsPublic = updatedTender.IsPublic;

        await tenderRepository.UpdateAsync();

        return existingTender;
    }

    public async Task<Tender> OpenTenderAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdWithQuestionsAndOptionsAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        // Opens tender if valid, otherwise throws an exception with the reason
        tender.Open();

        await tenderRepository.UpdateAsync();

        return tender;
    }
}
