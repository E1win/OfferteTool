namespace Presentation.Models.TenderReview;

public class TenderReviewPageViewModel
{
    public required Guid TenderId { get; init; }
    public required string TenderTitle { get; init; }
    public required string TenderDescription { get; init; }
    public required Guid SubmissionId { get; init; }
    public required string SupplierName { get; init; }
    public required DateTime SubmittedAt { get; init; }
    public required IReadOnlyList<TenderReviewQuestionViewModel> Questions { get; init; }
    public required TenderReviewFormViewModel Form { get; init; }
    public required IReadOnlyList<TenderReviewRatingOptionViewModel> RatingOptions { get; init; }
    public string? ErrorMessage { get; init; }
}
