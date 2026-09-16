using Microsoft.EntityFrameworkCore;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }

    DbSet<Customer> Customers { get; }

    DbSet<Product> Products { get; }

    DbSet<Transporter> Transporters { get; }

    DbSet<PaymentCondition> PaymentConditions { get; }

    DbSet<Order> Orders { get; }

    DbSet<OrderItem> OrderItems { get; }

    DbSet<OrderModificationRequest> OrderModificationRequests { get; }

    DbSet<Dispatch> Dispatches { get; }

    DbSet<DispatchDocument> DispatchDocuments { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default);
}
