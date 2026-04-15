using Domain.Entities.TenderAnswers;
using Domain.Enums;

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
        {
            throw new ArgumentException("Een tekstveld moet minimaal uit één regel bestaan.");
        }
        if (MaxLength.HasValue && MaxLength.Value <= 0)
        {
            throw new ArgumentException("De maximale lengte moet groter zijn dan nul.");
        }
    }

    public override void ValidateAnswer(TenderAnswer answer)
    {
        if (answer == null)
            throw new InvalidOperationException("Vul een antwoord in.");

        if (answer is not TextAnswer textAnswer)
            throw new InvalidOperationException("Het ingevulde antwoord past niet bij deze vraag.");

        var value = textAnswer.TextValue;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Het antwoord mag niet leeg zijn.");

        if (MaxLength.HasValue && value.Length > MaxLength.Value)
            throw new InvalidOperationException($"Het antwoord is te lang. Gebruik maximaal {MaxLength.Value} tekens.");
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
