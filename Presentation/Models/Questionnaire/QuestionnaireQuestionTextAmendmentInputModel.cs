using System.ComponentModel.DataAnnotations;

namespace Presentation.Models.Questionnaire;

public class QuestionnaireQuestionTextAmendmentInputModel
{
    [Required(ErrorMessage = "Vul een vraag in.")]
    [MaxLength(512, ErrorMessage = "Een vraag mag maximaal 512 tekens bevatten.")]
    public string Text { get; set; } = string.Empty;
}
