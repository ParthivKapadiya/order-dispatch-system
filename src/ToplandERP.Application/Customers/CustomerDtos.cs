using ToplandERP.Application.Common;

namespace ToplandERP.Application.Customers;

public sealed class CustomerDto
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Mobile { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string BillingAddress { get; init; } = string.Empty;
    public string BillingCity { get; init; } = string.Empty;
    public string BillingState { get; init; } = string.Empty;
    public string BillingPincode { get; init; } = string.Empty;
    public string DeliveryAddress { get; init; } = string.Empty;
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryState { get; init; } = string.Empty;
    public string DeliveryPincode { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class CustomerWriteRequest
{
    public Guid? CompanyId { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string BillingAddress { get; set; } = string.Empty;
    public string BillingCity { get; set; } = string.Empty;
    public string BillingState { get; set; } = string.Empty;
    public string BillingPincode { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public string DeliveryPincode { get; set; } = string.Empty;
}

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CustomerDto> CreateAsync(CustomerWriteRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto> UpdateAsync(Guid id, CustomerWriteRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
