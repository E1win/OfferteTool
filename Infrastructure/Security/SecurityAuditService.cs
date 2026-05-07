using System.Text.Json;
using Application.Interfaces.Services;
using Application.Models.SecurityAudit;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Security;

public class SecurityAuditService(
    AppDbContext dbContext,
    ILogger<SecurityAuditService> logger) : ISecurityAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task LogAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        await dbContext.SecurityAuditLogs.AddAsync(CreateLog(auditEvent), cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task TryLogAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await LogAsync(auditEvent, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Security audit logging failed for event type {EventType}.", auditEvent.EventType);
        }
    }

    private static SecurityAuditLog CreateLog(SecurityAuditEvent auditEvent) =>
        new()
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = DateTimeOffset.UtcNow,
            EventType = auditEvent.EventType,
            Outcome = auditEvent.Outcome,
            ActorUserId = Truncate(auditEvent.ActorUserId, 450),
            ActorIdentifier = Truncate(auditEvent.ActorIdentifier, 256),
            TargetUserId = Truncate(auditEvent.TargetUserId, 450),
            TargetOrganisationId = auditEvent.TargetOrganisationId,
            IpAddress = Truncate(auditEvent.IpAddress, 64),
            UserAgent = Truncate(auditEvent.UserAgent, 512),
            TraceId = Truncate(auditEvent.TraceId, 128),
            DetailsJson = SerializeDetails(auditEvent.Details)
        };

    private static string? SerializeDetails(IReadOnlyDictionary<string, string> details)
    {
        if (details.Count == 0)
            return null;

        var safeDetails = details
            .Where(detail => !string.IsNullOrWhiteSpace(detail.Key))
            .ToDictionary(
                detail => Truncate(detail.Key, 128)!,
                detail => Truncate(detail.Value, 512) ?? string.Empty);

        return Truncate(JsonSerializer.Serialize(safeDetails, JsonOptions), 4096);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }
}
