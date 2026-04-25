using System.ComponentModel.DataAnnotations;

namespace Presentation.Models.Tender;

public class TenderCreateRequest
{
    [Required(ErrorMessage = "Vul een titel in.")]
    [MaxLength(256, ErrorMessage = "De titel mag maximaal 256 tekens bevatten.")]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = "Vul een beschrijving in.")]
    [MaxLength(2048, ErrorMessage = "De beschrijving mag maximaal 2048 tekens bevatten.")]
    public string Description { get; init; } = string.Empty;

    [DataType(DataType.Date)]
    public DateOnly StartDate { get; init; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Date)]
    public DateOnly EndDate { get; init; } = DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    public bool IsPublic { get; init; }
}
