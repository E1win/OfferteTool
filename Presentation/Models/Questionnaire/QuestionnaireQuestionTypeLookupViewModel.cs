using Domain.Enums;

namespace Presentation.Models.Questionnaire;

public class QuestionnaireQuestionTypeLookupViewModel
{
    public required QuestionType Choice { get; init; }
    public required QuestionType Text { get; init; }
    public required QuestionType Numeric { get; init; }
}
