using TenderEntity = Domain.Entities.Tender;

namespace Presentation.Models.Tender;

public class TenderIndexViewModel
{
    public List<TenderEntity> Tenders { get; set; } = [];
    public TenderFormModalViewModel CreateTenderModal { get; set; } = new();
}
