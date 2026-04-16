using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities.TenderAnswers;

public class TextAnswer : TenderAnswer
{
    [MaxLength(4096)]
    public string? TextValue { get; set; }

    public TextAnswer()
    {
        Type = AnswerType.Text;
    }
}
