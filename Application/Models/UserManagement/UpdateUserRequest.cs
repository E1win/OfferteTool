namespace Application.Models.UserManagement;

public class UpdateUserRequest
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public Guid? OrganisationId { get; set; }
    public bool IsActive { get; set; }
}
