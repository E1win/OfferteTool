using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Models.TenderComparison;
using Domain.Entities;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Services;

namespace Application.Services;

public class TenderComparisonService(
    ITenderRepository tenderRepository,
    ICurrentUserService currentUserService) : ITenderComparisonService
{
    public async Task<TenderComparisonDashboard> GetTenderComparisonDashboardAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdWithComparisonDataAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.CanBeManagedBy(user, role))
            throw new UnauthorizedAccessException("U kunt dit offertetraject niet beheren.");

        if (tender.Status != TenderStatus.Completed)
            throw new BusinessRuleViolationException("Inschrijvingen kunnen pas worden vergeleken zodra het offertetraject is afgerond.");

        var scoredQuestions = tender.Questions
            .Where(question => question.Score.HasValue)
            .OrderBy(question => question.Order)
            .ToList();
        var maximumScore = TenderReviewScoreCalculator.CalculateMaximumPoints(scoredQuestions);
        var submissions = tender.Submissions
            .OrderBy(submission => submission.Supplier?.Name ?? string.Empty)
            .ThenBy(submission => submission.SubmittedAt)
            .ToList();
        var submissionRows = CreateSubmissionRows(submissions, scoredQuestions, maximumScore, tender.Reviewers.Count);

        return new TenderComparisonDashboard
        {
            TenderId = tender.Id,
            TenderTitle = tender.Title,
            Status = tender.Status,
            MaximumScore = maximumScore,
            ReviewerCount = tender.Reviewers.Count,
            Submissions = submissionRows
        };
    }

    private static List<TenderComparisonSubmission> CreateSubmissionRows(
        IReadOnlyList<TenderSubmission> submissions,
        IReadOnlyList<TenderQuestion> scoredQuestions,
        decimal maximumScore,
        int reviewerCount)
    {
        var rankedSubmissions = submissions
            .Select(submission => new
            {
                Submission = submission,
                AwardedScore = TenderReviewScoreCalculator.CalculateAwardedPoints(submission.Reviews, scoredQuestions)
            })
            .OrderByDescending(submission => submission.AwardedScore)
            .ThenBy(submission => submission.Submission.Supplier?.Name ?? string.Empty)
            .ThenBy(submission => submission.Submission.SubmittedAt)
            .ToList();

        var rows = new List<TenderComparisonSubmission>();
        decimal? previousScore = null;
        var previousRank = 0;

        for (var index = 0; index < rankedSubmissions.Count; index++)
        {
            var rankedSubmission = rankedSubmissions[index];
            var rank = previousScore == rankedSubmission.AwardedScore
                ? previousRank
                : index + 1;

            rows.Add(new TenderComparisonSubmission
            {
                SubmissionId = rankedSubmission.Submission.Id,
                SupplierName = GetSupplierName(rankedSubmission.Submission),
                SubmittedAt = rankedSubmission.Submission.SubmittedAt,
                Rank = rank,
                AwardedScore = rankedSubmission.AwardedScore,
                ScorePercentage = CalculatePercentage(rankedSubmission.AwardedScore, maximumScore),
                CompletedReviewCount = rankedSubmission.Submission.Reviews.Count,
                ReviewerCount = reviewerCount
            });

            previousScore = rankedSubmission.AwardedScore;
            previousRank = rank;
        }

        return rows;
    }

    private static decimal CalculatePercentage(decimal awardedScore, decimal maximumScore) =>
        maximumScore == 0
            ? 0
            : decimal.Round(awardedScore / maximumScore * 100, 2, MidpointRounding.AwayFromZero);

    private static string GetSupplierName(TenderSubmission submission) =>
        submission.Supplier?.Name ?? "Onbekende leverancier";
}
