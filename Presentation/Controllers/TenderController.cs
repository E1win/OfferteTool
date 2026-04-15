using System.Security.Claims;
using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models.Questionnaire;
using Presentation.Models.Tender;

namespace Presentation.Controllers;

[Authorize]
public class TenderController(ITenderService tenderService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await BuildTenderIndexViewModelAsync(userId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateTenderModal.Form")] TenderFormViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
            return View(nameof(Index), await BuildTenderIndexViewModelAsync(userId, model, true));

        var tender = MapToTender(model);

        try
        {
            var createdTender = await tenderService.CreateTenderAsync(tender, userId);
            return RedirectToAction(nameof(Details), new { id = createdTender.Id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Index), await BuildTenderIndexViewModelAsync(userId, model, true, ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = "EditTenderModal.Form")] TenderFormViewModel model)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        if (!ModelState.IsValid)
            return View(nameof(Details), await BuildTenderDetailsViewModelAsync(id, userId, model, true));

        try
        {
            await tenderService.UpdateTenderAsync(id, MapToTender(model), userId);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Details), await BuildTenderDetailsViewModelAsync(id, userId, model, true, ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Open(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        try
        {
            await tenderService.OpenTenderAsync(id, userId);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Details), await BuildTenderDetailsViewModelAsync(id, userId, actionErrorMessage: ex.Message));
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return View(await BuildTenderDetailsViewModelAsync(id, userId));
    }

    private async Task<TenderIndexViewModel> BuildTenderIndexViewModelAsync(
        string userId,
        TenderFormViewModel? createTender = null,
        bool openCreateTenderModal = false,
        string? errorMessage = null)
    {
        return new TenderIndexViewModel
        {
            Tenders = await tenderService.GetAccessibleTendersAsync(userId),
            CreateTenderModal = new TenderFormModalViewModel
            {
                ModalId = "createTenderModal",
                ModalTitle = "Nieuwe tender aanmaken",
                SubmitAction = nameof(Create),
                SubmitButtonText = "Tender aanmaken",
                ErrorMessage = errorMessage,
                ShowOnLoad = openCreateTenderModal,
                Form = createTender ?? new TenderFormViewModel()
            }
        };
    }

    private async Task<TenderDetailsViewModel> BuildTenderDetailsViewModelAsync(
        Guid id,
        string userId,
        TenderFormViewModel? editTender = null,
        bool openEditTenderModal = false,
        string? errorMessage = null,
        string? actionErrorMessage = null)
    {
        var canManageTender = await tenderService.CanManageTenderAsync(id, userId);
        var tender = await tenderService.GetAccessibleTenderByIdAsync(id, userId);
        var canEditTender = canManageTender && tender.CanBeEdited();

        return new TenderDetailsViewModel
        {
            Tender = tender,
            CanManageTender = canManageTender,
            ActionErrorMessage = actionErrorMessage,
            OpenTenderModal = CreateOpenTenderModal(tender, canEditTender),
            QuestionnaireEditor = new QuestionnaireEditorBootstrapViewModel
            {
                ApiBaseUrl = $"/api/tenders/{tender.Id}/questionnaire",
                CanManageQuestions = canEditTender,
                AntiforgeryHeaderName = "X-CSRF-TOKEN",
                QuestionTypes = new QuestionnaireQuestionTypeLookupViewModel
                {
                    Choice = QuestionType.Choice,
                    Text = QuestionType.Text,
                    Numeric = QuestionType.Numeric
                }
            },
            EditTenderModal = canEditTender
                ? new TenderFormModalViewModel
                {
                    ModalId = "editTenderModal",
                    ModalTitle = "Tender wijzigen",
                    SubmitAction = nameof(Edit),
                    SubmitButtonText = "Wijzigingen opslaan",
                    ErrorMessage = errorMessage,
                    ShowOnLoad = openEditTenderModal,
                    TenderId = tender.Id,
                    Form = editTender ?? MapToTenderForm(tender)
                }
                : null
        };
    }

    private ConfirmationModalViewModel? CreateOpenTenderModal(Tender tender, bool canEditTender)
    {
        if (!canEditTender)
            return null;

        return new ConfirmationModalViewModel
        {
            ModalId = "openTenderModal",
            ModalTitle = "Offertetraject openen",
            Description = "Weet u zeker dat u dit offertetraject wilt openen? Zodra het traject open staat, kunnen de tendergegevens en vragenlijst niet meer worden gewijzigd.",
            SubmitAction = nameof(Open),
            SubmitButtonText = "Offertetraject openen",
            TenderId = tender.Id
        };
    }

    private static Tender MapToTender(TenderFormViewModel model)
    {
        return new Tender
        {
            Title = model.Title,
            Description = model.Description,
            StartDate = model.StartDate,
            EndDate = model.EndDate,
            IsPublic = model.IsPublic,
            Status = TenderStatus.Design,
            OrganisationId = Guid.Empty
        };
    }

    private static TenderFormViewModel MapToTenderForm(Tender tender)
    {
        return new TenderFormViewModel
        {
            Title = tender.Title,
            Description = tender.Description,
            StartDate = tender.StartDate,
            EndDate = tender.EndDate,
            IsPublic = tender.IsPublic
        };
    }
}
