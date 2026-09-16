using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Orders;
using ToplandERP.Application.Notifications;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Domain.Enums;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class OrderServiceTests
{
    [Fact]
    public async Task SalesEmployee_creates_order_as_received_for_own_company()
    {
        var databaseName = Guid.NewGuid().ToString();
        var masters = await SeedAsync(databaseName);
        var currentUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.GravisCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = CreateService(dbContext, currentUser);

        var created = await service.CreateAsync(ValidRequest(masters));

        created.Status.Should().Be(OrderStatus.Received);
        created.StatusLabel.Should().Be("Order Received");
        created.CompanyId.Should().Be(SeedIdentifiers.GravisCompanyId);
        created.CustomerName.Should().Be("Gravis customer");
        created.Items.Should().ContainSingle();
        created.OrderNumber.Should().StartWith("GRAVIS-");
        created.BillingCity.Should().Be("Ahmedabad");
        created.DeliveryCity.Should().Be("Rajkot");
    }

    [Fact]
    public async Task SalesEmployee_cannot_use_another_company_customer()
    {
        var databaseName = Guid.NewGuid().ToString();
        var masters = await SeedAsync(databaseName);
        var currentUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.GravisCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = CreateService(dbContext, currentUser);
        var request = ValidRequest(masters);
        request.CustomerId = masters.JeekoCustomerId;

        var act = async () => await service.CreateAsync(request);

        await act.Should().ThrowAsync<BusinessException>();
    }

    [Fact]
    public async Task Cross_company_order_access_is_denied()
    {
        var databaseName = Guid.NewGuid().ToString();
        var masters = await SeedAsync(databaseName);
        var creator = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.JeekoCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };
        Guid orderId;
        await using (var createContext = CreateContext(databaseName, creator))
        {
            var created = await CreateService(createContext, creator).CreateAsync(new CreateOrderRequest
            {
                CustomerId = masters.JeekoCustomerId,
                PaymentConditionId = masters.JeekoPaymentId,
                TransporterId = masters.JeekoTransporterId,
                BillingAddress = "Jeeko bill",
                BillingCity = "Rajkot",
                BillingState = "Gujarat",
                BillingPincode = "360001",
                DeliveryAddress = "Jeeko del",
                DeliveryCity = "Rajkot",
                DeliveryState = "Gujarat",
                DeliveryPincode = "360001",
                BillAmount = 100,
                Items = [new OrderLineRequest { ProductId = masters.JeekoProductId, Quantity = 1 }]
            });
            orderId = created.Id;
        }

        var gravisUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.GravisCompanyId,
            Roles = [RoleNames.CompanyAdmin]
        };
        await using var queryContext = CreateContext(databaseName, gravisUser);
        (await CreateService(queryContext, gravisUser).GetByIdAsync(orderId)).Should().BeNull();
    }

    [Fact]
    public async Task SuperAdmin_can_list_orders_from_all_companies()
    {
        var databaseName = Guid.NewGuid().ToString();
        var masters = await SeedAsync(databaseName);
        var sales = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.SalesEmployee] };
        await using (var dbContext = CreateContext(databaseName, sales))
        {
            await CreateService(dbContext, sales).CreateAsync(ValidRequest(masters));
        }

        var superAdmin = new TestCurrentUser { Roles = [RoleNames.SuperAdmin] };
        await using var queryContext = CreateContext(databaseName, superAdmin);
        var result = await CreateService(queryContext, superAdmin).GetPagedAsync(new OrderListQuery());
        result.Items.Should().Contain(item => item.CompanyCode == "GRAVIS");
    }

    [Fact]
    public void Create_rejects_order_without_products()
    {
        var validator = new CreateOrderRequestValidator();
        var result = validator.TestValidate(new CreateOrderRequest
        {
            CustomerId = Guid.NewGuid(),
            PaymentConditionId = Guid.NewGuid(),
            TransporterId = Guid.NewGuid(),
            BillingAddress = "A",
            BillingCity = "Ahmedabad",
            BillingState = "Gujarat",
            BillingPincode = "380001",
            DeliveryAddress = "B",
            DeliveryCity = "Rajkot",
            DeliveryState = "Gujarat",
            DeliveryPincode = "360001",
            Items = [new OrderLineRequest()]
        });
        result.ShouldHaveValidationErrorFor(request => request.Items);
    }

    [Fact]
    public async Task Forged_company_id_is_ignored_for_sales_employee()
    {
        var databaseName = Guid.NewGuid().ToString();
        var masters = await SeedAsync(databaseName);
        var currentUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.GravisCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var request = ValidRequest(masters);
        request.CompanyId = SeedIdentifiers.JeekoCompanyId;

        var created = await CreateService(dbContext, currentUser).CreateAsync(request);
        created.CompanyId.Should().Be(SeedIdentifiers.GravisCompanyId);
    }

    private static CreateOrderRequest ValidRequest(SeededMasters masters)
    {
        return new CreateOrderRequest
        {
            CustomerId = masters.GravisCustomerId,
            PaymentConditionId = masters.GravisPaymentId,
            TransporterId = masters.GravisTransporterId,
            BillingAddress = "Navrangpura",
            BillingCity = "Ahmedabad",
            BillingState = "Gujarat",
            BillingPincode = "380009",
            DeliveryAddress = "Kalawad Road",
            DeliveryCity = "Rajkot",
            DeliveryState = "Gujarat",
            DeliveryPincode = "360005",
            BillAmount = 125000,
            BookingNumber = "LR-101",
            Remarks = "Handle with care",
            Items = [new OrderLineRequest { ProductId = masters.GravisProductId, Quantity = 2 }]
        };
    }

    private static OrderService CreateService(ApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        return new OrderService(
            dbContext,
            currentUser,
            new TestAuditLogger(),
            new NotificationService(dbContext, currentUser, new TestUserDirectory()),
            new CreateOrderRequestValidator());
    }

    private static ApplicationDbContext CreateContext(string databaseName, ICurrentUser currentUser)
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options,
            currentUser);
    }

    private static async Task<SeededMasters> SeedAsync(string databaseName)
    {
        await using var dbContext = CreateContext(databaseName, SystemCurrentUser.Instance);
        dbContext.Companies.AddRange(
            new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis India Private Limited", Code = CompanyCodes.Gravis, IsActive = true },
            new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko Agritech LLP", Code = CompanyCodes.Jeeko, IsActive = true });

        var gravisCustomer = MasterDataFactory.Customer(SeedIdentifiers.GravisCompanyId, "GRA-001", "Gravis customer", "9876500001");
        var jeekoCustomer = MasterDataFactory.Customer(SeedIdentifiers.JeekoCompanyId, "JEE-001", "Jeeko customer", "9876500002");
        var gravisProduct = MasterDataFactory.Product(SeedIdentifiers.GravisCompanyId, "GRA-P1", "Gravis product");
        var jeekoProduct = MasterDataFactory.Product(SeedIdentifiers.JeekoCompanyId, "JEE-P1", "Jeeko product");
        var gravisPayment = MasterDataFactory.PaymentCondition(SeedIdentifiers.GravisCompanyId, "Cash");
        var jeekoPayment = MasterDataFactory.PaymentCondition(SeedIdentifiers.JeekoCompanyId, "Credit");
        var gravisTransporter = MasterDataFactory.Transporter(SeedIdentifiers.GravisCompanyId, "Mehta Transport");
        var jeekoTransporter = MasterDataFactory.Transporter(SeedIdentifiers.JeekoCompanyId, "Haresh Transport");

        dbContext.Customers.AddRange(gravisCustomer, jeekoCustomer);
        dbContext.Products.AddRange(gravisProduct, jeekoProduct);
        dbContext.PaymentConditions.AddRange(gravisPayment, jeekoPayment);
        dbContext.Transporters.AddRange(gravisTransporter, jeekoTransporter);
        await dbContext.SaveChangesAsync();

        return new SeededMasters(
            gravisCustomer.Id,
            jeekoCustomer.Id,
            gravisProduct.Id,
            jeekoProduct.Id,
            gravisPayment.Id,
            jeekoPayment.Id,
            gravisTransporter.Id,
            jeekoTransporter.Id);
    }

    private sealed record SeededMasters(
        Guid GravisCustomerId,
        Guid JeekoCustomerId,
        Guid GravisProductId,
        Guid JeekoProductId,
        Guid GravisPaymentId,
        Guid JeekoPaymentId,
        Guid GravisTransporterId,
        Guid JeekoTransporterId);
}
