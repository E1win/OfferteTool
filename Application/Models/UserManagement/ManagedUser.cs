namespace Application.Models.UserManagement;

public class ManagedUser
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public Guid? OrganisationId { get; set; }
    public string? OrganisationName { get; set; }
}
