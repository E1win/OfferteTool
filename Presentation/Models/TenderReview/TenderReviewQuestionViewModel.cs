namespace Presentation.Models.TenderReview;

public abstract class TenderReviewQuestionViewModel
{
    public required int Index { get; init; }
    public required Guid QuestionId { get; init; }
    public required string Text { get; init; }
    public int? Score { get; init; }
    public int? RatingInputIndex { get; init; }
    public bool CanBeRated => RatingInputIndex.HasValue;
}
