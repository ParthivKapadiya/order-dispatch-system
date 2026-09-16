using ToplandERP.Domain.Common;

namespace ToplandERP.Domain.Entities;

public class Dispatch : BaseEntity, ICompanyScoped
{
    public Guid CompanyId { get; set; }

    public Guid OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public Guid? TransporterId { get; set; }

    public Transporter? Transporter { get; set; }

    public DateTime? DispatchDate { get; set; }

    public DateTime? DispatchedAt { get; set; }

    public Guid? DispatchedByUserId { get; set; }

    public string DispatchPersonName { get; set; } = string.Empty;

    public string? LrNumber { get; set; }

    public string? BookingNumber { get; set; }

    public string? Notes { get; set; }

    public bool IsCompleted { get; set; }

    public ICollection<DispatchDocument> Documents { get; set; } = new List<DispatchDocument>();
}
