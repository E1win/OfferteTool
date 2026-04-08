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
            throw new ArgumentException("Rows must be at least 1.");
        }
        if (MaxLength.HasValue && MaxLength.Value <= 0)
        {
            throw new ArgumentException("MaxLength must be greater than 0.");
        }
    }

    public override void ValidateAnswer(TenderAnswer answer)
    {
        if (answer == null)
            throw new InvalidOperationException("Answer is required.");

        if (answer is not TextAnswer textAnswer)
            throw new InvalidOperationException("Answer type does not match question type.");

        var value = textAnswer.Value;

        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException("Answer cannot be empty.");

        if (MaxLength.HasValue && value.Length > MaxLength.Value)
            throw new InvalidOperationException($"Answer exceeds max length of {MaxLength.Value}.");
    }
}
