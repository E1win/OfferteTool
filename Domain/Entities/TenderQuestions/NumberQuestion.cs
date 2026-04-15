using Domain.Entities.TenderAnswers;
using Domain.Enums;
using Domain.Exceptions;

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
            throw new BusinessRuleViolationException("De minimale waarde mag niet hoger zijn dan de maximale waarde.");
    }

    public override void ValidateAnswer(TenderAnswer answer)
    {
        if (answer == null)
            throw new BusinessRuleViolationException("Vul een antwoord in.");

        if (answer is not NumberAnswer numberAnswer)
            throw new BusinessRuleViolationException("Het ingevulde antwoord past niet bij deze vraag.");

        if (!numberAnswer.NumericValue.HasValue)
            throw new BusinessRuleViolationException("Vul een getal in.");

        if (MinValue.HasValue && numberAnswer.NumericValue.Value < MinValue.Value)
            throw new BusinessRuleViolationException($"De waarde moet minimaal {MinValue.Value} zijn.");

        if (MaxValue.HasValue && numberAnswer.NumericValue.Value > MaxValue.Value)
            throw new BusinessRuleViolationException($"De waarde mag maximaal {MaxValue.Value} zijn.");
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
