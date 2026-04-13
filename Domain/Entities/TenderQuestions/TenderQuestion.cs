using Domain.Entities.TenderAnswers;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.TenderQuestions;

public abstract class TenderQuestion
{
    public Guid Id { get; set; }

    public required Guid TenderId { get; set; }
    public Tender? Tender { get; set; }

    public required int Order { get; set; }

    [MaxLength(512)]
    public required string Text { get; set; }
    public int? Score { get; set; }
    public QuestionType Type { get; protected init; }

    // Check if the question is valid (e.g. for numeric, check if minimal number is not less than maximum)
    public abstract void Validate();

    public abstract void ValidateAnswer(TenderAnswer answer);
}
