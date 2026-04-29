using Domain.Enums;

namespace Presentation.Models.TenderReview;

public class TenderReviewRatingOptionViewModel
{
    public required TenderReviewRating Value { get; init; }
    public required string Label { get; init; }
}
