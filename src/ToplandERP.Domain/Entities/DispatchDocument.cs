using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class DispatchDocument : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Guid DispatchId { get; set; }

    public Dispatch Dispatch { get; set; } = null!;

    public string FilePath { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string? ContentType { get; set; }

    public string? DocumentType { get; set; }
}
