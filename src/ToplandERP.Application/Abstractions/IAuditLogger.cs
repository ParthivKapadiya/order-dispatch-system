namespace ToplandERP.Application.Abstractions;

public interface IAuditLogger
{
    void Record(string action, string entityType, Guid? entityId, Guid? companyId, string? details = null);
}
