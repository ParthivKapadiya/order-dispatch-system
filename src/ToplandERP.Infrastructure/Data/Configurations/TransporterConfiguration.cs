using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class TransporterConfiguration : IEntityTypeConfiguration<Transporter>
{
    public void Configure(EntityTypeBuilder<Transporter> builder)
    {
        builder.ToTable("Transporters");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.ContactPerson)
            .HasMaxLength(200);

        builder.Property(entity => entity.Mobile)
            .HasColumnName("Phone")
            .HasMaxLength(30);

        builder.Property(entity => entity.Address)
            .HasMaxLength(500);

        builder.Property<string?>("VehicleDetails")
            .HasMaxLength(250);

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => new { entity.CompanyId, entity.Name });
    }
}
