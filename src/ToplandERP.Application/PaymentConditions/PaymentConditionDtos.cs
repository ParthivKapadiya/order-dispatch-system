using ToplandERP.Application.Common;

namespace ToplandERP.Application.PaymentConditions;

public sealed class PaymentConditionDto
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

public sealed class PaymentConditionWriteRequest
{
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public interface IPaymentConditionService
{
    Task<PagedResult<PaymentConditionDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<PaymentConditionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentConditionDto> CreateAsync(PaymentConditionWriteRequest request, CancellationToken cancellationToken = default);
    Task<PaymentConditionDto> UpdateAsync(Guid id, PaymentConditionWriteRequest request, CancellationToken cancellationToken = default);
    Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);
}
