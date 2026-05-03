using Domain.Enums;

namespace Application.Models.TenderComparison;

public class TenderComparisonDashboard
{
    public required Guid TenderId { get; init; }
    public required string TenderTitle { get; init; }
    public required TenderStatus Status { get; init; }
    public required decimal MaximumScore { get; init; }
    public required int ReviewerCount { get; init; }
    public required IReadOnlyList<TenderComparisonSubmission> Submissions { get; init; }
}
