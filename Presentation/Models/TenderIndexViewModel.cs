using Domain.Entities;

namespace Presentation.Models;

public class TenderIndexViewModel
{
    public List<Tender> Tenders { get; set; } = [];
    public TenderFormModalViewModel CreateTenderModal { get; set; } = new();
}
