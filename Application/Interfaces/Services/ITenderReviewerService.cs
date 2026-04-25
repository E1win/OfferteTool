using Domain.Entities;

namespace Application.Interfaces.Services;

public interface ITenderReviewerService
{
    Task<bool> CanReviewTenderAsync(Guid tenderId, string userId);
    Task<List<ApplicationUser>> GetAssignableReviewersAsync(Guid tenderId, string actorUserId);
    Task<List<ApplicationUser>> GetAssignedReviewersAsync(Guid tenderId, string actorUserId);
    Task AddReviewerAsync(Guid tenderId, string reviewerUserId, string actorUserId);
    Task RemoveReviewerAsync(Guid tenderId, string reviewerUserId, string actorUserId);
}
