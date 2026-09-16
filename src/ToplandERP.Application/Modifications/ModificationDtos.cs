using ToplandERP.Application.Common;
using ToplandERP.Application.Orders;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Application.Modifications;

public sealed class ModificationListQuery
{
    public ModificationRequestStatus? Status { get; set; }

    public Guid? CompanyId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed class CreateModificationRequest
{
    public string Reason { get; set; } = string.Empty;

    public decimal BillAmount { get; set; }

    public Guid? PaymentConditionId { get; set; }

    public Guid? TransporterId { get; set; }

    public string BillingAddress { get; set; } = string.Empty;
    public string BillingCity { get; set; } = string.Empty;
    public string BillingState { get; set; } = string.Empty;
    public string BillingPincode { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public string DeliveryPincode { get; set; } = string.Empty;

    public string? BookingNumber { get; set; }
    public DateTime? BookingDate { get; set; }
    public string? BookingFrom { get; set; }
    public string? BookingTo { get; set; }
    public string? BookingDetails { get; set; }
    public string? Remarks { get; set; }
    public string? SpecialInstructions { get; set; }

    public List<OrderLineRequest> Items { get; set; } = [];
}

public sealed class RejectModificationRequest
{
    public string RejectionReason { get; set; } = string.Empty;
}

public sealed class ModificationRequestSummaryDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string RequestedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string? Reason { get; init; }
    public ModificationRequestStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public string? ReviewedByName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
}

public sealed class ModificationRequestDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Guid CompanyId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerCode { get; init; } = string.Empty;
    public Guid RequestedByUserId { get; init; }
    public string RequestedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
    public ModificationRequestStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public string? ReviewedByName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
    public OrderChangeSet Current { get; init; } = new();
    public OrderChangeSet Requested { get; init; } = new();
}

public interface IModificationService
{
    Task<PagedResult<ModificationRequestSummaryDto>> GetPagedAsync(ModificationListQuery query, CancellationToken cancellationToken = default);
    Task<ModificationRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModificationRequestSummaryDto>> GetForOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<ModificationRequestDto> RequestAsync(Guid orderId, CreateModificationRequest request, CancellationToken cancellationToken = default);
    Task<ModificationRequestDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ModificationRequestDto> RejectAsync(Guid id, RejectModificationRequest request, CancellationToken cancellationToken = default);
}
