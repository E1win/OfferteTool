namespace Presentation.Models.Questionnaire;

public class QuestionnaireStateViewModel
{
    public required IReadOnlyList<QuestionnaireQuestionViewModel> Questions { get; init; }
}
