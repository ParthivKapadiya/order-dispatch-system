using ToplandERP.Domain.Common;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Domain.Entities;

public class DispatchDocument : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Guid DispatchId { get; set; }

    public Dispatch Dispatch { get; set; } = null!;

    public string FilePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public DispatchDocumentType DocumentType { get; set; } = DispatchDocumentType.Other;

    public Guid? UploadedByUserId { get; set; }
}
