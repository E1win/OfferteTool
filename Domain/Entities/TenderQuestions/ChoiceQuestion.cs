using Domain.Entities.TenderAnswers;
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

    public override void ValidateAnswer(TenderAnswer answer)
    {
        if (answer == null)
            throw new InvalidOperationException("Answer is required.");

        if (answer is not ChoiceAnswer choiceAnswer)
            throw new InvalidOperationException("Answer type does not match question type.");
        
        var selectedOptionIds = choiceAnswer.Selections.Select(s => s.OptionId).ToHashSet();

        if (selectedOptionIds.Count == 0)
            throw new InvalidOperationException("At least one option must be selected.");

        if (!AllowMultipleSelection && selectedOptionIds.Count > 1)
            throw new InvalidOperationException("Multiple selections are not allowed for this question.");

        var validOptionIds = Options.Select(o => o.Id).ToHashSet();

        if (selectedOptionIds.Any(id => !validOptionIds.Contains(id)))
            throw new InvalidOperationException("One or more selected options are invalid.");
    }
}
