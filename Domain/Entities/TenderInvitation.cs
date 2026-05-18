using System.ComponentModel.DataAnnotations;
using Domain.Exceptions;

namespace Domain.Entities;

public class TenderInvitation
{
    private TenderInvitation()
    {
    }

    public TenderInvitation(Guid supplierOrganisationId, string invitedByUserId, DateTimeOffset invitedAtUtc)
    {
        if (supplierOrganisationId == Guid.Empty)
            throw new BusinessRuleViolationException("De uitgenodigde leverancier is ongeldig.");

        if (string.IsNullOrWhiteSpace(invitedByUserId))
            throw new BusinessRuleViolationException("De uitnodigende gebruiker is ongeldig.");

        SupplierOrganisationId = supplierOrganisationId;
        InvitedByUserId = invitedByUserId;
        InvitedAtUtc = invitedAtUtc;
    }

    public Guid Id { get; private set; }

    public Guid TenderId { get; set; }
    public Tender? Tender { get; set; }

    public Guid SupplierOrganisationId { get; private set; }
    public Organisation? SupplierOrganisation { get; private set; }

    [MaxLength(450)]
    public string InvitedByUserId { get; private set; } = string.Empty;
    public ApplicationUser? InvitedByUser { get; private set; }

    public DateTimeOffset InvitedAtUtc { get; private set; }
}
