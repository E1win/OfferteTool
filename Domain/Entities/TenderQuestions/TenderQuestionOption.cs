using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.TenderQuestions;

public class TenderQuestionOption
{
    public Guid Id { get; set; }

    public required Guid QuestionId { get; set; }
    public ChoiceQuestion? Question { get; set; }

    public required int Order { get; set; }
    
    [MaxLength(512)]
    public required string Text { get; set; }
}
