namespace Application.Models.TenderComparison;

public class TenderSubmissionComparisonDetails
{
    public required Guid TenderId { get; init; }
    public required string TenderTitle { get; init; }
    public required string TenderDescription { get; init; }
    public required Guid SubmissionId { get; init; }
    public required string SupplierName { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required decimal MaximumScore { get; init; }
    public required decimal AwardedScore { get; init; }
    public required decimal ScorePercentage { get; init; }
    public required IReadOnlyList<TenderSubmissionComparisonQuestion> Questions { get; init; }
}
