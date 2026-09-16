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

        builder.Property(entity => entity.CustomerCode)
            .HasMaxLength(50);

        builder.Property(entity => entity.CustomerName)
            .HasMaxLength(200);

        builder.Property(entity => entity.CustomerMobile)
            .HasMaxLength(30);

        builder.Property(entity => entity.BillingAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(entity => entity.BillingCity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(entity => entity.BillingState)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(entity => entity.BillingPincode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(entity => entity.DeliveryAddress)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(entity => entity.DeliveryCity)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(entity => entity.DeliveryState)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(entity => entity.DeliveryPincode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(entity => entity.BillAmount)
            .HasPrecision(18, 2);

        builder.Property(entity => entity.BookingNumber)
            .HasMaxLength(80);

        builder.Property(entity => entity.BookingFrom)
            .HasMaxLength(150);

        builder.Property(entity => entity.BookingTo)
            .HasMaxLength(150);

        builder.Property(entity => entity.BookingDetails)
            .HasMaxLength(1000);

        builder.Property(entity => entity.Remarks)
            .HasColumnName("Notes")
            .HasMaxLength(2000);

        builder.Property(entity => entity.SpecialInstructions)
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

        builder.HasOne(entity => entity.Transporter)
            .WithMany()
            .HasForeignKey(entity => entity.TransporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => new { entity.CompanyId, entity.OrderNumber }).IsUnique();
        builder.HasIndex(entity => entity.Status);
        builder.HasIndex(entity => entity.CustomerId);
        builder.HasIndex(entity => entity.TransporterId);
    }
}
