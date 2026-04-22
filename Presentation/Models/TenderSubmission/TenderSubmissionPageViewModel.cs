using TenderEntity = Domain.Entities.Tender;

namespace Presentation.Models.TenderSubmission;

public class TenderSubmissionPageViewModel
{
    public required TenderEntity Tender { get; init; }
    public required IReadOnlyList<TenderSubmissionQuestionViewModel> Questions { get; init; }
    public required TenderSubmissionFormViewModel Form { get; init; }
    public string? ErrorMessage { get; init; }
}
