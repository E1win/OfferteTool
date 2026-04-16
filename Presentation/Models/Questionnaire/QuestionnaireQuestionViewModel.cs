using Domain.Enums;

namespace Presentation.Models.Questionnaire;

public class QuestionnaireQuestionViewModel
{
    public required Guid Id { get; init; }
    public required string Text { get; init; }
    public required int? Score { get; init; }
    public required QuestionType Type { get; init; }
    public required string TypeLabel { get; init; }
    public required int Order { get; init; }
    public required bool AllowMultipleSelection { get; init; }
    public required int? Rows { get; init; }
    public required int? MaxLength { get; init; }
    public required decimal? MinValue { get; init; }
    public required decimal? MaxValue { get; init; }
    public required IReadOnlyList<QuestionnaireOptionViewModel> Options { get; init; }
}
