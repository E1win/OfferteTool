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
        string? actionErrorMessage = null);

    Task<TenderSubmissionPageViewModel> BuildSubmissionAsync(
        Guid id,
        string userId,
        TenderSubmissionFormViewModel? form = null,
        string? errorMessage = null);
}
