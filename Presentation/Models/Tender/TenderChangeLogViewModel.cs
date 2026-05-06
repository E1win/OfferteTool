namespace Presentation.Models.Tender;

public class TenderChangeLogViewModel
{
    public required DateTimeOffset ChangedAtUtc { get; init; }
    public required string Message { get; init; }
    public required string ChangedByDisplayName { get; init; }
}
