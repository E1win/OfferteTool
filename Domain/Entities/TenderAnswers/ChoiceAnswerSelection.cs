using Domain.Entities.TenderQuestions;

namespace Domain.Entities.TenderAnswers;

public class ChoiceAnswerSelection
{
    public Guid Id { get; set; }

    public required Guid ChoiceAnswerId { get; set; }
    public ChoiceAnswer? ChoiceAnswer { get; set; }

    public Guid OptionId { get; set; }
    public TenderQuestionOption? Option { get; set; }
}
