using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Entities;

public class SecurityAuditLog
{
    public Guid Id { get; set; }

    public required DateTimeOffset OccurredAtUtc { get; set; }

    public required SecurityAuditEventType EventType { get; set; }

    public required SecurityAuditOutcome Outcome { get; set; }

    [MaxLength(450)]
    public string? ActorUserId { get; set; }

    [MaxLength(256)]
    public string? ActorIdentifier { get; set; }

    [MaxLength(450)]
    public string? TargetUserId { get; set; }

    public Guid? TargetOrganisationId { get; set; }

    [MaxLength(64)]
    public string? IpAddress { get; set; }

    [MaxLength(512)]
    public string? UserAgent { get; set; }

    [MaxLength(128)]
    public string? TraceId { get; set; }

    [MaxLength(4096)]
    public string? DetailsJson { get; set; }
}
