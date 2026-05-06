using Presentation.Models.Tender;
using Presentation.Models.TenderSubmission;

namespace Presentation.Builders;

public interface ITenderPageModelBuilder
{
    Task<TenderIndexViewModel> BuildIndexAsync(
        string userId,
        TenderFormViewModel? createTender = null,
        bool openCreateTenderModal = false,
        string? errorMessage = null);

    Task<TenderDetailsViewModel> BuildDetailsAsync(
        Guid id,
        string userId,
        TenderFormViewModel? editTender = null,
        bool openEditTenderModal = false,
        string? errorMessage = null,
        TenderReviewerAssignmentFormViewModel? reviewerAssignmentForm = null,
        bool openReviewerAssignmentModal = false,
        string? reviewerErrorMessage = null,
        string? actionErrorMessage = null);

    Task<TenderChangeLogPageViewModel> BuildChangeLogAsync(Guid id, string userId);

    Task<TenderSubmissionPageViewModel> BuildSubmissionAsync(
        Guid id,
        string userId,
        TenderSubmissionFormViewModel? form = null,
        string? errorMessage = null);
}
