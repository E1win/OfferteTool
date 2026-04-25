using Domain.Enums;

namespace Presentation.Models.TenderSubmission;

public class TenderSubmissionAnswerInputModel
{
    public Guid QuestionId { get; set; }
    public QuestionType Type { get; set; }
    public string TextValue { get; set; } = string.Empty;
    public decimal? NumericValue { get; set; }
    public List<Guid> SelectedOptionIds { get; set; } = [];
}
