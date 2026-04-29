namespace Presentation.Models.TenderReview;

public class ChoiceTenderReviewQuestionViewModel : TenderReviewQuestionViewModel
{
    public required IReadOnlyList<string> SelectedOptions { get; init; }
}
