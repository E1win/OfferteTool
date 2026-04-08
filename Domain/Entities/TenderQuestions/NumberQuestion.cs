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
            throw new InvalidOperationException("MinValue cannot be greater than MaxValue.");
    }

    public override void ValidateAnswer(TenderAnswer answer)
    {
        if (answer == null)
            throw new InvalidOperationException("Answer is required.");

        if (answer is not NumberAnswer numberAnswer)
            throw new InvalidOperationException("Answer type does not match question type.");

        if (!numberAnswer.Value.HasValue)
            throw new InvalidOperationException("A numeric value is required.");

        if (MinValue.HasValue && numberAnswer.Value.Value < MinValue.Value)
            throw new InvalidOperationException($"Value must be at least {MinValue.Value}.");

        if (MaxValue.HasValue && numberAnswer.Value.Value > MaxValue.Value)
            throw new InvalidOperationException($"Value must be at most {MaxValue.Value}.");
    }
}
