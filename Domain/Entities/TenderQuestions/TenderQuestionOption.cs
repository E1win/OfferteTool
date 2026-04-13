using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.TenderQuestions;

public class TenderQuestionOption
{
    public Guid Id { get; set; }

    public Guid QuestionId { get; set; }
    public ChoiceQuestion? Question { get; set; }

    public int Order { get; set; }
    
    [MaxLength(512)]
    public required string Text { get; set; }
}
