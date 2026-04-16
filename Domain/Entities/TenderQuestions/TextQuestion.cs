using Domain.Entities.TenderAnswers;
using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities.TenderQuestions;

public class TextQuestion : TenderQuestion
{
    public int Rows { get; set; } = 1; // 1 = single line, > 1 = textarea
    public int? MaxLength { get; set; }

    public TextQuestion()
    {
        Type = QuestionType.Text;
    }

    public override void Validate()
    {
        if (Rows < 1)
            throw new BusinessRuleViolationException("Een tekstveld moet minimaal uit één regel bestaan.");

        if (MaxLength.HasValue && MaxLength.Value <= 0)
            throw new BusinessRuleViolationException("De maximale lengte moet groter zijn dan nul.");
    }

    public override void ValidateAnswer(TenderAnswer answer)
    {
        if (answer == null)
            throw new BusinessRuleViolationException("Vul een antwoord in.");

        if (answer is not TextAnswer textAnswer)
            throw new BusinessRuleViolationException("Het ingevulde antwoord past niet bij deze vraag.");

        var value = textAnswer.TextValue;

        if (string.IsNullOrWhiteSpace(value))
            throw new BusinessRuleViolationException("Het antwoord mag niet leeg zijn.");

        if (MaxLength.HasValue && value.Length > MaxLength.Value)
            throw new BusinessRuleViolationException($"Het antwoord is te lang. Gebruik maximaal {MaxLength.Value} tekens.");
    }

    public override void UpdateFrom(TenderQuestion source)
    {
        if (source is not TextQuestion textSource)
            return;

        Text = textSource.Text;
        Score = textSource.Score;
        Rows = textSource.Rows;
        MaxLength = textSource.MaxLength;
    }
}
