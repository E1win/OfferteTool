using Application.Interfaces.Services;
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
}
