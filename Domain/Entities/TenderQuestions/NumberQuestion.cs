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

    public override void ValidateAnswer(object? answer)
    {
        throw new NotImplementedException();
    }
}
