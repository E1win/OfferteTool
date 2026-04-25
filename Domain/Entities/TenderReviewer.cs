using System.ComponentModel.DataAnnotations;
using Domain.Exceptions;

namespace Domain.Entities;

public class TenderReviewer
{
    private TenderReviewer()
    {
    }

    public TenderReviewer(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new BusinessRuleViolationException("De beoordelaar is ongeldig.");

        UserId = userId;
    }

    public Guid Id { get; private set; }

    [MaxLength(450)]
    public string UserId { get; private set; } = string.Empty;

    public Guid TenderId { get; set; }
    public Tender? Tender { get; set; }
}
