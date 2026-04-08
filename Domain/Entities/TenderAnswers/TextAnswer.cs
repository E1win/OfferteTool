using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.TenderAnswers;

public class TextAnswer : TenderAnswer
{
    [MaxLength(4096)]
    public string? Value { get; set; }

    public TextAnswer()
    {
        Type = AnswerType.Text;
    }

    public override object? GetValue() => Value;
}
