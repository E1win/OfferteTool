using Domain.Enums;

namespace Domain.Entities.TenderAnswers;

public class ChoiceAnswer : TenderAnswer
{
    public ICollection<ChoiceAnswerSelection> Selections { get; set; } = [];

    public ChoiceAnswer()
    {
        Type = AnswerType.Choice;
    }

    public override object? GetValue() => Selections.Select(s => s.OptionId).ToList();
}
