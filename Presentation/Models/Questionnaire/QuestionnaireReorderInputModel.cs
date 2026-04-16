using System.ComponentModel.DataAnnotations;

namespace Presentation.Models.Questionnaire;

public class QuestionnaireReorderInputModel
{
    [Required(ErrorMessage = "De nieuwe volgorde ontbreekt.")]
    public List<Guid> OrderedQuestionIds { get; set; } = [];
}
