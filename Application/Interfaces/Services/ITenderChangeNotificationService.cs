using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ITenderChangeNotificationService
{
    Task NotifySubmittedSuppliersAsync(Tender tender, IReadOnlyCollection<TenderChangeLog> changes);
}
