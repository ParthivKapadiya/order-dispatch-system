using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class Customer : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Company Company { get; set; } = null!;

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

    public bool IsActive { get; set; } = true;
}
