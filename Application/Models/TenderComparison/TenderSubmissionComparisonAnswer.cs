using Domain.Enums;

namespace Application.Models.TenderComparison;

public class TenderSubmissionComparisonAnswer
{
    public required AnswerType Type { get; init; }
    public string? TextValue { get; init; }
    public decimal? NumericValue { get; init; }
    public required IReadOnlyList<string> SelectedOptions { get; init; }
}
