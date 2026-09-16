namespace ToplandERP.Application.Orders;

public sealed class OrderChangeSet
{
    public decimal BillAmount { get; set; }

    public Guid PaymentConditionId { get; set; }

    public string PaymentConditionName { get; set; } = string.Empty;

    public Guid TransporterId { get; set; }

    public string TransporterName { get; set; } = string.Empty;

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

    public List<OrderChangeLine> Items { get; set; } = [];
}

public sealed class OrderChangeLine
{
    public Guid ProductId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? ModelNumber { get; set; }

    public string? Unit { get; set; }

    public decimal Quantity { get; set; }
}

public static class OrderStatusDisplay
{
    public static string Label(Domain.Enums.OrderStatus status) => status switch
    {
        Domain.Enums.OrderStatus.Received => "Order Received",
        Domain.Enums.OrderStatus.ReadyToDispatch => "Ready to Dispatch",
        Domain.Enums.OrderStatus.DispatchDone => "Dispatch Done",
        _ => status.ToString()
    };
}
