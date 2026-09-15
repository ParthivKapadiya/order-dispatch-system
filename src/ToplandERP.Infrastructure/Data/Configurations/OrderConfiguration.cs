using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.OrderNumber)
            .HasMaxLength(50);

        builder.Property(entity => entity.Notes)
            .HasMaxLength(2000);

        builder.Property(entity => entity.Status)
            .HasConversion<int>();

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Customer)
            .WithMany()
            .HasForeignKey(entity => entity.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.PaymentCondition)
            .WithMany()
            .HasForeignKey(entity => entity.PaymentConditionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => new { entity.CompanyId, entity.OrderNumber });
        builder.HasIndex(entity => entity.Status);
    }
}
