namespace Presentation.Models.TenderComparison;

public class TenderSubmissionComparisonReviewerScoreViewModel
{
    public required string ReviewerName { get; init; }
    public required string RatingLabel { get; init; }
    public required decimal AwardedScore { get; init; }
}
