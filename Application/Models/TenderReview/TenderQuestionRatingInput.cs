using Domain.Enums;

namespace Application.Models.TenderReview;

public class TenderQuestionRatingInput
{
    public required Guid QuestionId { get; init; }
    public required TenderReviewRating Rating { get; init; }
}
