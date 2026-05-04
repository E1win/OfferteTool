using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Presentation.Models.UserManagement;

public class UserFormViewModel
{
    public string? UserId { get; set; }

    [Required(ErrorMessage = "Vul een e-mailadres in.")]
    [EmailAddress(ErrorMessage = "Vul een geldig e-mailadres in.")]
    [Display(Name = "E-mailadres")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vul een voornaam in.")]
    [Display(Name = "Voornaam")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vul een achternaam in.")]
    [Display(Name = "Achternaam")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kies een rol.")]
    [Display(Name = "Rol")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Organisatie")]
    public Guid? OrganisationId { get; set; }

    public string? OrganisationName { get; set; }

    [Display(Name = "Actief")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> RoleOptions { get; set; } = [];
    public List<SelectListItem> OrganisationOptions { get; set; } = [];
}
