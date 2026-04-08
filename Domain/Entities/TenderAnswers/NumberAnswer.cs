using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.TenderAnswers;

public class NumberAnswer : TenderAnswer
{
    public decimal? Value { get; set; }

    public NumberAnswer()
    {
        Type = AnswerType.Numeric;
    }
}
