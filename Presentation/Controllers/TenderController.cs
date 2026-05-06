using Application.Interfaces.Services;
using Domain.Constants;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Builders;
using Presentation.Mappings;
using Presentation.Models.Tender;
using Presentation.Models.TenderSubmission;

namespace Presentation.Controllers;

public class TenderController(
    ITenderService tenderService,
    ITenderReviewerService tenderReviewerService,
    ITenderSubmissionService tenderSubmissionService,
    ITenderPageModelBuilder tenderPageModelBuilder,
    ITenderComparisonPageModelBuilder tenderComparisonPageModelBuilder) : AuthenticatedControllerBase
{
    public async Task<IActionResult> Index()
    {
        return View(await tenderPageModelBuilder.BuildIndexAsync(UserId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Inkoper)]
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
    [Authorize(Roles = Roles.Inkoper)]
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
    [Authorize(Roles = Roles.Inkoper)]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Inkoper)]
    public async Task<IActionResult> Close(Guid id)
    {
        try
        {
            await tenderService.CloseTenderAsync(id, UserId);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Details), await tenderPageModelBuilder.BuildDetailsAsync(id, UserId, actionErrorMessage: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Inkoper)]
    public async Task<IActionResult> Complete(Guid id)
    {
        try
        {
            await tenderService.CompleteTenderAsync(id, UserId);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Details), await tenderPageModelBuilder.BuildDetailsAsync(id, UserId, actionErrorMessage: ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Inkoper)]
    public async Task<IActionResult> UpdateReviewers(Guid id, [Bind(Prefix = "ReviewerAssignmentModal.Form")] TenderReviewerAssignmentFormViewModel model)
    {
        var reviewers = model.Reviewers ?? [];
        var selectedReviewerIds = reviewers
            .Where(reviewer => reviewer.IsSelected)
            .Select(reviewer => reviewer.UserId)
            .ToHashSet();

        try
        {
            var assignedReviewers = await tenderReviewerService.GetAssignedReviewersAsync(id, UserId);
            var assignedReviewerIds = assignedReviewers
                .Select(reviewer => reviewer.Id)
                .ToHashSet();

            foreach (var reviewerId in assignedReviewerIds.Except(selectedReviewerIds))
                await tenderReviewerService.RemoveReviewerAsync(id, reviewerId, UserId);

            foreach (var reviewerId in selectedReviewerIds.Except(assignedReviewerIds))
                await tenderReviewerService.AddReviewerAsync(id, reviewerId, UserId);

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Details), await tenderPageModelBuilder.BuildDetailsAsync(
                id,
                UserId,
                reviewerAssignmentForm: model,
                openReviewerAssignmentModal: true,
                reviewerErrorMessage: ex.Message));
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        return View(await tenderPageModelBuilder.BuildDetailsAsync(id, UserId));
    }

    [HttpGet]
    public async Task<IActionResult> Changes(Guid id)
    {
        return View(await tenderPageModelBuilder.BuildChangeLogAsync(id, UserId));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Inkoper)]
    public async Task<IActionResult> Comparison(Guid id)
    {
        return View(await tenderComparisonPageModelBuilder.BuildComparisonAsync(id, UserId));
    }

    [HttpGet("Tender/{tenderId:guid}/Submissions/{submissionId:guid}/Comparison")]
    [Authorize(Roles = Roles.Inkoper)]
    public async Task<IActionResult> SubmissionComparison(Guid tenderId, Guid submissionId)
    {
        return View(await tenderComparisonPageModelBuilder.BuildSubmissionComparisonAsync(tenderId, submissionId, UserId));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Leverancier)]
    public async Task<IActionResult> Submit(Guid id)
    {
        return View(await tenderPageModelBuilder.BuildSubmissionAsync(id, UserId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Roles.Leverancier)]
    public async Task<IActionResult> Submit(Guid id, [Bind(Prefix = "Form")] TenderSubmissionFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(await tenderPageModelBuilder.BuildSubmissionAsync(id, UserId, model));

        try
        {
            await tenderSubmissionService.SubmitAsync(id, TenderSubmissionMapper.ToEntities(model), UserId);
            TempData["TenderSubmissionSuccess"] = "Uw offerte is succesvol ingediend.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(await tenderPageModelBuilder.BuildSubmissionAsync(id, UserId, model, ex.Message));
        }
    }
}
