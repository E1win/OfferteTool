namespace Application.Models.TenderComparison;

public class TenderComparisonSubmission
{
    public required Guid SubmissionId { get; init; }
    public required string SupplierName { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required int Rank { get; init; }
    public required decimal AwardedScore { get; init; }
    public required decimal ScorePercentage { get; init; }
    public required int CompletedReviewCount { get; init; }
    public required int ReviewerCount { get; init; }
}
