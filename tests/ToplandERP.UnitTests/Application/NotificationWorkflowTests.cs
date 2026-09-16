using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Dispatching;
using ToplandERP.Application.Modifications;
using ToplandERP.Application.Notifications;
using ToplandERP.Application.Orders;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Domain.Enums;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class NotificationWorkflowTests
{
    [Fact]
    public async Task Order_created_notifies_company_admin_and_super_admin_not_sales_creator()
    {
        var fixture = await SeedAsync();
        await using var dbContext = CreateContext(fixture.DatabaseName, fixture.Sales);
        await CreateOrderService(dbContext, fixture.Sales, fixture.Directory).CreateAsync(ValidRequest(fixture.Masters));

        await AssertTypesAsync(fixture, fixture.Admin.UserId!.Value, NotificationType.OrderCreated);
        await AssertTypesAsync(fixture, fixture.SuperAdmin.UserId!.Value, NotificationType.OrderCreated);
        await AssertNoneAsync(fixture, fixture.Sales.UserId!.Value);
        await AssertNoneAsync(fixture, fixture.Dispatch.UserId!.Value);
    }

    [Fact]
    public async Task Ready_to_dispatch_notifies_admin_dispatch_and_super_admin()
    {
        var fixture = await SeedAsync();
        var orderId = await CreateOrderAsync(fixture);
        await using (var dbContext = CreateContext(fixture.DatabaseName, fixture.Admin))
        {
            await CreateOrderService(dbContext, fixture.Admin, fixture.Directory).MarkReadyToDispatchAsync(orderId);
        }

        await AssertTypesAsync(fixture, fixture.Admin.UserId!.Value, NotificationType.OrderCreated, NotificationType.OrderReadyToDispatch);
        await AssertTypesAsync(fixture, fixture.Dispatch.UserId!.Value, NotificationType.OrderReadyToDispatch);
        await AssertTypesAsync(fixture, fixture.SuperAdmin.UserId!.Value, NotificationType.OrderCreated, NotificationType.OrderReadyToDispatch);
        await AssertNoneAsync(fixture, fixture.Sales.UserId!.Value);
    }

    [Fact]
    public async Task Dispatch_done_notifies_sales_admin_and_super_admin_not_dispatcher()
    {
        var fixture = await SeedAsync();
        var orderId = await CreateOrderAsync(fixture);
        await using (var ready = CreateContext(fixture.DatabaseName, fixture.Admin))
        {
            await CreateOrderService(ready, fixture.Admin, fixture.Directory).MarkReadyToDispatchAsync(orderId);
        }

        await using (var dispatchContext = CreateContext(fixture.DatabaseName, fixture.Dispatch))
        {
            await CreateDispatchService(dispatchContext, fixture.Dispatch, fixture.Directory)
                .CompleteAsync(orderId, DispatchRequest(fixture.Masters.GravisTransporterId));
        }

        await AssertTypesAsync(
            fixture,
            fixture.Sales.UserId!.Value,
            NotificationType.OrderDispatched);
        await AssertTypesAsync(
            fixture,
            fixture.Admin.UserId!.Value,
            NotificationType.OrderCreated,
            NotificationType.OrderReadyToDispatch,
            NotificationType.OrderDispatched);
        await AssertTypesAsync(
            fixture,
            fixture.SuperAdmin.UserId!.Value,
            NotificationType.OrderCreated,
            NotificationType.OrderReadyToDispatch,
            NotificationType.OrderDispatched);

        var dispatchInbox = await InboxAsync(fixture, fixture.Dispatch);
        dispatchInbox.Should().OnlyContain(item => item.Type == NotificationType.OrderReadyToDispatch);

        var salesInbox = await InboxAsync(fixture, fixture.Sales);
        salesInbox.Should().ContainSingle(item => item.Type == NotificationType.OrderDispatched && item.Message.Contains("VRL Logistics"));
    }

    [Fact]
    public async Task Modification_requested_approved_and_rejected_notify_the_correct_people()
    {
        var fixture = await SeedAsync();
        var orderId = await CreateOrderAsync(fixture);
        Guid requestId;
        await using (var salesContext = CreateContext(fixture.DatabaseName, fixture.Sales))
        {
            requestId = (await CreateModificationService(salesContext, fixture.Sales, fixture.Directory)
                .RequestAsync(orderId, Modification(fixture.Masters))).Id;
        }

        await AssertTypesAsync(fixture, fixture.Admin.UserId!.Value, NotificationType.OrderCreated, NotificationType.ModificationRequested);
        await AssertTypesAsync(fixture, fixture.SuperAdmin.UserId!.Value, NotificationType.OrderCreated, NotificationType.ModificationRequested);
        (await InboxAsync(fixture, fixture.Sales)).Should().BeEmpty();

        await using (var approveContext = CreateContext(fixture.DatabaseName, fixture.Admin))
        {
            await CreateModificationService(approveContext, fixture.Admin, fixture.Directory).ApproveAsync(requestId);
        }

        var salesAfterApprove = await InboxAsync(fixture, fixture.Sales);
        salesAfterApprove.Should().ContainSingle(item => item.Type == NotificationType.ModificationApproved);

        var secondOrderId = await CreateOrderAsync(fixture);
        Guid rejectId;
        await using (var salesContext = CreateContext(fixture.DatabaseName, fixture.Sales))
        {
            rejectId = (await CreateModificationService(salesContext, fixture.Sales, fixture.Directory)
                .RequestAsync(secondOrderId, Modification(fixture.Masters))).Id;
        }

        await using (var rejectContext = CreateContext(fixture.DatabaseName, fixture.Admin))
        {
            await CreateModificationService(rejectContext, fixture.Admin, fixture.Directory)
                .RejectAsync(rejectId, new RejectModificationRequest { RejectionReason = "Order already prepared for dispatch." });
        }

        var salesInbox = await InboxAsync(fixture, fixture.Sales);
        salesInbox.Should().Contain(item => item.Type == NotificationType.ModificationApproved);
        salesInbox.Should().Contain(item =>
            item.Type == NotificationType.ModificationRejected
            && item.Message.Contains("Order already prepared for dispatch."));
    }

    [Fact]
    public async Task Failed_order_create_does_not_leave_a_notification()
    {
        var fixture = await SeedAsync();
        await using var dbContext = CreateContext(fixture.DatabaseName, fixture.Sales);
        var request = ValidRequest(fixture.Masters);
        request.CustomerId = Guid.NewGuid();
        var act = async () => await CreateOrderService(dbContext, fixture.Sales, fixture.Directory).CreateAsync(request);
        await act.Should().ThrowAsync<BusinessException>();
        (await dbContext.Notifications.IgnoreQueryFilters().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Relational_transaction_rollback_does_not_leave_a_notification()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options;
        await using var dbContext = new ApplicationDbContext(options, SystemCurrentUser.Instance);
        await dbContext.Database.EnsureCreatedAsync();
        dbContext.Companies.Add(new Company
        {
            Id = SeedIdentifiers.GravisCompanyId,
            Name = "Gravis",
            Code = CompanyCodes.Gravis,
            IsActive = true
        });
        await dbContext.SaveChangesAsync();

        var act = async () => await dbContext.ExecuteInTransactionAsync(async token =>
        {
            dbContext.Notifications.Add(new Notification
            {
                CompanyId = SeedIdentifiers.GravisCompanyId,
                RecipientUserId = Guid.NewGuid(),
                Type = NotificationType.OrderCreated,
                Title = "Should roll back",
                Message = "Should roll back",
                EventKey = "rollback-test"
            });
            await dbContext.SaveChangesAsync(token);
            throw new InvalidOperationException("force rollback");
        });

        await act.Should().ThrowAsync<InvalidOperationException>();
        (await dbContext.Notifications.CountAsync()).Should().Be(0);
    }

    private static async Task<Guid> CreateOrderAsync(Fixture fixture)
    {
        await using var dbContext = CreateContext(fixture.DatabaseName, fixture.Sales);
        var created = await CreateOrderService(dbContext, fixture.Sales, fixture.Directory).CreateAsync(ValidRequest(fixture.Masters));
        return created.Id;
    }

    private static async Task AssertTypesAsync(Fixture fixture, Guid recipientId, params NotificationType[] types)
    {
        var user = recipientId == fixture.SuperAdmin.UserId
            ? fixture.SuperAdmin
            : recipientId == fixture.Admin.UserId
                ? fixture.Admin
                : recipientId == fixture.Dispatch.UserId
                    ? fixture.Dispatch
                    : fixture.Sales;
        var inbox = await InboxAsync(fixture, user);
        inbox.Select(item => item.Type).Should().BeEquivalentTo(types);
    }

    private static async Task AssertNoneAsync(Fixture fixture, Guid recipientId)
    {
        var user = recipientId == fixture.Sales.UserId ? fixture.Sales : fixture.Dispatch;
        (await InboxAsync(fixture, user)).Should().BeEmpty();
    }

    private static async Task<IReadOnlyList<NotificationDto>> InboxAsync(Fixture fixture, TestCurrentUser user)
    {
        await using var dbContext = CreateContext(fixture.DatabaseName, user);
        var page = await new NotificationService(dbContext, user, fixture.Directory)
            .GetUserNotificationsAsync(new NotificationListQuery { PageSize = 50 });
        return page.Items;
    }

    private static OrderService CreateOrderService(
        ApplicationDbContext dbContext,
        ICurrentUser user,
        TestUserDirectory directory) =>
        new(dbContext, user, new TestAuditLogger(), new NotificationService(dbContext, user, directory), new CreateOrderRequestValidator());

    private static ModificationService CreateModificationService(
        ApplicationDbContext dbContext,
        ICurrentUser user,
        TestUserDirectory directory) =>
        new(dbContext, user, new TestAuditLogger(), new NotificationService(dbContext, user, directory), new CreateModificationRequestValidator(), new RejectModificationRequestValidator());

    private static DispatchService CreateDispatchService(
        ApplicationDbContext dbContext,
        ICurrentUser user,
        TestUserDirectory directory) =>
        new(dbContext, user, new TestAuditLogger(), new NotificationService(dbContext, user, directory), new TestFileStorage(), new CompleteDispatchRequestValidator());

    private static ApplicationDbContext CreateContext(string databaseName, ICurrentUser currentUser) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options, currentUser);

    private static CreateOrderRequest ValidRequest(SeededMasters masters) => new()
    {
        CustomerId = masters.GravisCustomerId,
        PaymentConditionId = masters.GravisPaymentId,
        TransporterId = masters.GravisTransporterId,
        BillingAddress = "Bill",
        BillingCity = "Ahmedabad",
        BillingState = "Gujarat",
        BillingPincode = "380001",
        DeliveryAddress = "Del",
        DeliveryCity = "Rajkot",
        DeliveryState = "Gujarat",
        DeliveryPincode = "360001",
        BillAmount = 1000,
        Items = [new OrderLineRequest { ProductId = masters.GravisProductId, Quantity = 1 }]
    };

    private static CreateModificationRequest Modification(SeededMasters masters) => new()
    {
        Reason = "Customer requested different model.",
        PaymentConditionId = masters.GravisPaymentId,
        TransporterId = masters.GravisTransporterId,
        BillingAddress = "Bill",
        BillingCity = "Ahmedabad",
        BillingState = "Gujarat",
        BillingPincode = "380001",
        DeliveryAddress = "Del",
        DeliveryCity = "Rajkot",
        DeliveryState = "Gujarat",
        DeliveryPincode = "360001",
        BillAmount = 1100,
        Items = [new OrderLineRequest { ProductId = masters.GravisProduct2Id, Quantity = 1 }]
    };

    private static CompleteDispatchRequest DispatchRequest(Guid transporterId) => new()
    {
        DispatchDate = DateTime.UtcNow.Date,
        DispatchPersonName = "Rakesh",
        TransporterId = transporterId,
        LrNumber = "LR-88",
        Files =
        [
            new DispatchFileUpload
            {
                Content = new MemoryStream("photo"u8.ToArray()),
                OriginalFileName = "material.jpg",
                ContentType = "image/jpeg",
                DocumentType = DispatchDocumentType.MaterialPhoto
            }
        ]
    };

    private static async Task<Fixture> SeedAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var dbContext = CreateContext(databaseName, SystemCurrentUser.Instance);
        dbContext.Companies.AddRange(
            new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
            new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true });
        var gravisCustomer = MasterDataFactory.Customer(SeedIdentifiers.GravisCompanyId, "GRA-001", "Jay Jalaram Electronics", "9876500001");
        var gravisProduct = MasterDataFactory.Product(SeedIdentifiers.GravisCompanyId, "GRA-P1", "Mini Tractor");
        var gravisProduct2 = MasterDataFactory.Product(SeedIdentifiers.GravisCompanyId, "GRA-P2", "Power Tiller");
        var gravisPayment = MasterDataFactory.PaymentCondition(SeedIdentifiers.GravisCompanyId, "Cash");
        var gravisTransporter = MasterDataFactory.Transporter(SeedIdentifiers.GravisCompanyId, "VRL Logistics");
        dbContext.Customers.Add(gravisCustomer);
        dbContext.Products.AddRange(gravisProduct, gravisProduct2);
        dbContext.PaymentConditions.Add(gravisPayment);
        dbContext.Transporters.Add(gravisTransporter);
        await dbContext.SaveChangesAsync();

        var superAdmin = new TestCurrentUser { Roles = [RoleNames.SuperAdmin], FullName = "Management" };
        var admin = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.CompanyAdmin], FullName = "Gravis Admin" };
        var sales = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.SalesEmployee], FullName = "Parthiv" };
        var dispatch = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.DispatchUser], FullName = "Dispatch" };
        var directory = TestUserDirectory.Standard(
            superAdmin.UserId!.Value,
            admin.UserId!.Value,
            dispatch.UserId!.Value,
            sales.UserId,
            SeedIdentifiers.GravisCompanyId);

        return new Fixture(
            databaseName,
            new SeededMasters(gravisCustomer.Id, gravisProduct.Id, gravisProduct2.Id, gravisPayment.Id, gravisTransporter.Id),
            superAdmin,
            admin,
            sales,
            dispatch,
            directory);
    }

    private sealed record Fixture(
        string DatabaseName,
        SeededMasters Masters,
        TestCurrentUser SuperAdmin,
        TestCurrentUser Admin,
        TestCurrentUser Sales,
        TestCurrentUser Dispatch,
        TestUserDirectory Directory);

    private sealed record SeededMasters(
        Guid GravisCustomerId,
        Guid GravisProductId,
        Guid GravisProduct2Id,
        Guid GravisPaymentId,
        Guid GravisTransporterId);
}
