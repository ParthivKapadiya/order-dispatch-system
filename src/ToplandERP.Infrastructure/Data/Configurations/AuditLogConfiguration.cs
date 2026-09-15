using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Action)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(entity => entity.EntityType)
            .HasMaxLength(150);

        builder.Property(entity => entity.Details)
            .HasMaxLength(4000);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.UserId);
        builder.HasIndex(entity => entity.CreatedAt);
        builder.HasIndex(entity => new { entity.EntityType, entity.EntityId });
    }
}
