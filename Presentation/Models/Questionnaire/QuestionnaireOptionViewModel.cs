namespace Presentation.Models.Questionnaire;

public class QuestionnaireOptionViewModel
{
    public required Guid Id { get; init; }
    public required string Text { get; init; }
    public required int Order { get; init; }
}
