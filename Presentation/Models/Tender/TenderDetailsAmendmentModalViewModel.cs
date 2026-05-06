namespace Presentation.Models.Tender;

public class TenderDetailsAmendmentModalViewModel
{
    public string ModalId { get; set; } = "tenderDetailsAmendmentModal";
    public string ModalTitle { get; set; } = string.Empty;
    public string SubmitAction { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool ShowOnLoad { get; set; }
    public Guid TenderId { get; set; }
    public TenderDetailsAmendmentFormViewModel Form { get; set; } = new();
}
