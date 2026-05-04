using Application.Interfaces.Services;
using Application.Models.OrganisationManagement;
using Domain.Constants;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Builders;
using Presentation.Models.OrganisationManagement;

namespace Presentation.Controllers;

[Authorize(Roles = Roles.Beheerder)]
public class OrganisationManagementController(
    IOrganisationManagementService organisationManagementService,
    IOrganisationManagementPageModelBuilder organisationManagementPageModelBuilder) : AuthenticatedControllerBase
{
    public async Task<IActionResult> Index(string? search, bool includeInactive = false)
    {
        return View(await organisationManagementPageModelBuilder.BuildIndexAsync(search, includeInactive));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateOrganisationModal.Form")] OrganisationFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await organisationManagementPageModelBuilder.BuildIndexAsync(
                createOrganisation: model,
                openCreateOrganisationModal: true));

        try
        {
            await organisationManagementService.CreateOrganisationAsync(new CreateOrganisationRequest
            {
                Name = model.Name,
                KvkNumber = model.KvkNumber,
                OrganisationType = model.OrganisationType!.Value
            }, UserId);

            TempData["OrganisationManagementSuccess"] = "De organisatie is aangemaakt.";
            return RedirectToAction(nameof(Index));
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Index), await organisationManagementPageModelBuilder.BuildIndexAsync(
                createOrganisation: model,
                openCreateOrganisationModal: true,
                createErrorMessage: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Bind(Prefix = "EditOrganisationModal.Form")] OrganisationFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await organisationManagementPageModelBuilder.BuildIndexAsync(
                includeInactive: true,
                editOrganisation: model,
                openEditOrganisationModal: true));

        try
        {
            await organisationManagementService.UpdateOrganisationAsync(new UpdateOrganisationRequest
            {
                Id = model.Id ?? Guid.Empty,
                Name = model.Name,
                KvkNumber = model.KvkNumber,
                OrganisationType = model.OrganisationType!.Value,
                IsActive = model.IsActive
            }, UserId);

            TempData["OrganisationManagementSuccess"] = model.IsActive
                ? "De organisatie is bijgewerkt."
                : "De organisatie en gekoppelde gebruikers zijn uitgeschakeld.";
            return RedirectToAction(nameof(Index), new { includeInactive = true });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Index), await organisationManagementPageModelBuilder.BuildIndexAsync(
                includeInactive: true,
                editOrganisation: model,
                openEditOrganisationModal: true,
                editErrorMessage: ex.Message));
        }
    }
}
