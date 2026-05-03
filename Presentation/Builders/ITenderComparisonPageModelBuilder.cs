using Presentation.Models.TenderComparison;

namespace Presentation.Builders;

public interface ITenderComparisonPageModelBuilder
{
    Task<TenderComparisonPageViewModel> BuildComparisonAsync(Guid tenderId, string userId);
    Task<TenderSubmissionComparisonPageViewModel> BuildSubmissionComparisonAsync(Guid tenderId, Guid submissionId, string userId);
}
