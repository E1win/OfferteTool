using System.ComponentModel.DataAnnotations;

namespace Presentation.Models.Tender;

public class TenderDetailsAmendmentFormViewModel
{
    [Required(ErrorMessage = "Vul een titel in.")]
    [MaxLength(256, ErrorMessage = "De titel mag maximaal 256 tekens bevatten.")]
    [Display(Name = "Titel")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vul een beschrijving in.")]
    [MaxLength(2048, ErrorMessage = "De beschrijving mag maximaal 2048 tekens bevatten.")]
    [Display(Name = "Beschrijving")]
    public string Description { get; set; } = string.Empty;
}
