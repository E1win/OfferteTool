using Domain.Entities;

namespace Presentation.Models;

public class TenderDetailsViewModel
{
    public required Tender Tender { get; init; }
    public TenderFormModalViewModel? EditTenderModal { get; init; }
}
