using Domain.Enums;

namespace Presentation.Models.TenderReview;

public class TenderQuestionReviewInputModel
{
    public required Guid QuestionId { get; init; }
    public TenderReviewRating? Rating { get; init; }
}
