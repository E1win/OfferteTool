using Domain.Constants;
using Domain.Entities.TenderQuestions;
using Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace Domain.Entities;

public class Tender
{
    public Guid Id { get; set; }

    [MaxLength(256)]
    public required string Title { get; set; }

    [MaxLength(2048)]
    public required string Description { get; set; }

    public required DateOnly StartDate { get; set; }
    public required DateOnly EndDate { get; set; }
    public required TenderStatus Status { get; set; }
    public required bool IsPublic { get; set; }

    public required Guid OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }

    public List<TenderQuestion> Questions { get; set; } = [];
    public List<TenderSubmission> Submissions { get; set; } = [];

    public void ValidateDates()
    {
        if (EndDate <= StartDate)
            throw new InvalidOperationException("De einddatum moet na de begindatum liggen.");

        if (StartDate < DateOnly.FromDateTime(DateTime.Today))
            throw new InvalidOperationException("De begindatum mag niet voor vandaag liggen.");
    }

    public bool IsValidOrganisationType(OrganisationType type) => type == OrganisationType.Client;

    public bool IsAccessibleBy(ApplicationUser user, string role) =>
        role switch
        {
            Roles.Inkoper or Roles.Beoordelaar
                => user.OrganisationId is not null && OrganisationId == user.OrganisationId.Value,
            Roles.Leverancier
                => IsPublic && Status == TenderStatus.Open,
            _ => false
        };

    public bool CanBeEdited() => Status == TenderStatus.Design;
}
