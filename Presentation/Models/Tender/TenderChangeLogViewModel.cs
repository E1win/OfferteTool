namespace Presentation.Models.Tender;

public class TenderChangeLogViewModel
{
    public required DateTimeOffset ChangedAtUtc { get; init; }
    public required string Message { get; init; }
    public required string OldValue { get; init; }
    public required string NewValue { get; init; }
    public required string ChangedByDisplayName { get; init; }
}
