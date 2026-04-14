using Domain.Enums;

namespace Presentation.Models.Questionnaire;

public class QuestionnaireEditorBootstrapViewModel
{
    public required Guid TenderId { get; init; }
    public required string ApiBaseUrl { get; init; }
    public required bool CanManageQuestions { get; init; }
    public required string AntiforgeryHeaderName { get; init; }
    public required QuestionnaireQuestionTypeLookupViewModel QuestionTypes { get; init; }
}
