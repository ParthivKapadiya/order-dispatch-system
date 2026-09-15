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

        builder.Property(entity => entity.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.Code)
            .HasMaxLength(64);

        builder.Property(entity => entity.Description)
            .HasMaxLength(1000);

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => new { entity.CompanyId, entity.Code });
    }
}
