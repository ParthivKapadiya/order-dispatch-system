namespace ToplandERP.Domain.Enums;

/// <summary>
/// UI-facing order statuses confirmed for the design system.
/// Additional workflow states will be defined when Order Management is implemented.
/// </summary>
public enum OrderStatus
{
    Received = 1,
    ReadyToDispatch = 2,
    DispatchDone = 3
}
