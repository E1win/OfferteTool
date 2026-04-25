namespace Presentation.Models.TenderSubmission;

public class ChoiceTenderSubmissionQuestionViewModel : TenderSubmissionQuestionViewModel
{
    public required bool AllowMultipleSelection { get; init; }
    public required IReadOnlyList<TenderSubmissionOptionViewModel> Options { get; init; }
}
