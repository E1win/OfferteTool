using Domain.Enums;

namespace Domain.Entities.TenderAnswers;

public class ChoiceAnswer : TenderAnswer
{
    public ICollection<ChoiceAnswerSelection> Selections { get; set; } = [];

    public ChoiceAnswer()
    {
        Type = AnswerType.Choice;
    }
}
