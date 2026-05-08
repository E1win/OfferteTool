namespace Domain.Enums;

public enum SecurityAuditEventType
{
    LoginSucceeded,
    LoginFailed,
    LoginRejectedInactiveUser,
    LoginLockedOut,
    AccessDenied,
    UserCreated,
    UserUpdated,
    UserDisabled,
    OrganisationCreated,
    OrganisationUpdated,
    OrganisationDeactivated
}
