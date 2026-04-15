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

    public void SetOptions(List<TenderQuestionOption> incomingOptions)
    {
        // Check if any incoming option has an Id that doesn't match an existing option (and is not a new option with Guid.Empty)
        var existingOptionIds = Options
            .Select(o => o.Id)
            .ToHashSet();

        var invalidIncomingOption = incomingOptions
            .FirstOrDefault(o => o.Id != Guid.Empty && !existingOptionIds.Contains(o.Id));

        if (invalidIncomingOption != null)
            throw new InvalidOperationException("Een van de opties hoort niet bij deze vraag.");

        // Remove options that are no longer in the incoming list
        var incomingIds = incomingOptions
            .Where(o => o.Id != Guid.Empty)
            .Select(o => o.Id)
            .ToHashSet();

        var toRemove = Options
            .Where(o => !incomingIds.Contains(o.Id))
            .ToList();

        foreach (var option in toRemove)
            Options.Remove(option);

        // Update existing and add new
        for (var i = 0; i < incomingOptions.Count; i++)
        {
            var incoming = incomingOptions[i];
            var existing = Options.FirstOrDefault(o => o.Id == incoming.Id && incoming.Id != Guid.Empty);

            if (existing is not null)
            {
                existing.Text = incoming.Text;
                existing.Order = i;
            }
            else
            {
                Options.Add(new TenderQuestionOption
                {
                    Text = incoming.Text,
                    Order = i
                });
            }
        }
    }

    public override void Validate()
    {
        if (Options.Count < 2)
            throw new InvalidOperationException("Voeg minimaal twee keuzes toe.");

        var duplicateValues = Options
            .GroupBy(o => o.Text)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);

        if (duplicateValues.Any())
            throw new InvalidOperationException("Gebruik voor elke keuze een unieke tekst.");

        if (Options.Any(option => string.IsNullOrEmpty(option.Text)))
            throw new InvalidOperationException("Elke keuze moet een tekst hebben.");
    }

    public override void ValidateAnswer(TenderAnswer answer)
    {
        if (answer == null)
            throw new InvalidOperationException("Vul een antwoord in.");

        if (answer is not ChoiceAnswer choiceAnswer)
            throw new InvalidOperationException("Het ingevulde antwoord past niet bij deze vraag.");
        
        var selectedOptionIds = choiceAnswer.Selections.Select(s => s.OptionId).ToHashSet();

        if (selectedOptionIds.Count == 0)
            throw new InvalidOperationException("Selecteer minimaal één optie.");

        if (!AllowMultipleSelection && selectedOptionIds.Count > 1)
            throw new InvalidOperationException("U kunt bij deze vraag maar één optie kiezen.");

        var validOptionIds = Options.Select(o => o.Id).ToHashSet();

        if (selectedOptionIds.Any(id => !validOptionIds.Contains(id)))
            throw new InvalidOperationException("Een of meer gekozen opties zijn ongeldig.");
    }

    public override void UpdateFrom(TenderQuestion source)
    {
        if (source is not ChoiceQuestion choiceSource)
            return;

        Text = choiceSource.Text;
        Score = choiceSource.Score;
        AllowMultipleSelection = choiceSource.AllowMultipleSelection;
        SetOptions([.. choiceSource.Options]);
    }
}
