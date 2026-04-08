using Domain.Enums;

namespace Domain.Entities.TenderQuestions;

public class ChoiceQuestion : TenderQuestion
{
    public bool AllowMultipleSelection { get; set; }
    public ICollection<TenderQuestionOption> Options { get; set; } = [];

    public ChoiceQuestion()
    {
        Type = QuestionType.Choice;
    }

    public override void Validate()
    {
        if (Options.Count == 0)
            throw new InvalidOperationException("Choice questions must have at least one option.");

        var duplicateValues = Options
            .GroupBy(o => o.Text)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        if (duplicateValues.Any())
            throw new InvalidOperationException("Duplicate option values are not allowed.");
    }

    public override void ValidateAnswer(object? answer)
    {
        throw new NotImplementedException();
    }
}
