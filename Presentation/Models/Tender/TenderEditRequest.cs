using System.ComponentModel.DataAnnotations;

namespace Presentation.Models.Tender;

public class TenderEditRequest
{
    [Required(ErrorMessage = "Vul een titel in.")]
    [MaxLength(256, ErrorMessage = "De titel mag maximaal 256 tekens bevatten.")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vul een beschrijving in.")]
    [MaxLength(2048, ErrorMessage = "De beschrijving mag maximaal 2048 tekens bevatten.")]
    public string Description { get; init; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly StartDate { get; init; }

    [DataType(DataType.Date)]
    public DateOnly EndDate { get; init; }

    public bool IsPublic { get; init; }
}
