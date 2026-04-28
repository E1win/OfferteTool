using Presentation.Models.TenderReview;

namespace Presentation.Builders;

public interface ITenderReviewPageModelBuilder
{
    Task<TenderReviewPageViewModel> BuildEditAsync(
        Guid tenderId,
        Guid submissionId,
        string reviewerUserId,
        TenderReviewFormViewModel? form = null,
        string? errorMessage = null);
}
