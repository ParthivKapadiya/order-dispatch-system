using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class OrderItem : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid? ProductId { get; set; }

    public Product? Product { get; set; }

    public decimal Quantity { get; set; }

    public string? Notes { get; set; }
}
