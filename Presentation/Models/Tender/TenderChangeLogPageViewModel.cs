using TenderEntity = Domain.Entities.Tender;

namespace Presentation.Models.Tender;

public class TenderChangeLogPageViewModel
{
    public required TenderEntity Tender { get; init; }
    public required IReadOnlyList<TenderChangeLogViewModel> ChangeLogs { get; init; }
}
