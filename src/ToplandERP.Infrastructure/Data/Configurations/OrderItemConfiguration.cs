using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.ProductCode)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(entity => entity.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.ModelNumber)
            .HasMaxLength(100);

        builder.Property(entity => entity.Unit)
            .HasMaxLength(30);

        builder.Property(entity => entity.Quantity)
            .HasPrecision(18, 3);

        builder.Property(entity => entity.Notes)
            .HasMaxLength(500);

        builder.HasOne(entity => entity.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Product)
            .WithMany()
            .HasForeignKey(entity => entity.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.OrderId);
        builder.HasIndex(entity => entity.ProductId);
    }
}
