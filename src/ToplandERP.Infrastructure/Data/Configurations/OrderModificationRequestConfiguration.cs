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

        builder.Property(entity => entity.Reason)
            .HasMaxLength(2000);

        builder.Property(entity => entity.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(entity => entity.Order)
            .WithMany(order => order.ModificationRequests)
            .HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.OrderId);
        builder.HasIndex(entity => entity.RequestedByUserId);
    }
}
