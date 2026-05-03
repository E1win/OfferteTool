namespace Presentation.Models.TenderComparison;

public class TenderComparisonPageViewModel
{
    public required Guid TenderId { get; init; }
    public required string TenderTitle { get; init; }
    public required string TenderDescription { get; init; }
    public required decimal MaximumScore { get; init; }
    public required IReadOnlyList<TenderComparisonSubmissionViewModel> Submissions { get; init; }
}
