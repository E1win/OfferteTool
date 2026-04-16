using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Enums;
using Presentation.Controllers;
using Presentation.Mappings;
using Presentation.Models.Questionnaire;
using Presentation.Models.Tender;

namespace Presentation.Builders;

public class TenderViewModelBuilder(ITenderService tenderService) : ITenderViewModelBuilder
{
    public async Task<TenderIndexViewModel> BuildIndexAsync(
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
                SubmitAction = nameof(TenderController.Create),
                SubmitButtonText = "Tender aanmaken",
                ErrorMessage = errorMessage,
                ShowOnLoad = openCreateTenderModal,
                Form = createTender ?? new TenderFormViewModel()
            }
        };
    }

    public async Task<TenderDetailsViewModel> BuildDetailsAsync(
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
                    SubmitAction = nameof(TenderController.Edit),
                    SubmitButtonText = "Wijzigingen opslaan",
                    ErrorMessage = errorMessage,
                    ShowOnLoad = openEditTenderModal,
                    TenderId = tender.Id,
                    Form = editTender ?? TenderMapper.ToFormViewModel(tender)
                }
                : null
        };
    }

    private static ConfirmationModalViewModel? CreateOpenTenderModal(Tender tender, bool canEditTender)
    {
        if (!canEditTender)
            return null;

        return new ConfirmationModalViewModel
        {
            ModalId = "openTenderModal",
            ModalTitle = "Offertetraject openen",
            Description = "Weet u zeker dat u dit offertetraject wilt openen? Zodra het traject open staat, kunnen de tendergegevens en vragenlijst niet meer worden gewijzigd.",
            SubmitAction = nameof(TenderController.Open),
            SubmitButtonText = "Offertetraject openen",
            TenderId = tender.Id
        };
    }
}
