using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.TenderReview;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Services;

public class TenderReviewService(
    ITenderSubmissionReviewRepository tenderSubmissionReviewRepository,
    ICurrentUserService currentUserService,
    ITenderSubmissionEncryptionService tenderSubmissionEncryptionService) : ITenderReviewService
{
    public async Task<TenderSubmission> GetSubmissionForReviewAsync(Guid tenderId, Guid submissionId, string reviewerUserId)
    {
        var submission = await GetAccessibleSubmissionAsync(tenderId, submissionId, reviewerUserId);

        tenderSubmissionEncryptionService.Decrypt(submission);

        return submission;
    }

    public async Task<TenderSubmissionReview?> GetReviewAsync(Guid tenderId, Guid submissionId, string reviewerUserId)
    {
        await GetAccessibleSubmissionAsync(tenderId, submissionId, reviewerUserId);

        return await tenderSubmissionReviewRepository.GetBySubmissionAndReviewerAsync(submissionId, reviewerUserId);
    }

    public async Task<List<TenderSubmissionReview>> GetReviewsAsync(Guid tenderId, Guid submissionId, string reviewerUserId)
    {
        await GetAccessibleSubmissionAsync(tenderId, submissionId, reviewerUserId);

        return await tenderSubmissionReviewRepository.GetBySubmissionAsync(submissionId);
    }

    public async Task<TenderSubmissionReview> SaveReviewAsync(
        Guid tenderId,
        Guid submissionId,
        IEnumerable<TenderQuestionRatingInput> ratings,
        string reviewerUserId)
    {
        var submission = await GetAccessibleSubmissionAsync(tenderId, submissionId, reviewerUserId);
        var normalizedRatings = NormalizeRatings(ratings, submission.Tender!);

        if (normalizedRatings.Count == 0)
            throw new BusinessRuleViolationException("Beoordeel minimaal een vraag voordat u de review opslaat.");

        var review = await tenderSubmissionReviewRepository.GetBySubmissionAndReviewerAsync(submissionId, reviewerUserId);
        var isNewReview = review is null;

        review ??= new TenderSubmissionReview(submissionId, reviewerUserId);

        SyncQuestionRatings(review, normalizedRatings);
        review.MarkReviewed(DateTime.UtcNow);

        if (isNewReview)
            return await tenderSubmissionReviewRepository.AddAsync(review);

        await tenderSubmissionReviewRepository.SaveChangesAsync();
        return review;
    }

    private async Task<TenderSubmission> GetAccessibleSubmissionAsync(Guid tenderId, Guid submissionId, string reviewerUserId)
    {
        var submission = await tenderSubmissionReviewRepository.GetSubmissionForReviewAsync(submissionId)
            ?? throw new KeyNotFoundException("De inschrijving kon niet worden gevonden.");

        if (submission.TenderId != tenderId)
            throw new KeyNotFoundException("De inschrijving kon niet worden gevonden.");

        var tender = submission.Tender
            ?? throw new InvalidOperationException("De inschrijving is niet volledig geladen voor beoordeling.");

        var (reviewer, _) = await currentUserService.GetUserWithRoleAsync(reviewerUserId);

        if (!tender.CanReview(reviewer))
            throw new UnauthorizedAccessException("U heeft geen toegang tot deze inschrijving.");

        return submission;
    }

    private static IReadOnlyDictionary<Guid, TenderQuestionRatingInput> NormalizeRatings(
        IEnumerable<TenderQuestionRatingInput> ratings,
        Tender tender)
    {
        ArgumentNullException.ThrowIfNull(ratings);
        ArgumentNullException.ThrowIfNull(tender);

        var scoredQuestionIds = tender.Questions
            .Where(question => question.Score.HasValue)
            .Select(question => question.Id)
            .ToHashSet();

        var normalizedRatings = new Dictionary<Guid, TenderQuestionRatingInput>();

        foreach (var rating in ratings)
        {
            if (!scoredQuestionIds.Contains(rating.QuestionId))
                throw new BusinessRuleViolationException("Alleen vragen met een score kunnen worden beoordeeld.");

            if (normalizedRatings.ContainsKey(rating.QuestionId))
                throw new BusinessRuleViolationException("Elke vraag mag maar één keer worden beoordeeld.");

            normalizedRatings.Add(rating.QuestionId, rating);
        }

        return normalizedRatings;
    }

    private static void SyncQuestionRatings(
        TenderSubmissionReview review,
        IReadOnlyDictionary<Guid, TenderQuestionRatingInput> ratingsByQuestionId)
    {
        var existingQuestionReviews = review.QuestionReviews.ToList();

        foreach (var questionReview in existingQuestionReviews)
        {
            if (!ratingsByQuestionId.ContainsKey(questionReview.QuestionId))
                review.QuestionReviews.Remove(questionReview);
        }

        foreach (var rating in ratingsByQuestionId.Values)
            review.SetQuestionRating(rating.QuestionId, rating.Rating);
    }
}
