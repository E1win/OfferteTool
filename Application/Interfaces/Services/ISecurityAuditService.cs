using Application.Models.SecurityAudit;

namespace Application.Interfaces.Services;

public interface ISecurityAuditService
{
    Task LogAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default);
    Task TryLogAsync(SecurityAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
