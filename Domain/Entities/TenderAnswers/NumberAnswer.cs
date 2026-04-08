using Domain.Enums;

namespace Domain.Entities.TenderAnswers;

public class NumberAnswer : TenderAnswer
{
    public decimal? NumericValue { get; set; }

    public NumberAnswer()
    {
        Type = AnswerType.Numeric;
    }
}
