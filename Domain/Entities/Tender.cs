using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

public class Tender
{
    public required Guid Id { get; set; }

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
    
    // EndDate has to be at least one day before StartDate
    public bool HasValidDateRange() => EndDate > StartDate;

    public bool IsValidOrganisationType(OrganisationType type) => type == OrganisationType.Client;
}
