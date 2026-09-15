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

    public string? OrderNumber { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Received;

    public DateTime? OrderedAt { get; set; }

    public string? Notes { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

    public ICollection<OrderModificationRequest> ModificationRequests { get; set; } = new List<OrderModificationRequest>();

    public ICollection<Dispatch> Dispatches { get; set; } = new List<Dispatch>();
}
