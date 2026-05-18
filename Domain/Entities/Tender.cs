using Domain.Constants;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using Domain.Exceptions;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Tender
{
    public Guid Id { get; set; }

    [MaxLength(256)]
    public required string Title { get; set; }

    [MaxLength(2048)]
    public required string Description { get; set; }

    public required DateOnly EndDate { get; set; }
    public required TenderStatus Status { get; set; }
    public required bool IsPublic { get; set; }

    public required Guid OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    public List<TenderQuestion> Questions { get; set; } = [];
    public List<TenderSubmission> Submissions { get; set; } = [];
    public List<TenderReviewer> Reviewers { get; set; } = [];
    public List<TenderInvitation> Invitations { get; set; } = [];
    public List<TenderChangeLog> ChangeLogs { get; set; } = [];

    public void ValidateDates()
    {
        if (EndDate <= DateOnly.FromDateTime(DateTime.Today))
            throw new BusinessRuleViolationException("De einddatum moet na vandaag liggen.");
    }

    public bool IsValidOrganisationType(OrganisationType type) => type == OrganisationType.Client;

    public bool IsAccessibleBy(ApplicationUser user, string role) =>
        role switch
        {
            Roles.Inkoper
                => user.OrganisationId is not null && OrganisationId == user.OrganisationId.Value,
            Roles.Beoordelaar
                => CanReview(user),
            Roles.Leverancier
                => Status == TenderStatus.Open && (IsPublic || IsInvitedSupplier(user)),
            _ => false
        };

    public bool CanBeManagedBy(ApplicationUser user, string role) =>
        role == Roles.Inkoper
        && user.OrganisationId is not null
        && OrganisationId == user.OrganisationId.Value;

    public bool CanBeEdited() => Status == TenderStatus.Design;

    public bool CanBeAmended() => Status == TenderStatus.Open;

    public bool CanBeOpened() =>
        Status == TenderStatus.Design
        && Questions.Count > 0;

    public bool CanBeClosed() => Status == TenderStatus.Open;

    public bool CanBeCompleted() => Status == TenderStatus.Closed;
    
    public bool CanBeReviewed() => Status == TenderStatus.Closed;

    public bool HasReviewer(string userId) =>
        !string.IsNullOrWhiteSpace(userId)
        && Reviewers.Any(reviewer => reviewer.UserId == userId);

    public bool HasInvitationForSupplier(Guid supplierOrganisationId) =>
        supplierOrganisationId != Guid.Empty
        && Invitations.Any(invitation => invitation.SupplierOrganisationId == supplierOrganisationId);

    private bool IsInvitedSupplier(ApplicationUser user) =>
        user.OrganisationId is not null
        && HasInvitationForSupplier(user.OrganisationId.Value);

    public bool CanReview(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return CanBeReviewed()
            && HasReviewer(user.Id);
    }

    public void EnsureCanBeReviewed()
    {
        if (!CanBeReviewed())
            throw new BusinessRuleViolationException("Antwoorden kunnen pas worden beoordeeld zodra het offertetraject is gesloten.");
    }

    public void EnsureCanReceiveSubmission(DateOnly today)
    {
        if (Status != TenderStatus.Open)
            throw new BusinessRuleViolationException("Alleen openstaande offertetrajecten kunnen offertes ontvangen.");

        if (EndDate < today)
            throw new BusinessRuleViolationException("De inschrijftermijn is verstreken.");
    }

    public void Open()
    {
        if (!CanBeOpened())
            throw new BusinessRuleViolationException("Alleen offertetrajecten met de status Ontwerp en minimaal een vraag kunnen worden gepubliceerd.");

        Status = TenderStatus.Open;
    }

    public void Close()
    {
        if (!CanBeClosed())
            throw new BusinessRuleViolationException("Alleen openstaande offertetrajecten kunnen worden gesloten.");

        Status = TenderStatus.Closed;
    }

    public void Complete()
    {
        if (!CanBeCompleted())
            throw new BusinessRuleViolationException("Alleen gesloten offertetrajecten kunnen worden afgerond.");

        Status = TenderStatus.Completed;
    }

    public void AddReviewer(string userId)
    {
        if (Status == TenderStatus.Completed)
            throw new BusinessRuleViolationException("Beoordelaars kunnen niet worden toegevoegd aan afgeronde offertetrajecten.");

        if (HasReviewer(userId))
            return;

        Reviewers.Add(new TenderReviewer(userId));
    }

    public void RemoveReviewer(string userId)
    {
        var reviewer = Reviewers.FirstOrDefault(existingReviewer => existingReviewer.UserId == userId);

        if (reviewer is null)
            return;

        Reviewers.Remove(reviewer);
    }
}
