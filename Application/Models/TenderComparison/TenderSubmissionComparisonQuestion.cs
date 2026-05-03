namespace Application.Models.TenderComparison;

public class TenderSubmissionComparisonQuestion
{
    public required Guid QuestionId { get; init; }
    public required int Order { get; init; }
    public required string Text { get; init; }
    public required int? MaximumScore { get; init; }
    public required TenderSubmissionComparisonAnswer Answer { get; init; }
    public required IReadOnlyList<TenderSubmissionComparisonReviewerScore> ReviewerScores { get; init; }
}
