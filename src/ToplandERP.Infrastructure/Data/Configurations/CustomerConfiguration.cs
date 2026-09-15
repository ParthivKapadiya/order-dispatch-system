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

        builder.Property(entity => entity.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.Phone)
            .HasMaxLength(30);

        builder.Property(entity => entity.Email)
            .HasMaxLength(256);

        builder.Property(entity => entity.Address)
            .HasMaxLength(500);

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => new { entity.CompanyId, entity.Name });
    }
}
