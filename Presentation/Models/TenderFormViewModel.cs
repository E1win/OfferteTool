using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public class TenderFormViewModel
{
    [Required]
    [MaxLength(256)]
    [Display(Name = "Titel")]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(2048)]
    [Display(Name = "Beschrijving")]
    public string Description { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Begindatum")]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    [Display(Name = "Einddatum")]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    [Display(Name = "Openbaar")]
    public bool IsPublic { get; set; }
}
