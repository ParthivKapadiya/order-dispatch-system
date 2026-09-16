using ToplandERP.Domain.Common;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Domain.Entities;

public class Order : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

    public Guid? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public Guid? PaymentConditionId { get; set; }

    public PaymentCondition? PaymentCondition { get; set; }

    public Guid? TransporterId { get; set; }

    public Transporter? Transporter { get; set; }

    public string? OrderNumber { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Received;

    public DateTime? OrderedAt { get; set; }

    public Guid? CreatedByUserId { get; set; }

    public string? CustomerCode { get; set; }

    public string? CustomerName { get; set; }

    public string? CustomerMobile { get; set; }

    public string BillingAddress { get; set; } = string.Empty;

    public string BillingCity { get; set; } = string.Empty;

    public string BillingState { get; set; } = string.Empty;

    public string BillingPincode { get; set; } = string.Empty;

    public string DeliveryAddress { get; set; } = string.Empty;

    public string DeliveryCity { get; set; } = string.Empty;

    public string DeliveryState { get; set; } = string.Empty;

    public string DeliveryPincode { get; set; } = string.Empty;

    public decimal BillAmount { get; set; }

    public string? BookingNumber { get; set; }

    public DateTime? BookingDate { get; set; }

    public string? BookingFrom { get; set; }

    public string? BookingTo { get; set; }

    public string? BookingDetails { get; set; }

    public string? Remarks { get; set; }

    public string? SpecialInstructions { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<OrderModificationRequest> ModificationRequests { get; set; } = new List<OrderModificationRequest>();

    public ICollection<Dispatch> Dispatches { get; set; } = new List<Dispatch>();
}
