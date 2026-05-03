using Presentation.Models.TenderComparison;

namespace Presentation.Builders;

public interface ITenderComparisonPageModelBuilder
{
    Task<TenderComparisonPageViewModel> BuildComparisonAsync(Guid tenderId, string userId);
}
