using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services;

public class TenderChangeLogService(
    ITenderRepository tenderRepository,
    ITenderChangeLogRepository tenderChangeLogRepository,
    ICurrentUserService currentUserService) : ITenderChangeLogService
{
    public async Task<List<TenderChangeLog>> GetVisibleTenderChangesAsync(Guid tenderId, string userId)
    {
        var tender = await tenderRepository.GetByIdWithReviewersAsync(tenderId)
            ?? throw new KeyNotFoundException("Dit offertetraject kon niet worden gevonden.");

        var (user, role) = await currentUserService.GetUserWithRoleAsync(userId);

        if (!tender.IsAccessibleBy(user, role))
            throw new UnauthorizedAccessException("U heeft geen toegang tot dit offertetraject.");

        return await tenderChangeLogRepository.GetByTenderAsync(tenderId);
    }
}
