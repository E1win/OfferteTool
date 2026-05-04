using Application.Interfaces.Services;
using Application.Models.OrganisationManagement;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using Presentation.Models.OrganisationManagement;

namespace Presentation.Builders;

public class OrganisationManagementPageModelBuilder(
    IOrganisationManagementService organisationManagementService) : IOrganisationManagementPageModelBuilder
{
    public async Task<OrganisationManagementIndexViewModel> BuildIndexAsync(
        string? search = null,
        bool includeInactive = false,
        OrganisationFormViewModel? createOrganisation = null,
        bool openCreateOrganisationModal = false,
        string? createErrorMessage = null,
        OrganisationFormViewModel? editOrganisation = null,
        bool openEditOrganisationModal = false,
        string? editErrorMessage = null)
    {
        var organisations = await organisationManagementService.GetOrganisationsAsync(new OrganisationManagementQuery
        {
            Search = search,
            IncludeInactive = includeInactive
        });

        return new OrganisationManagementIndexViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            IncludeInactive = includeInactive,
            Organisations = organisations,
            CreateOrganisationModal = CreateOrganisationModal(createOrganisation, openCreateOrganisationModal, createErrorMessage),
            EditOrganisationModal = EditOrganisationModal(editOrganisation, openEditOrganisationModal, editErrorMessage)
        };
    }

    private OrganisationFormModalViewModel CreateOrganisationModal(
        OrganisationFormViewModel? form,
        bool showOnLoad,
        string? errorMessage)
    {
        return new OrganisationFormModalViewModel
        {
            ModalId = "createOrganisationModal",
            ModalTitle = "Nieuwe organisatie aanmaken",
            SubmitAction = "Create",
            SubmitButtonText = "Organisatie aanmaken",
            ErrorMessage = errorMessage,
            ShowOnLoad = showOnLoad,
            ShowIsActive = false,
            Form = BuildOrganisationForm(form)
        };
    }

    private OrganisationFormModalViewModel EditOrganisationModal(
        OrganisationFormViewModel? form,
        bool showOnLoad,
        string? errorMessage)
    {
        return new OrganisationFormModalViewModel
        {
            ModalId = "editOrganisationModal",
            ModalTitle = "Organisatie wijzigen",
            SubmitAction = "Edit",
            SubmitButtonText = "Wijzigingen opslaan",
            ErrorMessage = errorMessage,
            ShowOnLoad = showOnLoad,
            ShowIsActive = true,
            Form = BuildOrganisationForm(form)
        };
    }

    private static OrganisationFormViewModel BuildOrganisationForm(OrganisationFormViewModel? form = null)
    {
        var model = form ?? new OrganisationFormViewModel();

        model.OrganisationTypeOptions = Enum.GetValues<OrganisationType>()
            .Select(organisationType => new SelectListItem(
                FormatOrganisationType(organisationType),
                organisationType.ToString(),
                organisationType == model.OrganisationType))
            .ToList();

        return model;
    }

    private static string FormatOrganisationType(OrganisationType organisationType) =>
        organisationType switch
        {
            OrganisationType.Client => "Opdrachtgever",
            OrganisationType.Supplier => "Leverancier",
            _ => organisationType.ToString()
        };
}
