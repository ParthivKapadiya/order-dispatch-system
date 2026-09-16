using FluentAssertions;
using FluentValidation.TestHelper;
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

public class OrderWorkflowTests
{
    [Fact]
    public void Modification_requires_reason_and_requested_changes()
    {
        var validator = new CreateModificationRequestValidator();
        var result = validator.TestValidate(new CreateModificationRequest());
        result.ShouldHaveValidationErrorFor(request => request.Reason);
        result.ShouldHaveValidationErrorFor(request => request.Items);
    }

    [Fact]
    public void Rejection_requires_reason()
    {
        var validator = new RejectModificationRequestValidator();
        validator.TestValidate(new RejectModificationRequest())
            .ShouldHaveValidationErrorFor(request => request.RejectionReason);
    }

    [Fact]
    public async Task Sales_employee_can_request_modification_for_own_order()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        await using var dbContext = CreateContext(databaseName, sales);
        var service = CreateModificationService(dbContext, sales);

        var created = await service.RequestAsync(orderId, Modification(masters, masters.GravisProduct2Id, "Customer requested different model."));

        created.Status.Should().Be(ModificationRequestStatus.Pending);
        created.Requested.Items.Should().ContainSingle(item => item.ProductId == masters.GravisProduct2Id);
        var unchanged = await CreateOrderService(dbContext, sales).GetByIdAsync(orderId);
        unchanged!.Items.Should().ContainSingle(item => item.ProductId == masters.GravisProductId);
    }

    [Fact]
    public async Task Sales_employee_cannot_request_another_employees_order()
    {
        var (databaseName, masters) = await SeedAsync();
        var owner = SalesUser();
        var other = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, owner, masters);
        await using var dbContext = CreateContext(databaseName, other);
        var act = async () => await CreateModificationService(dbContext, other)
            .RequestAsync(orderId, Modification(masters, masters.GravisProduct2Id, "Change model"));
        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Sales_employee_cannot_approve_or_reject()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var requestId = await CreatePendingRequestAsync(databaseName, sales, masters);
        await using var dbContext = CreateContext(databaseName, sales);
        var service = CreateModificationService(dbContext, sales);
        await service.Invoking(item => item.ApproveAsync(requestId)).Should().ThrowAsync<ForbiddenException>();
        await service.Invoking(item => item.RejectAsync(requestId, new RejectModificationRequest { RejectionReason = "No" }))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Company_admin_approves_own_company_request_and_updates_order()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        Guid requestId;
        await using (var createContext = CreateContext(databaseName, sales))
        {
            requestId = (await CreateModificationService(createContext, sales)
                .RequestAsync(orderId, Modification(masters, masters.GravisProduct2Id, "Customer requested different model."))).Id;
        }

        var admin = AdminUser();
        await using var dbContext = CreateContext(databaseName, admin);
        var audit = new TestAuditLogger();
        var approved = await CreateModificationService(dbContext, admin, audit).ApproveAsync(requestId);
        approved.Status.Should().Be(ModificationRequestStatus.Approved);
        var order = await CreateOrderService(dbContext, admin).GetByIdAsync(orderId);
        order!.Items.Should().ContainSingle(item => item.ProductId == masters.GravisProduct2Id);
        audit.Entries.Should().Contain(item => item.Action == "OrderModificationApproved");
    }

    [Fact]
    public async Task Rejected_request_does_not_change_order()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        var requestId = await CreatePendingRequestAsync(databaseName, sales, masters, orderId);
        var admin = AdminUser();
        await using var dbContext = CreateContext(databaseName, admin);
        await CreateModificationService(dbContext, admin).RejectAsync(requestId, new RejectModificationRequest
        {
            RejectionReason = "Material already prepared"
        });
        var order = await CreateOrderService(dbContext, admin).GetByIdAsync(orderId);
        order!.Items.Should().ContainSingle(item => item.ProductId == masters.GravisProductId);
    }

    [Fact]
    public async Task Double_approval_is_prevented()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var requestId = await CreatePendingRequestAsync(databaseName, sales, masters);
        var admin = AdminUser();
        await using var dbContext = CreateContext(databaseName, admin);
        var service = CreateModificationService(dbContext, admin);
        await service.ApproveAsync(requestId);
        await service.Invoking(item => item.ApproveAsync(requestId))
            .Should().ThrowAsync<BusinessException>().WithMessage("*already been processed*");
    }

    [Fact]
    public async Task Multiple_pending_requests_are_prevented()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        await using var dbContext = CreateContext(databaseName, sales);
        var service = CreateModificationService(dbContext, sales);
        await service.RequestAsync(orderId, Modification(masters, masters.GravisProduct2Id, "First"));
        await service.Invoking(item => item.RequestAsync(orderId, Modification(masters, masters.GravisProduct2Id, "Second")))
            .Should().ThrowAsync<BusinessException>().WithMessage("*pending modification request*");
    }

    [Fact]
    public async Task Company_admin_cannot_see_other_company_request()
    {
        var (databaseName, masters) = await SeedAsync();
        var jeekoSales = new TestCurrentUser { CompanyId = SeedIdentifiers.JeekoCompanyId, Roles = [RoleNames.SalesEmployee] };
        var requestId = await CreatePendingRequestAsync(databaseName, jeekoSales, masters, jeeko: true);
        var admin = AdminUser();
        await using var dbContext = CreateContext(databaseName, admin);
        (await CreateModificationService(dbContext, admin).GetByIdAsync(requestId)).Should().BeNull();
    }

    [Fact]
    public async Task SuperAdmin_can_approve_across_companies()
    {
        var (databaseName, masters) = await SeedAsync();
        var jeekoSales = new TestCurrentUser { CompanyId = SeedIdentifiers.JeekoCompanyId, Roles = [RoleNames.SalesEmployee] };
        var requestId = await CreatePendingRequestAsync(databaseName, jeekoSales, masters, jeeko: true);
        var superAdmin = new TestCurrentUser { Roles = [RoleNames.SuperAdmin] };
        await using var dbContext = CreateContext(databaseName, superAdmin);
        var approved = await CreateModificationService(dbContext, superAdmin).ApproveAsync(requestId);
        approved.Status.Should().Be(ModificationRequestStatus.Approved);
    }

    [Fact]
    public async Task Dispatch_user_cannot_approve_modifications()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var requestId = await CreatePendingRequestAsync(databaseName, sales, masters);
        var dispatch = DispatchUser();
        await using var dbContext = CreateContext(databaseName, dispatch);
        await CreateModificationService(dbContext, dispatch).Invoking(item => item.ApproveAsync(requestId))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Received_can_move_to_ready_and_invalid_transitions_are_rejected()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        var admin = AdminUser();
        await using var dbContext = CreateContext(databaseName, admin);
        var service = CreateOrderService(dbContext, admin);
        var ready = await service.MarkReadyToDispatchAsync(orderId);
        ready.Status.Should().Be(OrderStatus.ReadyToDispatch);
        await service.Invoking(item => item.MarkReadyToDispatchAsync(orderId))
            .Should().ThrowAsync<BusinessException>();
        await CreateOrderService(CreateContext(databaseName, sales), sales)
            .Invoking(item => item.MarkReadyToDispatchAsync(orderId))
            .Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Pending_modification_blocks_ready_and_dispatch()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        await CreatePendingRequestAsync(databaseName, sales, masters, orderId);
        var admin = AdminUser();
        await using var dbContext = CreateContext(databaseName, admin);
        await CreateOrderService(dbContext, admin).Invoking(item => item.MarkReadyToDispatchAsync(orderId))
            .Should().ThrowAsync<BusinessException>().WithMessage("*pending*");
    }

    [Fact]
    public async Task Dispatch_user_can_complete_own_company_order_and_lock_it()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        var admin = AdminUser();
        await using (var readyContext = CreateContext(databaseName, admin))
        {
            await CreateOrderService(readyContext, admin).MarkReadyToDispatchAsync(orderId);
        }

        var dispatchUser = DispatchUser();
        var files = new TestFileStorage();
        var audit = new TestAuditLogger();
        await using var dbContext = CreateContext(databaseName, dispatchUser);
        var completed = await CreateDispatchService(dbContext, dispatchUser, files, audit).CompleteAsync(orderId, DispatchRequest(masters));
        completed.IsCompleted.Should().BeTrue();
        completed.OrderStatus.Should().Be(OrderStatus.DispatchDone);
        files.Saved.Should().NotBeEmpty();
        audit.Entries.Should().Contain(item => item.Action == "OrderLocked");

        await using var salesContext = CreateContext(databaseName, sales);
        await CreateModificationService(salesContext, sales)
            .Invoking(item => item.RequestAsync(orderId, Modification(masters, masters.GravisProduct2Id, "Too late")))
            .Should().ThrowAsync<BusinessException>().WithMessage("*locked*");
        await CreateDispatchService(dbContext, dispatchUser, files).Invoking(item => item.CompleteAsync(orderId, DispatchRequest(masters)))
            .Should().ThrowAsync<BusinessException>().WithMessage("*already been dispatched*");
    }

    [Fact]
    public async Task Dispatch_user_cannot_dispatch_another_company_order()
    {
        var (databaseName, masters) = await SeedAsync();
        var jeekoSales = new TestCurrentUser { CompanyId = SeedIdentifiers.JeekoCompanyId, Roles = [RoleNames.SalesEmployee] };
        var orderId = await CreateReceivedOrderAsync(databaseName, jeekoSales, masters, jeeko: true);
        var jeekoAdmin = new TestCurrentUser { CompanyId = SeedIdentifiers.JeekoCompanyId, Roles = [RoleNames.CompanyAdmin] };
        await using (var readyContext = CreateContext(databaseName, jeekoAdmin))
        {
            await CreateOrderService(readyContext, jeekoAdmin).MarkReadyToDispatchAsync(orderId);
        }

        var gravisDispatch = DispatchUser();
        await using var dbContext = CreateContext(databaseName, gravisDispatch);
        (await CreateDispatchService(dbContext, gravisDispatch).GetByOrderIdAsync(orderId)).Should().BeNull();
        await CreateDispatchService(dbContext, gravisDispatch)
            .Invoking(item => item.CompleteAsync(orderId, DispatchRequest(masters)))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cross_company_transporter_is_rejected_on_dispatch()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        var admin = AdminUser();
        await using (var readyContext = CreateContext(databaseName, admin))
        {
            await CreateOrderService(readyContext, admin).MarkReadyToDispatchAsync(orderId);
        }

        var dispatchUser = DispatchUser();
        await using var dbContext = CreateContext(databaseName, dispatchUser);
        var request = DispatchRequest(masters);
        request.TransporterId = masters.JeekoTransporterId;
        await CreateDispatchService(dbContext, dispatchUser)
            .Invoking(item => item.CompleteAsync(orderId, request))
            .Should().ThrowAsync<BusinessException>().WithMessage("*transporter*");
    }

    [Fact]
    public async Task Invalid_and_oversized_uploads_are_rejected()
    {
        var (databaseName, masters) = await SeedAsync();
        var sales = SalesUser();
        var orderId = await CreateReceivedOrderAsync(databaseName, sales, masters);
        var admin = AdminUser();
        await using (var readyContext = CreateContext(databaseName, admin))
        {
            await CreateOrderService(readyContext, admin).MarkReadyToDispatchAsync(orderId);
        }

        var dispatchUser = DispatchUser();
        await using var invalidContext = CreateContext(databaseName, dispatchUser);
        var invalidStorage = new TestFileStorage { SaveException = new InvalidOperationException("Invalid file type.") };
        await CreateDispatchService(invalidContext, dispatchUser, invalidStorage)
            .Invoking(item => item.CompleteAsync(orderId, DispatchRequest(masters)))
            .Should().ThrowAsync<BusinessException>().WithMessage("Invalid file type.");

        await using var oversizedContext = CreateContext(databaseName, dispatchUser);
        var oversizedStorage = new TestFileStorage { SaveException = new InvalidOperationException("File size exceeds the allowed limit.") };
        await CreateDispatchService(oversizedContext, dispatchUser, oversizedStorage)
            .Invoking(item => item.CompleteAsync(orderId, DispatchRequest(masters)))
            .Should().ThrowAsync<BusinessException>().WithMessage("File size exceeds the allowed limit.");
    }

    private static TestCurrentUser SalesUser() =>
        new() { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.SalesEmployee], FullName = "Rahul" };

    private static TestCurrentUser AdminUser() =>
        new() { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.CompanyAdmin], FullName = "Admin" };

    private static TestCurrentUser DispatchUser() =>
        new() { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.DispatchUser], FullName = "Godown" };

    private static CreateModificationRequest Modification(SeededMasters masters, Guid productId, string reason, bool jeeko = false)
    {
        return new CreateModificationRequest
        {
            Reason = reason,
            BillAmount = 99000,
            PaymentConditionId = jeeko ? masters.JeekoPaymentId : masters.GravisPaymentId,
            TransporterId = jeeko ? masters.JeekoTransporterId : masters.GravisTransporterId,
            BillingAddress = "Navrangpura",
            BillingCity = "Ahmedabad",
            BillingState = "Gujarat",
            BillingPincode = "380009",
            DeliveryAddress = "Kalawad Road",
            DeliveryCity = "Rajkot",
            DeliveryState = "Gujarat",
            DeliveryPincode = "360005",
            Items = [new OrderLineRequest { ProductId = productId, Quantity = 2 }]
        };
    }

    private static CompleteDispatchRequest DispatchRequest(SeededMasters masters) => new()
    {
        DispatchDate = DateTime.UtcNow.Date,
        DispatchPersonName = "Godown",
        TransporterId = masters.GravisTransporterId,
        LrNumber = "LR-7788",
        Files =
        [
            new DispatchFileUpload
            {
                Content = new MemoryStream("photo"u8.ToArray()),
                OriginalFileName = "material.jpg",
                ContentType = "image/jpeg",
                DocumentType = DispatchDocumentType.MaterialPhoto
            },
            new DispatchFileUpload
            {
                Content = new MemoryStream("receipt"u8.ToArray()),
                OriginalFileName = "lr.pdf",
                ContentType = "application/pdf",
                DocumentType = DispatchDocumentType.LrDocument
            }
        ]
    };

    private static async Task<Guid> CreateReceivedOrderAsync(
        string databaseName,
        TestCurrentUser user,
        SeededMasters masters,
        bool jeeko = false)
    {
        await using var dbContext = CreateContext(databaseName, user);
        var created = await CreateOrderService(dbContext, user).CreateAsync(new CreateOrderRequest
        {
            CustomerId = jeeko ? masters.JeekoCustomerId : masters.GravisCustomerId,
            PaymentConditionId = jeeko ? masters.JeekoPaymentId : masters.GravisPaymentId,
            TransporterId = jeeko ? masters.JeekoTransporterId : masters.GravisTransporterId,
            BillingAddress = "Bill",
            BillingCity = "Ahmedabad",
            BillingState = "Gujarat",
            BillingPincode = "380001",
            DeliveryAddress = "Del",
            DeliveryCity = "Rajkot",
            DeliveryState = "Gujarat",
            DeliveryPincode = "360001",
            BillAmount = 1000,
            Items = [new OrderLineRequest { ProductId = jeeko ? masters.JeekoProductId : masters.GravisProductId, Quantity = 1 }]
        });
        return created.Id;
    }

    private static async Task<Guid> CreatePendingRequestAsync(
        string databaseName,
        TestCurrentUser sales,
        SeededMasters masters,
        Guid? orderId = null,
        bool jeeko = false)
    {
        orderId ??= await CreateReceivedOrderAsync(databaseName, sales, masters, jeeko);
        await using var dbContext = CreateContext(databaseName, sales);
        var created = await CreateModificationService(dbContext, sales)
            .RequestAsync(orderId.Value, Modification(masters, jeeko ? masters.JeekoProductId : masters.GravisProduct2Id, "Change", jeeko));
        return created.Id;
    }

    private static OrderService CreateOrderService(ApplicationDbContext dbContext, ICurrentUser user) =>
        new(dbContext, user, new TestAuditLogger(), CreateNotificationService(dbContext, user), new CreateOrderRequestValidator());

    private static ModificationService CreateModificationService(
        ApplicationDbContext dbContext,
        ICurrentUser user,
        TestAuditLogger? audit = null) =>
        new(dbContext, user, audit ?? new TestAuditLogger(), CreateNotificationService(dbContext, user), new CreateModificationRequestValidator(), new RejectModificationRequestValidator());

    private static DispatchService CreateDispatchService(
        ApplicationDbContext dbContext,
        ICurrentUser user,
        IFileStorage? files = null,
        TestAuditLogger? audit = null) =>
        new(dbContext, user, audit ?? new TestAuditLogger(), CreateNotificationService(dbContext, user), files ?? new TestFileStorage(), new CompleteDispatchRequestValidator());

    private static NotificationService CreateNotificationService(ApplicationDbContext dbContext, ICurrentUser user) =>
        new(dbContext, user, new TestUserDirectory());

    private static ApplicationDbContext CreateContext(string databaseName, ICurrentUser currentUser) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options, currentUser);

    private static async Task<(string DatabaseName, SeededMasters Masters)> SeedAsync()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using var dbContext = CreateContext(databaseName, SystemCurrentUser.Instance);
        dbContext.Companies.AddRange(
            new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
            new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true });
        var gravisCustomer = MasterDataFactory.Customer(SeedIdentifiers.GravisCompanyId, "GRA-001", "Gravis customer", "9876500001");
        var jeekoCustomer = MasterDataFactory.Customer(SeedIdentifiers.JeekoCompanyId, "JEE-001", "Jeeko customer", "9876500002");
        var gravisProduct = MasterDataFactory.Product(SeedIdentifiers.GravisCompanyId, "GRA-P1", "Mini Tractor");
        var gravisProduct2 = MasterDataFactory.Product(SeedIdentifiers.GravisCompanyId, "GRA-P2", "Power Tiller");
        var jeekoProduct = MasterDataFactory.Product(SeedIdentifiers.JeekoCompanyId, "JEE-P1", "Jeeko product");
        var gravisPayment = MasterDataFactory.PaymentCondition(SeedIdentifiers.GravisCompanyId, "Cash");
        var jeekoPayment = MasterDataFactory.PaymentCondition(SeedIdentifiers.JeekoCompanyId, "Credit");
        var gravisTransporter = MasterDataFactory.Transporter(SeedIdentifiers.GravisCompanyId, "VRL Logistics");
        var jeekoTransporter = MasterDataFactory.Transporter(SeedIdentifiers.JeekoCompanyId, "Gati KWE");
        dbContext.Customers.AddRange(gravisCustomer, jeekoCustomer);
        dbContext.Products.AddRange(gravisProduct, gravisProduct2, jeekoProduct);
        dbContext.PaymentConditions.AddRange(gravisPayment, jeekoPayment);
        dbContext.Transporters.AddRange(gravisTransporter, jeekoTransporter);
        await dbContext.SaveChangesAsync();
        return (databaseName, new SeededMasters(
            gravisCustomer.Id, jeekoCustomer.Id, gravisProduct.Id, gravisProduct2.Id, jeekoProduct.Id,
            gravisPayment.Id, jeekoPayment.Id, gravisTransporter.Id, jeekoTransporter.Id));
    }

    private sealed record SeededMasters(
        Guid GravisCustomerId,
        Guid JeekoCustomerId,
        Guid GravisProductId,
        Guid GravisProduct2Id,
        Guid JeekoProductId,
        Guid GravisPaymentId,
        Guid JeekoPaymentId,
        Guid GravisTransporterId,
        Guid JeekoTransporterId);
}
