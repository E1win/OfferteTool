namespace Presentation.Models.Tender;

public class TenderReviewerAssignmentModalViewModel
{
    public required string ModalId { get; init; }
    public required string ModalTitle { get; init; }
    public required string SubmitAction { get; init; }
    public required string SubmitButtonText { get; init; }
    public string? ErrorMessage { get; init; }
    public bool ShowOnLoad { get; init; }
    public Guid? TenderId { get; init; }
    public required TenderReviewerAssignmentFormViewModel Form { get; init; }
}
