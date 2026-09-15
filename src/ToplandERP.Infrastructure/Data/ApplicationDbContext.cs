using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Common;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Identity;

namespace ToplandERP.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly ICurrentUser _currentUser;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUser currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    private bool BypassCompanyFilter => _currentUser.BypassCompanyFilter;

    private bool IsSuperAdminUser => _currentUser.IsSuperAdmin;

    private Guid CurrentCompanyId => _currentUser.CompanyId ?? Guid.Empty;

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Transporter> Transporters => Set<Transporter>();

    public DbSet<PaymentCondition> PaymentConditions => Set<PaymentCondition>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<OrderModificationRequest> OrderModificationRequests => Set<OrderModificationRequest>();

    public DbSet<Dispatch> Dispatches => Set<Dispatch>();

    public DbSet<DispatchDocument> DispatchDocuments => Set<DispatchDocument>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyCompanyQueryFilters(modelBuilder);
    }

    private void ApplyAuditTimestamps()
    {
        var utcNow = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.Id == Guid.Empty)
                {
                    entry.Entity.Id = Guid.NewGuid();
                }

                entry.Entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }

        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
            }
        }
    }

    private void ApplyCompanyQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!typeof(ICompanyScoped).IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }

            typeof(ApplicationDbContext)
                .GetMethod(nameof(SetCompanyQueryFilter), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(entityType.ClrType)
                .Invoke(this, [modelBuilder]);
        }
    }

    private void SetCompanyQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ICompanyScoped
    {
        Expression<Func<TEntity, bool>> filter = entity =>
            BypassCompanyFilter
            || IsSuperAdminUser
            || entity.CompanyId == CurrentCompanyId;

        modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
    }
}
