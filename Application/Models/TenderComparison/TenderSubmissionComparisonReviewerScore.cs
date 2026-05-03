using Domain.Enums;

namespace Application.Models.TenderComparison;

public class TenderSubmissionComparisonReviewerScore
{
    public required string ReviewerName { get; init; }
    public required TenderReviewRating Rating { get; init; }
    public required decimal AwardedScore { get; init; }
}
