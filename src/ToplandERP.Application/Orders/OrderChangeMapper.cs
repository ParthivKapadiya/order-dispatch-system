using System.Text.Json;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Application.Orders;

public static class OrderChangeMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static OrderChangeSet FromOrder(Order order)
    {
        return new OrderChangeSet
        {
            BillAmount = order.BillAmount,
            PaymentConditionId = order.PaymentConditionId ?? Guid.Empty,
            PaymentConditionName = order.PaymentCondition?.Name ?? string.Empty,
            TransporterId = order.TransporterId ?? Guid.Empty,
            TransporterName = order.Transporter?.Name ?? string.Empty,
            BillingAddress = order.BillingAddress,
            BillingCity = order.BillingCity,
            BillingState = order.BillingState,
            BillingPincode = order.BillingPincode,
            DeliveryAddress = order.DeliveryAddress,
            DeliveryCity = order.DeliveryCity,
            DeliveryState = order.DeliveryState,
            DeliveryPincode = order.DeliveryPincode,
            BookingNumber = order.BookingNumber,
            BookingDate = order.BookingDate,
            BookingFrom = order.BookingFrom,
            BookingTo = order.BookingTo,
            BookingDetails = order.BookingDetails,
            Remarks = order.Remarks,
            SpecialInstructions = order.SpecialInstructions,
            Items = order.Items
                .OrderBy(item => item.ProductName)
                .Select(item => new OrderChangeLine
                {
                    ProductId = item.ProductId ?? Guid.Empty,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    ModelNumber = item.ModelNumber,
                    Unit = item.Unit,
                    Quantity = item.Quantity
                })
                .ToList()
        };
    }

    public static string Serialize(OrderChangeSet changeSet) =>
        JsonSerializer.Serialize(changeSet, JsonOptions);

    public static OrderChangeSet Deserialize(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new OrderChangeSet();
        }

        return JsonSerializer.Deserialize<OrderChangeSet>(json, JsonOptions) ?? new OrderChangeSet();
    }

    public static string? TrimOrNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
