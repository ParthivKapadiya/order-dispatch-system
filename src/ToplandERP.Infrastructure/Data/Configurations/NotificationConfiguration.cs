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

        builder.Property(entity => entity.RecipientUserId)
            .HasColumnName("UserId")
            .IsRequired();

        builder.Property(entity => entity.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(entity => entity.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(entity => entity.Message)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(entity => entity.RelatedEntityType)
            .HasMaxLength(100);

        builder.Property(entity => entity.EventKey)
            .HasMaxLength(80);

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.RecipientUserId)
            .HasDatabaseName("IX_Notifications_UserId");
        builder.HasIndex(entity => new { entity.RecipientUserId, entity.IsRead })
            .HasDatabaseName("IX_Notifications_UserId_IsRead");
        builder.HasIndex(entity => new { entity.RecipientUserId, entity.IsRead, entity.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");
        builder.HasIndex(entity => entity.EventKey)
            .IsUnique()
            .HasDatabaseName("IX_Notifications_EventKey");
    }
}
