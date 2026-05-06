using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.Tender;
using Domain.Constants;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Application.Services;

public class TenderService(
    ITenderRepository tenderRepository,
    ICurrentUserService currentUserService,
    ITenderChangeLogRepository tenderChangeLogRepository) : ITenderService
{
    public async Task<List<Tender>> GetAccessibleTendersAsync(string userId)
    {
        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        return role switch
        {
            Roles.Inkoper when user.OrganisationId is not null
                => await tenderRepository.GetByOrganisationAsync(user.OrganisationId.Value),
            Roles.Beoordelaar
                => await tenderRepository.GetClosedByReviewerAsync(user.Id),
            Roles.Leverancier
                => await tenderRepository.GetPublicOpenAsync(),
            _ => []
        };
    }

    public async Task<Tender> GetAccessibleTenderByIdAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdWithReviewersAsync(tenderId)
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

        // Add current user as a reviewer
        tender.AddReviewer(user.Id);

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
        existingTender.EndDate = updatedTender.EndDate;
        existingTender.IsPublic = updatedTender.IsPublic;

        await tenderRepository.UpdateAsync();

        return existingTender;
    }

    public async Task<Tender> AmendTenderDetailsAsync(Guid tenderId, TenderDetailsAmendment amendment, string userId)
    {
        var existingTender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!existingTender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        if (!existingTender.CanBeAmended())
            throw new BusinessRuleViolationException("Titel en beschrijving kunnen alleen worden aangepast zolang het offertetraject open staat.");

        var newTitle = amendment.Title.Trim();
        var newDescription = amendment.Description.Trim();

        ValidatePublishedDetailsAmendment(newTitle, newDescription);

        var hasChanges = false;

        if (!string.Equals(existingTender.Title, newTitle, StringComparison.Ordinal))
        {
            await tenderChangeLogRepository.AddAsync(new TenderChangeLog
            {
                TenderId = existingTender.Id,
                Type = TenderChangeLogType.TenderTitleAmended,
                FieldName = nameof(Tender.Title),
                OldValue = existingTender.Title,
                NewValue = newTitle,
                SupplierVisibleMessage = $"De titel is gewijzigd van \"{existingTender.Title}\" naar \"{newTitle}\".",
                ChangedAtUtc = DateTimeOffset.UtcNow,
                ChangedByUserId = user.Id,
                ChangedByDisplayName = GetDisplayName(user)
            });

            existingTender.Title = newTitle;
            hasChanges = true;
        }

        if (!string.Equals(existingTender.Description, newDescription, StringComparison.Ordinal))
        {
            await tenderChangeLogRepository.AddAsync(new TenderChangeLog
            {
                TenderId = existingTender.Id,
                Type = TenderChangeLogType.TenderDescriptionAmended,
                FieldName = nameof(Tender.Description),
                OldValue = existingTender.Description,
                NewValue = newDescription,
                SupplierVisibleMessage = "De beschrijving van het offertetraject is gewijzigd.",
                ChangedAtUtc = DateTimeOffset.UtcNow,
                ChangedByUserId = user.Id,
                ChangedByDisplayName = GetDisplayName(user)
            });

            existingTender.Description = newDescription;
            hasChanges = true;
        }

        if (hasChanges)
            await tenderChangeLogRepository.SaveChangesAsync();

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

    public async Task<Tender> CloseTenderAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        // Closes tender if valid, otherwise throws an exception with the reason
        tender.Close();

        await tenderRepository.UpdateAsync();

        return tender;
    }

    public async Task<Tender> CompleteTenderAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        tender.Complete();

        await tenderRepository.UpdateAsync();

        return tender;
    }

    private static void ValidatePublishedDetailsAmendment(string title, string description)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new BusinessRuleViolationException("Vul een titel in.");

        if (title.Length > 256)
            throw new BusinessRuleViolationException("De titel mag maximaal 256 tekens bevatten.");

        if (string.IsNullOrWhiteSpace(description))
            throw new BusinessRuleViolationException("Vul een beschrijving in.");

        if (description.Length > 2048)
            throw new BusinessRuleViolationException("De beschrijving mag maximaal 2048 tekens bevatten.");
    }

    private static string GetDisplayName(ApplicationUser user)
    {
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;

        if (!string.IsNullOrWhiteSpace(user.Email))
            return user.Email!;

        return user.UserName ?? "Onbekende gebruiker";
    }
}
