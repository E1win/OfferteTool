using Application.Models.SecurityAudit;

namespace Application.Interfaces.Services;

public interface ISecurityAuditService
{
    Task LogAsync(SecurityAuditEvent auditEvent);
    Task TryLogAsync(SecurityAuditEvent auditEvent);
}
