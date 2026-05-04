namespace Presentation.Models.UserManagement;

public class UserFormModalViewModel
{
    public string ModalId { get; set; } = "userModal";
    public string ModalTitle { get; set; } = string.Empty;
    public string SubmitAction { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public bool ShowOnLoad { get; set; }
    public string? UserId { get; set; }
    public bool ShowIsActive { get; set; }
    public bool AllowOrganisationChange { get; set; } = true;
    public UserFormViewModel Form { get; set; } = new();
}
