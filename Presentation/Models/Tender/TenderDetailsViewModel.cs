using Presentation.Models.Questionnaire;
using TenderEntity = Domain.Entities.Tender;

namespace Presentation.Models.Tender;

public class TenderDetailsViewModel
{
    public required TenderEntity Tender { get; init; }
    public required bool CanManageTender { get; init; }
    public string? ActionErrorMessage { get; init; }
    public TenderFormModalViewModel? EditTenderModal { get; init; }
    public required QuestionnaireEditorBootstrapViewModel QuestionnaireEditor { get; init; }
}
