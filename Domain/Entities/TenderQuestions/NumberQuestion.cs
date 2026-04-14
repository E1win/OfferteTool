using Domain.Entities.TenderAnswers;
using Domain.Enums;

namespace Domain.Entities.TenderQuestions;

public class NumberQuestion : TenderQuestion
{
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }

    public NumberQuestion()
    {
        Type = QuestionType.Numeric;
    }

    public override void Validate()
    {
        if (MinValue.HasValue && MaxValue.HasValue && MinValue > MaxValue)
            throw new InvalidOperationException("De minimale waarde mag niet hoger zijn dan de maximale waarde.");
    }

    public override void ValidateAnswer(TenderAnswer answer)
    {
        if (answer == null)
            throw new InvalidOperationException("Vul een antwoord in.");

        if (answer is not NumberAnswer numberAnswer)
            throw new InvalidOperationException("Het ingevulde antwoord past niet bij deze vraag.");

        if (!numberAnswer.NumericValue.HasValue)
            throw new InvalidOperationException("Vul een getal in.");

        if (MinValue.HasValue && numberAnswer.NumericValue.Value < MinValue.Value)
            throw new InvalidOperationException($"De waarde moet minimaal {MinValue.Value} zijn.");

        if (MaxValue.HasValue && numberAnswer.NumericValue.Value > MaxValue.Value)
            throw new InvalidOperationException($"De waarde mag maximaal {MaxValue.Value} zijn.");
    }

    public override void UpdateFrom(TenderQuestion source)
    {
        if (source is not NumberQuestion numberSource)
            return;

        Text = numberSource.Text;
        Score = numberSource.Score;
        MinValue = numberSource.MinValue;
        MaxValue = numberSource.MaxValue;
    }
}
