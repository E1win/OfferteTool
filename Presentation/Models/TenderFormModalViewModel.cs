namespace Presentation.Models;

public class TenderFormModalViewModel
{
    public string ModalId { get; set; } = "tenderModal";
    public string ModalTitle { get; set; } = string.Empty;
    public string SubmitAction { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool ShowOnLoad { get; set; }
    public Guid? TenderId { get; set; }
    public TenderFormViewModel Form { get; set; } = new();
}
