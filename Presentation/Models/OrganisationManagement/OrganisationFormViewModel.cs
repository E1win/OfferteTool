using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Presentation.Models.OrganisationManagement;

public class OrganisationFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Vul een organisatienaam in.")]
    [StringLength(256, ErrorMessage = "De organisatienaam mag maximaal 256 tekens bevatten.")]
    [Display(Name = "Naam")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vul een KvK-nummer in.")]
    [RegularExpression(@"^\d{8}$", ErrorMessage = "Het KvK-nummer moet uit 8 cijfers bestaan.")]
    [Display(Name = "KvK-nummer")]
    public string KvkNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kies een organisatietype.")]
    [Display(Name = "Type")]
    public OrganisationType? OrganisationType { get; set; }

    [Display(Name = "Actief")]
    public bool IsActive { get; set; } = true;

    public List<SelectListItem> OrganisationTypeOptions { get; set; } = [];
}
