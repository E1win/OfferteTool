using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Presentation.Models.Questionnaire;

public class QuestionnaireQuestionInputModel : IValidatableObject
{
    [Required(ErrorMessage = "Vul een vraag in.")]
    [MaxLength(512, ErrorMessage = "Een vraag mag maximaal 512 tekens bevatten.")]
    public string Text { get; set; } = string.Empty;

    public int? Score { get; set; }

    [Required(ErrorMessage = "Kies een vraagtype.")]
    public QuestionType Type { get; set; } = QuestionType.Choice;

    public bool AllowMultipleSelection { get; set; }
    public int? Rows { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public List<QuestionnaireQuestionOptionInputModel> Options { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Type == QuestionType.Choice)
        {
            var trimmedOptions = Options
                .Select(option => option.Text.Trim())
                .ToList();

            var filledOptionCount = trimmedOptions.Count(text => !string.IsNullOrWhiteSpace(text));

            if (filledOptionCount < 2)
            {
                yield return new ValidationResult(
                    "Voeg minimaal twee volledig ingevulde antwoordopties toe.",
                    [nameof(Options)]);
            }
            else if (trimmedOptions.Any(string.IsNullOrWhiteSpace))
            {
                yield return new ValidationResult(
                    "Vul alle antwoordopties in of verwijder lege opties.",
                    [nameof(Options)]);
            }

            if (trimmedOptions.Any(text => text.Length > 512))
            {
                yield return new ValidationResult(
                    "Een antwoordoptie mag maximaal 512 tekens bevatten.",
                    [nameof(Options)]);
            }
        }

        if (Type == QuestionType.Text)
        {
            if (Rows is < 1)
            {
                yield return new ValidationResult(
                    "Een tekstvraag moet minimaal 1 regel hoog zijn.",
                    [nameof(Rows)]);
            }

            if (MaxLength is < 1)
            {
                yield return new ValidationResult(
                    "Het maximale aantal tekens moet minimaal 1 zijn.",
                    [nameof(MaxLength)]);
            }
        }

        if (Type == QuestionType.Numeric
            && MinValue.HasValue
            && MaxValue.HasValue
            && MinValue > MaxValue)
        {
            yield return new ValidationResult(
                "De minimumwaarde mag niet hoger zijn dan de maximumwaarde.",
                [nameof(MinValue), nameof(MaxValue)]);
        }
    }
}
