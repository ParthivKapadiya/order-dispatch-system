using ToplandERP.Domain.Enums;

namespace ToplandERP.Application.Notifications;

public static class NotificationMessages
{
    public static (string Title, string Message) Compose(
        NotificationType type,
        string orderNumber,
        string? customerName = null,
        string? actorName = null,
        string? transporterName = null,
        string? rejectionReason = null)
    {
        return type switch
        {
            NotificationType.OrderCreated => (
                "New Order Created",
                $"Order {orderNumber} has been created for {customerName}."),
            NotificationType.OrderReadyToDispatch => (
                "Order Ready to Dispatch",
                $"Order {orderNumber} is ready to dispatch."),
            NotificationType.OrderDispatched => (
                "Order Dispatched",
                string.IsNullOrWhiteSpace(transporterName)
                    ? $"Order {orderNumber} has been dispatched."
                    : $"Order {orderNumber} has been dispatched via {transporterName}."),
            NotificationType.ModificationRequested => (
                "Modification Requested",
                $"{actorName} requested a modification for order {orderNumber}."),
            NotificationType.ModificationApproved => (
                "Modification Approved",
                $"Your modification request for order {orderNumber} has been approved."),
            NotificationType.ModificationRejected => (
                "Modification Rejected",
                string.IsNullOrWhiteSpace(rejectionReason)
                    ? $"Your modification request for order {orderNumber} has been rejected."
                    : $"Your modification request for order {orderNumber} was rejected. Reason: {rejectionReason}"),
            _ => ("Notification", $"An update is available for order {orderNumber}.")
        };
    }
}
