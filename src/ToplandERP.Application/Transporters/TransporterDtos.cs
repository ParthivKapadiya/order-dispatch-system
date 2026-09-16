using ToplandERP.Application.Common;

namespace ToplandERP.Application.Transporters;

public sealed class TransporterDto
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? ContactPerson { get; init; }
    public string? Mobile { get; init; }
    public string? Address { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class TransporterWriteRequest
{
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Mobile { get; set; }
    public string? Address { get; set; }
}

public interface ITransporterService
{
    Task<PagedResult<TransporterDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<TransporterDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TransporterDto> CreateAsync(TransporterWriteRequest request, CancellationToken cancellationToken = default);
    Task<TransporterDto> UpdateAsync(Guid id, TransporterWriteRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
