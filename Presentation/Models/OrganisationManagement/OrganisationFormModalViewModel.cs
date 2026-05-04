namespace Presentation.Models.OrganisationManagement;

public class OrganisationFormModalViewModel
{
    public string ModalId { get; set; } = "organisationModal";
    public string ModalTitle { get; set; } = string.Empty;
    public string SubmitAction { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool ShowOnLoad { get; set; }
    public bool ShowIsActive { get; set; }
    public OrganisationFormViewModel Form { get; set; } = new();
}
