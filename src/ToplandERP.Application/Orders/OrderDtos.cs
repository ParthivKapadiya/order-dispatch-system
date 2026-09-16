using ToplandERP.Application.Common;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Application.Orders;

public sealed class OrderListQuery
{
    public string? Search { get; set; }

    public OrderStatus? Status { get; set; }

    public Guid? CompanyId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed class OrderItemDto
{
    public Guid Id { get; init; }
    public Guid? ProductId { get; init; }
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public string? ModelNumber { get; init; }
    public string? Unit { get; init; }
    public decimal Quantity { get; init; }
}

public sealed class OrderDto
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public DateTime? OrderedAt { get; init; }
    public Guid? CustomerId { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerMobile { get; init; } = string.Empty;
    public string BillingAddress { get; init; } = string.Empty;
    public string BillingCity { get; init; } = string.Empty;
    public string BillingState { get; init; } = string.Empty;
    public string BillingPincode { get; init; } = string.Empty;
    public string DeliveryAddress { get; init; } = string.Empty;
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryState { get; init; } = string.Empty;
    public string DeliveryPincode { get; init; } = string.Empty;
    public decimal BillAmount { get; init; }
    public Guid? PaymentConditionId { get; init; }
    public string PaymentConditionName { get; init; } = string.Empty;
    public Guid? TransporterId { get; init; }
    public string TransporterName { get; init; } = string.Empty;
    public string? BookingNumber { get; init; }
    public DateTime? BookingDate { get; init; }
    public string? BookingFrom { get; init; }
    public string? BookingTo { get; init; }
    public string? BookingDetails { get; init; }
    public string? Remarks { get; init; }
    public string? SpecialInstructions { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];

    public Guid? CreatedByUserId { get; init; }

    public bool IsLocked { get; init; }

    public bool HasPendingModification { get; init; }

    public IReadOnlyList<OrderModificationHistoryDto> ModificationHistory { get; init; } = [];

    public OrderDispatchInfoDto? Dispatch { get; init; }
}

public sealed class OrderModificationHistoryDto
{
    public Guid Id { get; init; }
    public string RequestedByName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string StatusLabel { get; init; } = string.Empty;
    public Domain.Enums.ModificationRequestStatus Status { get; init; }
    public string? ReviewedByName { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
}

public sealed class OrderDispatchInfoDto
{
    public Guid Id { get; init; }
    public DateTime? DispatchDate { get; init; }
    public DateTime? DispatchedAt { get; init; }
    public string DispatchPersonName { get; init; } = string.Empty;
    public string TransporterName { get; init; } = string.Empty;
    public string? LrNumber { get; init; }
    public string? BookingNumber { get; init; }
    public string? Notes { get; init; }
    public bool IsCompleted { get; init; }
    public IReadOnlyList<OrderDispatchDocumentDto> Documents { get; init; } = [];
}

public sealed class OrderDispatchDocumentDto
{
    public Guid Id { get; init; }
    public string DocumentTypeLabel { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
}

public sealed class OrderLineRequest
{
    public Guid? ProductId { get; set; }

    public decimal Quantity { get; set; }
}

public sealed class CreateOrderRequest
{
    public Guid? CompanyId { get; set; }

    public Guid? CustomerId { get; set; }

    public string BillingAddress { get; set; } = string.Empty;
    public string BillingCity { get; set; } = string.Empty;
    public string BillingState { get; set; } = string.Empty;
    public string BillingPincode { get; set; } = string.Empty;
    public string DeliveryAddress { get; set; } = string.Empty;
    public string DeliveryCity { get; set; } = string.Empty;
    public string DeliveryState { get; set; } = string.Empty;
    public string DeliveryPincode { get; set; } = string.Empty;

    public List<OrderLineRequest> Items { get; set; } = [];

    public decimal BillAmount { get; set; }

    public Guid? PaymentConditionId { get; set; }

    public Guid? TransporterId { get; set; }

    public string? BookingNumber { get; set; }

    public DateTime? BookingDate { get; set; }

    public string? BookingFrom { get; set; }

    public string? BookingTo { get; set; }

    public string? BookingDetails { get; set; }

    public string? Remarks { get; set; }

    public string? SpecialInstructions { get; set; }
}

public class OrderLookupItem
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
    public string? Extra { get; init; }
}

public sealed class OrderCustomerLookup : OrderLookupItem
{
    public string Mobile { get; init; } = string.Empty;
    public string BillingAddress { get; init; } = string.Empty;
    public string BillingCity { get; init; } = string.Empty;
    public string BillingState { get; init; } = string.Empty;
    public string BillingPincode { get; init; } = string.Empty;
    public string DeliveryAddress { get; init; } = string.Empty;
    public string DeliveryCity { get; init; } = string.Empty;
    public string DeliveryState { get; init; } = string.Empty;
    public string DeliveryPincode { get; init; } = string.Empty;
}

public sealed class OrderCreateCatalog
{
    public IReadOnlyList<OrderCustomerLookup> Customers { get; init; } = [];
    public IReadOnlyList<OrderLookupItem> Products { get; init; } = [];
    public IReadOnlyList<OrderLookupItem> PaymentConditions { get; init; } = [];
    public IReadOnlyList<OrderLookupItem> Transporters { get; init; } = [];
}

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetPagedAsync(OrderListQuery query, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderCreateCatalog> GetCreateCatalogAsync(Guid? companyId, CancellationToken cancellationToken = default);
    Task<OrderCustomerLookup?> GetCustomerLookupAsync(Guid customerId, CancellationToken cancellationToken = default);
    Task<OrderDto> MarkReadyToDispatchAsync(Guid id, CancellationToken cancellationToken = default);
}
