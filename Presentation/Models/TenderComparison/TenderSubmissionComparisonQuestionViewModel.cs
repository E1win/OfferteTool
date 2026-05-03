namespace Presentation.Models.TenderComparison;

public class TenderSubmissionComparisonQuestionViewModel
{
    public required Guid QuestionId { get; init; }
    public required int Order { get; init; }
    public required string Text { get; init; }
    public required int? MaximumScore { get; init; }
    public required string Answer { get; init; }
    public required IReadOnlyList<TenderSubmissionComparisonReviewerScoreViewModel> ReviewerScores { get; init; }
}
