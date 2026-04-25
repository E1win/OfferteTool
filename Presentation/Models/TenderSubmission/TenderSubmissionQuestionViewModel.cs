using Domain.Enums;

namespace Presentation.Models.TenderSubmission;

public abstract class TenderSubmissionQuestionViewModel
{
    public required int Index { get; init; }
    public required string Text { get; init; }
    public required TenderSubmissionAnswerInputModel Answer { get; init; }

    public string FieldPrefix => $"Form.Answers[{Index}]";
    public QuestionType Type => Answer.Type;
}
