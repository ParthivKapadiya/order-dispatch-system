using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Infrastructure.Data.Configurations;

public sealed class DispatchDocumentConfiguration : IEntityTypeConfiguration<DispatchDocument>
{
    public void Configure(EntityTypeBuilder<DispatchDocument> builder)
    {
        builder.ToTable("DispatchDocuments");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(entity => entity.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(entity => entity.ContentType)
            .HasMaxLength(150);

        builder.Property(entity => entity.DocumentType)
            .HasMaxLength(100);

        builder.HasOne(entity => entity.Dispatch)
            .WithMany(dispatch => dispatch.Documents)
            .HasForeignKey(entity => entity.DispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.DispatchId);
    }
}
