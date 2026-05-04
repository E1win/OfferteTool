namespace Application.Models.UserManagement;

public class CreateUserResult
{
    public required ManagedUser User { get; set; }
    public required string InitialPassword { get; set; }
}
