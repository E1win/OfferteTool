using Application.Interfaces.Services;
using Application.Models.UserManagement;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using Presentation.Models.UserManagement;

namespace Presentation.Builders;

public class UserManagementPageModelBuilder(IUserManagementService userManagementService) : IUserManagementPageModelBuilder
{
    public async Task<UserManagementIndexViewModel> BuildIndexAsync(
        string? search = null,
        UserFormViewModel? createUser = null,
        bool openCreateUserModal = false,
        string? createErrorMessage = null,
        UserFormViewModel? editUser = null,
        bool openEditUserModal = false,
        string? editErrorMessage = null)
    {
        var users = await userManagementService.GetUsersAsync(new UserManagementQuery
        {
            Search = search
        });

        return new UserManagementIndexViewModel
        {
            Search = search?.Trim() ?? string.Empty,
            Users = users,
            CreateUserModal = await CreateUserModalAsync(createUser, openCreateUserModal, createErrorMessage),
            EditUserModal = await EditUserModalAsync(editUser, openEditUserModal, editErrorMessage)
        };
    }

    private async Task<UserFormModalViewModel> CreateUserModalAsync(
        UserFormViewModel? form,
        bool showOnLoad,
        string? errorMessage)
    {
        return new UserFormModalViewModel
        {
            ModalId = "createUserModal",
            ModalTitle = "Nieuwe gebruiker aanmaken",
            SubmitAction = "Create",
            SubmitButtonText = "Gebruiker aanmaken",
            ErrorMessage = errorMessage,
            ShowOnLoad = showOnLoad,
            ShowIsActive = false,
            AllowOrganisationChange = true,
            Form = await BuildUserFormAsync(form)
        };
    }

    private async Task<UserFormModalViewModel> EditUserModalAsync(
        UserFormViewModel? form,
        bool showOnLoad,
        string? errorMessage)
    {
        return new UserFormModalViewModel
        {
            ModalId = "editUserModal",
            ModalTitle = "Gebruiker wijzigen",
            SubmitAction = "Edit",
            SubmitButtonText = "Wijzigingen opslaan",
            ErrorMessage = errorMessage,
            ShowOnLoad = showOnLoad,
            ShowIsActive = true,
            AllowOrganisationChange = false,
            Form = await BuildUserFormAsync(form)
        };
    }

    private async Task<UserFormViewModel> BuildUserFormAsync(UserFormViewModel? form = null)
    {
        var model = form ?? new UserFormViewModel();
        var options = await userManagementService.GetFormOptionsAsync();

        model.RoleOptions = options.Roles
            .Select(role => new SelectListItem(role.Value, role.Value, role.Value == model.Role))
            .ToList();

        model.OrganisationOptions = options.Organisations
            .Select(organisation => new SelectListItem(
                $"{organisation.Name} ({FormatOrganisationType(organisation.OrganisationType)})",
                organisation.Id.ToString(),
                organisation.Id == model.OrganisationId))
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
