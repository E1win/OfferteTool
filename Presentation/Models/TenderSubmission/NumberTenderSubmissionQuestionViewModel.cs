namespace Presentation.Models.TenderSubmission;

public class NumberTenderSubmissionQuestionViewModel : TenderSubmissionQuestionViewModel
{
    public decimal? MinValue { get; init; }
    public decimal? MaxValue { get; init; }
}
