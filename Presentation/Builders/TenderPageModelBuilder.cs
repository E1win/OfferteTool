using Application.Interfaces.Services;
using Domain.Entities;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Presentation.Controllers;
using Presentation.Mappings;
using Presentation.Models.Questionnaire;
using Presentation.Models.Tender;
using Presentation.Models.TenderSubmission;

namespace Presentation.Builders;

public class TenderPageModelBuilder(
    ITenderService tenderService,
    ITenderQuestionService tenderQuestionService,
    ITenderSubmissionService tenderSubmissionService) : ITenderPageModelBuilder
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
        var supplierSubmissions = canManageTender && tender.Status == TenderStatus.Open
            ? await tenderSubmissionService.GetForManagedTenderAsync(id, userId)
            : [];

        return new TenderDetailsViewModel
        {
            Tender = tender,
            CanManageTender = canManageTender,
            ActionErrorMessage = actionErrorMessage,
            SupplierSubmissions = supplierSubmissions
                .Select(submission => new TenderSubmissionSupplierViewModel
                {
                    Name = submission.Supplier?.Name ?? "Onbekende leverancier"
                })
                .ToList(),
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

    public async Task<TenderSubmissionPageViewModel> BuildSubmissionAsync(
        Guid id,
        string userId,
        TenderSubmissionFormViewModel? form = null,
        string? errorMessage = null)
    {
        var tender = await tenderService.GetAccessibleTenderByIdAsync(id, userId);
        var questions = await tenderQuestionService.GetQuestionsAsync(id, userId)
            ?? [];
        var persistedForm = form;

        if (persistedForm is null)
        {
            var submission = await tenderSubmissionService.GetByTenderForCurrentSupplierAsync(id, userId);
            if (submission is not null)
                persistedForm = TenderSubmissionMapper.ToFormViewModel(submission.Answers);
        }

        var orderedQuestions = questions.OrderBy(question => question.Order).ToList();
        var normalizedForm = CreateSubmissionForm(orderedQuestions, persistedForm);

        return new TenderSubmissionPageViewModel
        {
            Tender = tender,
            Questions = CreateSubmissionQuestions(orderedQuestions, normalizedForm),
            Form = normalizedForm,
            ErrorMessage = errorMessage
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

    private static TenderSubmissionFormViewModel CreateSubmissionForm(
        IEnumerable<TenderQuestion> questions,
        TenderSubmissionFormViewModel? existingForm = null)
    {
        var existingAnswers = existingForm?.Answers
            .GroupBy(answer => answer.QuestionId)
            .ToDictionary(group => group.Key, group => group.First())
            ?? [];

        return new TenderSubmissionFormViewModel
        {
            Answers = questions
                .OrderBy(question => question.Order)
                .Select(question =>
                {
                    if (existingAnswers.TryGetValue(question.Id, out var existingAnswer))
                    {
                        return new TenderSubmissionAnswerInputModel
                        {
                            QuestionId = question.Id,
                            Type = question.Type,
                            TextValue = existingAnswer.TextValue,
                            NumericValue = existingAnswer.NumericValue,
                            SelectedOptionIds = existingAnswer.SelectedOptionIds.ToList()
                        };
                    }

                    return new TenderSubmissionAnswerInputModel
                    {
                        QuestionId = question.Id,
                        Type = question.Type
                    };
                })
                .ToList()
        };
    }

    private static List<TenderSubmissionQuestionViewModel> CreateSubmissionQuestions(
        IReadOnlyList<TenderQuestion> questions,
        TenderSubmissionFormViewModel form)
    {
        return questions
            .Select((question, index) => CreateSubmissionQuestion(question, form.Answers[index], index))
            .ToList();
    }

    private static TenderSubmissionQuestionViewModel CreateSubmissionQuestion(
        TenderQuestion question,
        TenderSubmissionAnswerInputModel answer,
        int index)
    {
        return question switch
        {
            TextQuestion textQuestion => new TextTenderSubmissionQuestionViewModel
            {
                Index = index,
                Text = textQuestion.Text,
                Answer = answer,
                Rows = textQuestion.Rows,
                MaxLength = textQuestion.MaxLength
            },
            NumberQuestion numberQuestion => new NumberTenderSubmissionQuestionViewModel
            {
                Index = index,
                Text = numberQuestion.Text,
                Answer = answer,
                MinValue = numberQuestion.MinValue,
                MaxValue = numberQuestion.MaxValue
            },
            ChoiceQuestion choiceQuestion => new ChoiceTenderSubmissionQuestionViewModel
            {
                Index = index,
                Text = choiceQuestion.Text,
                Answer = answer,
                AllowMultipleSelection = choiceQuestion.AllowMultipleSelection,
                Options = choiceQuestion.Options
                    .OrderBy(option => option.Order)
                    .Select(option => new TenderSubmissionOptionViewModel
                    {
                        Id = option.Id,
                        Text = option.Text
                    })
                    .ToList()
            },
            _ => throw new InvalidOperationException("Het gekozen vraagtype wordt niet ondersteund.")
        };
    }
}
