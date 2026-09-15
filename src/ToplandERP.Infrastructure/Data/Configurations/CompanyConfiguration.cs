using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.Code)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(entity => entity.Address)
            .HasMaxLength(500);

        builder.Property(entity => entity.Phone)
            .HasMaxLength(30);

        builder.Property(entity => entity.Email)
            .HasMaxLength(256);

        builder.HasIndex(entity => entity.Code)
            .IsUnique();
    }
}
