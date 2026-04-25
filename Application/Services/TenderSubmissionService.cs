using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Entities;
using Domain.Entities.TenderAnswers;
using Domain.Exceptions;

namespace Application.Services;

public class TenderSubmissionService(
    ITenderRepository tenderRepository,
    ITenderSubmissionRepository tenderSubmissionRepository,
    ICurrentUserService currentUserService,
    ITenderSubmissionEncryptionService tenderSubmissionEncryptionService) : ITenderSubmissionService
{
    public async Task<TenderSubmission?> GetByTenderForCurrentSupplierAsync(Guid tenderId, string userId)
    {
        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (role != Roles.Leverancier)
            throw new UnauthorizedAccessException("Alleen leveranciers kunnen zich inschrijven op een offertetraject.");

        if (user.OrganisationId is null)
            throw new BusinessRuleViolationException("Uw account is nog niet gekoppeld aan een organisatie.");

        var submission = await tenderSubmissionRepository.GetByTenderAndSupplierAsync(tenderId, user.OrganisationId.Value);

        if (submission is not null)
            tenderSubmissionEncryptionService.Decrypt(submission);

        return submission;
    }

    public async Task<List<TenderSubmission>> GetForManagedTenderAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        return await tenderSubmissionRepository.GetByTenderWithSuppliersAsync(tenderId);
    }

    public async Task<TenderSubmission> SubmitAsync(Guid tenderId, IEnumerable<TenderAnswer> answers, string userId)
    {
        var tender = await tenderRepository.GetByIdWithQuestionsAndOptionsAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (role != Roles.Leverancier)
            throw new UnauthorizedAccessException("Alleen leveranciers kunnen zich inschrijven op een offertetraject.");

        if (user.OrganisationId is null)
            throw new BusinessRuleViolationException("Uw account is nog niet gekoppeld aan een organisatie.");

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");

        tender.EnsureCanReceiveSubmission(DateOnly.FromDateTime(DateTime.UtcNow));

        var submission = await tenderSubmissionRepository.GetByTenderAndSupplierAsync(tenderId, user.OrganisationId.Value);

        if (submission is null)
        {
            submission = new TenderSubmission
            {
                TenderId = tenderId,
                SupplierId = user.OrganisationId.Value
            };

            submission.Submit(tender, answers, DateTime.UtcNow);
            tenderSubmissionEncryptionService.Encrypt(submission);
            return await tenderSubmissionRepository.AddAsync(submission);
        }

        submission.Submit(tender, answers, DateTime.UtcNow);
        tenderSubmissionEncryptionService.Encrypt(submission);
        await tenderSubmissionRepository.SaveChangesAsync();

        return submission;
    }
}
