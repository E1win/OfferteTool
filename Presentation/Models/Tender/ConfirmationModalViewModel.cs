namespace Presentation.Models.Tender;

public class ConfirmationModalViewModel
{
    public string ModalId { get; set; } = "confirmationModal";
    public string ModalTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubmitAction { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public Guid? TenderId { get; set; }
}
