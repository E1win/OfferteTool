namespace Presentation.Models.Questionnaire;

public class QuestionnaireStateViewModel
{
    public required bool CanManageQuestions { get; init; }
    public required IReadOnlyList<QuestionnaireQuestionViewModel> Questions { get; init; }
}
