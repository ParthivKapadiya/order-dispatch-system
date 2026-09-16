using ToplandERP.Application.Common;

namespace ToplandERP.Application.Products;

public sealed class ProductDto
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string? ModelNumber { get; init; }
    public string? Description { get; init; }
    public string? Unit { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class ProductWriteRequest
{
    public Guid? CompanyId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? ModelNumber { get; set; }
    public string? Description { get; set; }
    public string? Unit { get; set; }
}

public interface IProductService
{
    Task<PagedResult<ProductDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDto> CreateAsync(ProductWriteRequest request, CancellationToken cancellationToken = default);
    Task<ProductDto> UpdateAsync(Guid id, ProductWriteRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
