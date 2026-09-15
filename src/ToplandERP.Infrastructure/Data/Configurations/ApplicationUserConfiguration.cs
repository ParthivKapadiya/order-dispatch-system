using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Infrastructure.Identity;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.Property(entity => entity.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasOne(entity => entity.Company)
            .WithMany()
            .HasForeignKey(entity => entity.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.IsActive);
    }
}
