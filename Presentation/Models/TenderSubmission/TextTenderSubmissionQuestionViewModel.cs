namespace Presentation.Models.TenderSubmission;

public class TextTenderSubmissionQuestionViewModel : TenderSubmissionQuestionViewModel
{
    public required int Rows { get; init; }
    public int? MaxLength { get; init; }
}
