using Domain.Enums;

namespace Application.Models.SecurityAudit;

public class SecurityAuditEvent
{
    public required SecurityAuditEventType EventType { get; init; }

    public required SecurityAuditOutcome Outcome { get; init; }

    public string? ActorUserId { get; init; }

    public string? ActorIdentifier { get; init; }

    public string? TargetUserId { get; init; }

    public Guid? TargetOrganisationId { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public string? TraceId { get; init; }

    public IReadOnlyDictionary<string, string> Details { get; init; } = new Dictionary<string, string>();
}
