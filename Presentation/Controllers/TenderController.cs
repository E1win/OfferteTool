using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Presentation.Mappings;
using Presentation.Models.Questionnaire;
using Presentation.Models.Tender;

namespace Presentation.Controllers;

public class TenderController(
    ITenderService tenderService) : AuthenticatedControllerBase
{
    public async Task<IActionResult> Index()
    {
        return View(await BuildTenderIndexViewModelAsync(UserId));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "CreateTenderModal.Form")] TenderFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Index), await BuildTenderIndexViewModelAsync(UserId, model, true));

        var tender = TenderMapper.ToEntity(model);

        try
        {
            var createdTender = await tenderService.CreateTenderAsync(tender, UserId);
            return RedirectToAction(nameof(Details), new { id = createdTender.Id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Index), await BuildTenderIndexViewModelAsync(UserId, model, true, ex.Message));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, [Bind(Prefix = "EditTenderModal.Form")] TenderFormViewModel model)
    {
        if (!ModelState.IsValid)
            return View(nameof(Details), await BuildTenderDetailsViewModelAsync(id, UserId, model, true));

        try
        {
            await tenderService.UpdateTenderAsync(id, TenderMapper.ToEntity(model), UserId);
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleViolationException ex)
        {
            return View(nameof(Details), await BuildTenderDetailsViewModelAsync(id, UserId, model, true, ex.Message));
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
            return View(nameof(Details), await BuildTenderDetailsViewModelAsync(id, UserId, actionErrorMessage: ex.Message));
        }
    }

    public async Task<IActionResult> Details(Guid id)
    {
        return View(await BuildTenderDetailsViewModelAsync(id, UserId));
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
                    Form = editTender ?? TenderMapper.ToFormViewModel(tender)
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
}
