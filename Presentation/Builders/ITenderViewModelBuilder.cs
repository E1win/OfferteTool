using Presentation.Models.Tender;

namespace Presentation.Builders;

public interface ITenderViewModelBuilder
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
}
