using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.CustomerCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(entity => entity.CustomerName)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.Mobile)
            .HasColumnName("Phone")
            .IsRequired()
            .HasMaxLength(30);

        builder.Property(entity => entity.Email)
            .HasMaxLength(256);

        builder.Property(entity => entity.BillingAddress)
            .HasColumnName("Address")
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

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => new { entity.CompanyId, entity.CustomerCode }).IsUnique();
        builder.HasIndex(entity => new { entity.CompanyId, entity.CustomerName });
        builder.HasIndex(entity => new { entity.CompanyId, entity.Mobile });
    }
}
