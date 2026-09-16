using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Auditing;

public sealed class AuditLogger : IAuditLogger
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public AuditLogger(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public void Record(string action, string entityType, Guid? entityId, Guid? companyId, string? details = null)
    {
        _dbContext.AuditLogs.Add(new AuditLog
        {
            CompanyId = companyId ?? _currentUser.CompanyId,
            UserId = _currentUser.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        });
    }
}
