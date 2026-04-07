using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

public class Organisation
{
    public required Guid Id { get; set; }

    [MaxLength(256)]
    public required string Name { get; set; }

    [MaxLength(8)]
    public required string KvkNumber { get; set; }

    public required OrganisationType OrganisationType { get; set; }
}
