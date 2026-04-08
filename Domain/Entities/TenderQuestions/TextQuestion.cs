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

    public override void ValidateAnswer(object? answer)
    {
        if (answer == null)
            throw new InvalidOperationException("Answer is required.");

        if (answer is not string text)
            throw new InvalidOperationException("Answer must be a string.");

        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("Answer cannot be empty.");

        if (MaxLength.HasValue && text.Length > MaxLength.Value)
            throw new InvalidOperationException($"Answer exceeds max length of {MaxLength.Value}.");
    }
}
