using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.ProductCode)
            .HasColumnName("Code")
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(entity => entity.ProductName)
            .HasColumnName("Name")
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.Category)
            .HasMaxLength(100);

        builder.Property(entity => entity.ModelNumber)
            .HasMaxLength(100);

        builder.Property(entity => entity.Description)
            .HasMaxLength(1000);

        builder.Property(entity => entity.Unit)
            .HasMaxLength(30);

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => new { entity.CompanyId, entity.ProductCode }).IsUnique();
        builder.HasIndex(entity => new { entity.CompanyId, entity.ProductName });
        builder.HasIndex(entity => new { entity.CompanyId, entity.ModelNumber });
        builder.HasIndex(entity => new { entity.CompanyId, entity.Category });
    }
}
