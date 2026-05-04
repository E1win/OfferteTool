using Application.Interfaces.Services;
using Application.Models.UserManagement;
using Domain.Constants;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Builders;
using Presentation.Models.UserManagement;

namespace Presentation.Controllers;

[Authorize(Roles = Roles.Beheerder)]
public class UserManagementController(
    IUserManagementService userManagementService,
    IUserManagementPageModelBuilder userManagementPageModelBuilder) : AuthenticatedControllerBase
{
    public async Task<IActionResult> Index(string? search)
    {
        return View(await userManagementPageModelBuilder.BuildIndexAsync(search));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateUserModal.Form")] UserFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await userManagementPageModelBuilder.BuildIndexAsync(
                createUser: model,
                openCreateUserModal: true));

        try
        {
            await userManagementService.CreateUserAsync(new CreateUserRequest
            {
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                OrganisationId = model.OrganisationId
            }, UserId);

            TempData["UserManagementSuccess"] = "De gebruiker is aangemaakt en heeft een e-mail met inloggegevens ontvangen.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Index), await userManagementPageModelBuilder.BuildIndexAsync(
                createUser: model,
                openCreateUserModal: true,
                createErrorMessage: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "EditUserModal.Form")] UserFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await userManagementPageModelBuilder.BuildIndexAsync(
                editUser: model,
                openEditUserModal: true));

        try
        {
            await userManagementService.UpdateUserAsync(new UpdateUserRequest
            {
                UserId = model.UserId ?? string.Empty,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Role = model.Role,
                IsActive = model.IsActive
            }, UserId);

            TempData["UserManagementSuccess"] = "De gebruiker is bijgewerkt.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Index), await userManagementPageModelBuilder.BuildIndexAsync(
                editUser: model,
                openEditUserModal: true,
                editErrorMessage: ex.Message));
        }
    }
}
