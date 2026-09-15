using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.UserId);
        builder.HasIndex(entity => new { entity.UserId, entity.IsRead });
    }
}
