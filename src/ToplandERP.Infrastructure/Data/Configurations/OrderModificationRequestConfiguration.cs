using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class OrderModificationRequestConfiguration : IEntityTypeConfiguration<OrderModificationRequest>
{
    public void Configure(EntityTypeBuilder<OrderModificationRequest> builder)
    {
        builder.ToTable("OrderModificationRequests");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.RequestedByName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.Reason)
            .HasMaxLength(2000);

        builder.Property(entity => entity.CurrentSnapshotJson)
            .IsRequired();

        builder.Property(entity => entity.RequestedChangesJson)
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(entity => entity.ReviewedByName)
            .HasMaxLength(200);

        builder.Property(entity => entity.RejectionReason)
            .HasMaxLength(2000);

        builder.HasOne(entity => entity.Order)
            .WithMany(order => order.ModificationRequests)
            .HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.OrderId);
        builder.HasIndex(entity => entity.RequestedByUserId);
        builder.HasIndex(entity => new { entity.OrderId, entity.Status });
    }
}
