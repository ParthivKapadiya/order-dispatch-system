using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class DispatchConfiguration : IEntityTypeConfiguration<Dispatch>
{
    public void Configure(EntityTypeBuilder<Dispatch> builder)
    {
        builder.ToTable("Dispatches");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.DispatchPersonName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.LrNumber)
            .HasMaxLength(80);

        builder.Property(entity => entity.BookingNumber)
            .HasMaxLength(80);

        builder.Property(entity => entity.Notes)
            .HasMaxLength(2000);

        builder.HasOne(entity => entity.Order)
            .WithMany(order => order.Dispatches)
            .HasForeignKey(entity => entity.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(entity => entity.Transporter)
            .WithMany()
            .HasForeignKey(entity => entity.TransporterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.OrderId).IsUnique();
    }
}
