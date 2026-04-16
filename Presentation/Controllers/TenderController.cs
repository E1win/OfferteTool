using Application.Interfaces.Services;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.Builders;
using Presentation.Mappings;
using Presentation.Models.Tender;

namespace Presentation.Controllers;

public class TenderController(
    ITenderService tenderService,
    ITenderPageModelBuilder tenderPageModelBuilder) : AuthenticatedControllerBase
{
    public async Task<IActionResult> Index()
    {
        return View(await tenderPageModelBuilder.BuildIndexAsync(UserId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateTenderModal.Form")] TenderFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await tenderPageModelBuilder.BuildIndexAsync(UserId, model, true));

        var tender = TenderMapper.ToEntity(model);

        try
        {
            var createdTender = await tenderService.CreateTenderAsync(tender, UserId);
            return RedirectToAction(nameof(Details), new { id = createdTender.Id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Index), await tenderPageModelBuilder.BuildIndexAsync(UserId, model, true, ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = "EditTenderModal.Form")] TenderFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Details), await tenderPageModelBuilder.BuildDetailsAsync(id, UserId, model, true));

        try
        {
            await tenderService.UpdateTenderAsync(id, TenderMapper.ToEntity(model), UserId);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Details), await tenderPageModelBuilder.BuildDetailsAsync(id, UserId, model, true, ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(Guid id)
    {
        try
        {
            await tenderService.OpenTenderAsync(id, UserId);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Details), await tenderPageModelBuilder.BuildDetailsAsync(id, UserId, actionErrorMessage: ex.Message));
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        return View(await tenderPageModelBuilder.BuildDetailsAsync(id, UserId));
    }
}
