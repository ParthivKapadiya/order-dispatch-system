using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ToplandERP.Domain.Entities;
using ToplandERP.Domain.Enums;

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
            .HasConversion(new ValueConverter<DispatchDocumentType, string>(
                value => ToStorage(value),
                value => FromStorage(value)))
            .HasMaxLength(100);

        builder.HasOne(entity => entity.Dispatch)
            .WithMany(dispatch => dispatch.Documents)
            .HasForeignKey(entity => entity.DispatchId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.CompanyId);
        builder.HasIndex(entity => entity.DispatchId);
    }

    private static string ToStorage(DispatchDocumentType type) => type switch
    {
        DispatchDocumentType.MaterialPhoto => "MATERIAL_PHOTO",
        DispatchDocumentType.TransportReceipt => "TRANSPORT_RECEIPT",
        DispatchDocumentType.LrDocument => "LR_DOCUMENT",
        _ => "OTHER"
    };

    private static DispatchDocumentType FromStorage(string value) => value switch
    {
        "MATERIAL_PHOTO" => DispatchDocumentType.MaterialPhoto,
        "TRANSPORT_RECEIPT" => DispatchDocumentType.TransportReceipt,
        "LR_DOCUMENT" => DispatchDocumentType.LrDocument,
        _ => DispatchDocumentType.Other
    };
}
