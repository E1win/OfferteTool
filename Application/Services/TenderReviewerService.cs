using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

public class TenderReviewerService(
    ITenderRepository tenderRepository,
    ICurrentUserService currentUserService,
    IApplicationUserRepository applicationUserRepository) : ITenderReviewerService
{
    public async Task<bool> CanReviewTenderAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdWithReviewersAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, _) = await currentUserService.GetUserWithRoleAsync(userId);

        return tender.CanReview(user);
    }

    public async Task<List<ApplicationUser>> GetAssignableReviewersAsync(Guid tenderId, string actorUserId)
    {
        var tender = await GetManagedTenderAsync(tenderId, actorUserId);
        var organisationUsers = await applicationUserRepository.GetByOrganisationAsync(tender.OrganisationId);

        var assignableReviewers = new List<ApplicationUser>();

        foreach (var user in organisationUsers)
        {
            var roles = await applicationUserRepository.GetRolesAsync(user);

            if (!CanBeReviewer(roles))
                continue;

            if (tender.HasReviewer(user.Id))
                continue;

            assignableReviewers.Add(user);
        }

        return assignableReviewers;
    }

    public async Task<List<ApplicationUser>> GetAssignedReviewersAsync(Guid tenderId, string actorUserId)
    {
        var tender = await GetManagedTenderAsync(tenderId, actorUserId);
        var reviewerIds = tender.Reviewers
            .Select(reviewer => reviewer.UserId)
            .Distinct()
            .ToList();

        if (reviewerIds.Count == 0)
            return [];

        return await applicationUserRepository.GetByIdsAsync(reviewerIds);
    }

    public async Task AddReviewerAsync(Guid tenderId, string reviewerUserId, string actorUserId)
    {
        var tender = await GetManagedTenderAsync(tenderId, actorUserId);
        var reviewer = await applicationUserRepository.GetByIdAsync(reviewerUserId)
            ?? throw new KeyNotFoundException("De beoordelaar kon niet worden gevonden.");
        var roles = await applicationUserRepository.GetRolesAsync(reviewer);

        EnsureReviewerCanBeAssigned(tender, reviewer, roles);

        tender.AddReviewer(reviewer.Id);

        await tenderRepository.UpdateAsync();
    }

    public async Task RemoveReviewerAsync(Guid tenderId, string reviewerUserId, string actorUserId)
    {
        var tender = await GetManagedTenderAsync(tenderId, actorUserId);

        tender.RemoveReviewer(reviewerUserId);

        await tenderRepository.UpdateAsync();
    }

    private async Task<Tender> GetManagedTenderAsync(Guid tenderId, string actorUserId)
    {
        var tender = await tenderRepository.GetByIdWithReviewersAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (actor, actorRole) = await currentUserService.GetUserWithRoleAsync(actorUserId);

        if (!tender.CanBeManagedBy(actor, actorRole))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        return tender;
    }

    private static void EnsureReviewerCanBeAssigned(Tender tender, ApplicationUser reviewer, IReadOnlyCollection<string> roles)
    {
        if (reviewer.OrganisationId is null || reviewer.OrganisationId.Value != tender.OrganisationId)
            throw new BusinessRuleViolationException("Alleen gebruikers uit dezelfde organisatie kunnen als beoordelaar worden toegevoegd.");

        if (!CanBeReviewer(roles))
            throw new BusinessRuleViolationException("Alleen gebruikers met de rol Inkoper of Beoordelaar kunnen als beoordelaar worden toegevoegd.");
    }

    private static bool CanBeReviewer(IEnumerable<string> roles) =>
        roles.Contains(Roles.Inkoper) || roles.Contains(Roles.Beoordelaar);
}
