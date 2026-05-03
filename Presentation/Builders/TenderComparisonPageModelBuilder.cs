using Application.Interfaces.Services;
using Application.Models.TenderComparison;
using Domain.Enums;
using Presentation.Models.TenderComparison;

namespace Presentation.Builders;

public class TenderComparisonPageModelBuilder(
    ITenderComparisonService tenderComparisonService) : ITenderComparisonPageModelBuilder
{
    public async Task<TenderComparisonPageViewModel> BuildComparisonAsync(Guid tenderId, string userId)
    {
        var dashboard = await tenderComparisonService.GetTenderComparisonDashboardAsync(tenderId, userId);

        return new TenderComparisonPageViewModel
        {
            TenderId = dashboard.TenderId,
            TenderTitle = dashboard.TenderTitle,
            TenderDescription = dashboard.TenderDescription,
            MaximumScore = dashboard.MaximumScore,
            Submissions = dashboard.Submissions
                .Select(submission => new TenderComparisonSubmissionViewModel
                {
                    SubmissionId = submission.SubmissionId,
                    SupplierName = submission.SupplierName,
                    SubmittedAt = submission.SubmittedAt,
                    Rank = submission.Rank,
                    AwardedScore = submission.AwardedScore,
                    ScorePercentage = submission.ScorePercentage,
                    CompletedReviewCount = submission.CompletedReviewCount,
                    ReviewerCount = submission.ReviewerCount
                })
                .ToList()
        };
    }

    public async Task<TenderSubmissionComparisonPageViewModel> BuildSubmissionComparisonAsync(
        Guid tenderId,
        Guid submissionId,
        string userId)
    {
        var details = await tenderComparisonService.GetTenderSubmissionComparisonDetailsAsync(tenderId, submissionId, userId);

        return new TenderSubmissionComparisonPageViewModel
        {
            TenderId = details.TenderId,
            TenderTitle = details.TenderTitle,
            TenderDescription = details.TenderDescription,
            SubmissionId = details.SubmissionId,
            SupplierName = details.SupplierName,
            SubmittedAt = details.SubmittedAt,
            MaximumScore = details.MaximumScore,
            AwardedScore = details.AwardedScore,
            ScorePercentage = details.ScorePercentage,
            Questions = details.Questions
                .OrderBy(question => question.Order)
                .Select(question => new TenderSubmissionComparisonQuestionViewModel
                {
                    QuestionId = question.QuestionId,
                    Order = question.Order,
                    Text = question.Text,
                    MaximumScore = question.MaximumScore,
                    Answer = FormatAnswer(question.Answer),
                    ReviewerScores = question.ReviewerScores
                        .Select(score => new TenderSubmissionComparisonReviewerScoreViewModel
                        {
                            ReviewerName = score.ReviewerName,
                            RatingLabel = GetRatingLabel(score.Rating),
                            AwardedScore = score.AwardedScore
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    private static string FormatAnswer(TenderSubmissionComparisonAnswer answer) =>
        answer.Type switch
        {
            AnswerType.Text => answer.TextValue ?? string.Empty,
            AnswerType.Numeric => answer.NumericValue?.ToString("0.##") ?? string.Empty,
            AnswerType.Choice => answer.SelectedOptions.Count == 0
                ? string.Empty
                : string.Join(", ", answer.SelectedOptions),
            _ => string.Empty
        };

    private static string GetRatingLabel(TenderReviewRating rating) =>
        rating switch
        {
            TenderReviewRating.Poor => "Slecht",
            TenderReviewRating.Insufficient => "Onvoldoende",
            TenderReviewRating.Sufficient => "Voldoende",
            TenderReviewRating.Good => "Goed",
            TenderReviewRating.Excellent => "Uitstekend",
            _ => rating.ToString()
        };
}
