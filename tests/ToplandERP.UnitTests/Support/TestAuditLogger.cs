using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Entities;

namespace ToplandERP.UnitTests.Support;

public sealed class TestAuditLogger : IAuditLogger
{
    public List<(string Action, string EntityType, Guid? EntityId, Guid? CompanyId, string? Details)> Entries { get; } = [];

    public void Record(string action, string entityType, Guid? entityId, Guid? companyId, string? details = null)
    {
        Entries.Add((action, entityType, entityId, companyId, details));
    }
}

public static class MasterDataFactory
{
    public static Customer Customer(Guid companyId, string code, string name, string mobile)
    {
        return new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerCode = code,
            CustomerName = name,
            Mobile = mobile,
            BillingAddress = "Billing street",
            BillingCity = "Ahmedabad",
            BillingState = "Gujarat",
            BillingPincode = "380001",
            DeliveryAddress = "Delivery street",
            DeliveryCity = "Rajkot",
            DeliveryState = "Gujarat",
            DeliveryPincode = "360001",
            IsActive = true
        };
    }

    public static Product Product(Guid companyId, string code, string name)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProductCode = code,
            ProductName = name,
            Category = "Tractor",
            ModelNumber = "M-100",
            IsActive = true
        };
    }

    public static Transporter Transporter(Guid companyId, string name)
    {
        return new Transporter
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            Mobile = "9876543210",
            IsActive = true
        };
    }

    public static PaymentCondition PaymentCondition(Guid companyId, string name)
    {
        return new PaymentCondition
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = name,
            IsActive = true
        };
    }
}
