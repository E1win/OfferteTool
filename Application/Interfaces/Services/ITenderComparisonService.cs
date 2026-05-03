using Application.Models.TenderComparison;

namespace Application.Interfaces.Services;

public interface ITenderComparisonService
{
    Task<TenderComparisonDashboard> GetTenderComparisonDashboardAsync(Guid tenderId, string userId);
}
