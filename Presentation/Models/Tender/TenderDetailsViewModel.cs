using Presentation.Models.Questionnaire;
using TenderEntity = Domain.Entities.Tender;

namespace Presentation.Models.Tender;

public class TenderDetailsViewModel
{
    public required TenderEntity Tender { get; init; }
    public required bool CanManageTender { get; init; }
    public string? ActionErrorMessage { get; init; }
    public required IReadOnlyList<TenderReviewerViewModel> AssignedReviewers { get; init; }
    public required IReadOnlyList<TenderSubmissionSupplierViewModel> SupplierSubmissions { get; init; }
    public ConfirmationModalViewModel? OpenTenderModal { get; init; }
    public ConfirmationModalViewModel? CloseTenderModal { get; init; }
    public TenderReviewerAssignmentModalViewModel? ReviewerAssignmentModal { get; init; }
    public TenderFormModalViewModel? EditTenderModal { get; init; }
    public required QuestionnaireEditorBootstrapViewModel QuestionnaireEditor { get; init; }
}
