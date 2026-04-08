using Domain.Entities.TenderQuestions;
using Domain.Enums;

namespace Domain.Entities.TenderAnswers;

public abstract class TenderAnswer
{
    public Guid Id { get; set; }

    public required Guid SubmissionId { get; set; }
    public TenderSubmission? Submission { get; set; }

    public required Guid QuestionId { get; set; }
    public TenderQuestion? Question { get; set; }

    public required AnswerType Type { get; set; }
}
