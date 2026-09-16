using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Customers;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class CustomerServiceTests
{
    [Fact]
    public async Task CompanyAdmin_sees_only_own_company_customers()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedAsync(databaseName);

        var currentUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.GravisCompanyId,
            Roles = [RoleNames.CompanyAdmin]
        };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = CreateService(dbContext, currentUser);

        var result = await service.GetPagedAsync(new PagedQuery());

        result.Items.Should().OnlyContain(item => item.CompanyId == SeedIdentifiers.GravisCompanyId);
        result.Items.Should().ContainSingle(item => item.CustomerCode == "GRA-001");
        result.Items.Should().NotContain(item => item.CustomerCode == "JEE-001");
    }

    [Fact]
    public async Task SalesEmployee_sees_only_own_company_customers()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedAsync(databaseName);

        var currentUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.JeekoCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = CreateService(dbContext, currentUser);

        var result = await service.GetPagedAsync(new PagedQuery());

        result.Items.Should().OnlyContain(item => item.CompanyId == SeedIdentifiers.JeekoCompanyId);
    }

    [Fact]
    public async Task Cross_company_customer_access_is_denied()
    {
        var databaseName = Guid.NewGuid().ToString();
        var otherId = Guid.Empty;
        await SeedAsync(databaseName, customer =>
        {
            if (customer.CustomerCode == "JEE-001")
            {
                otherId = customer.Id;
            }
        });

        var currentUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.GravisCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = CreateService(dbContext, currentUser);

        var customer = await service.GetByIdAsync(otherId);
        customer.Should().BeNull();
    }

    [Fact]
    public async Task SuperAdmin_can_access_all_companies()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedAsync(databaseName);

        var currentUser = new TestCurrentUser { Roles = [RoleNames.SuperAdmin] };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = CreateService(dbContext, currentUser);

        var result = await service.GetPagedAsync(new PagedQuery());
        result.Items.Select(item => item.CompanyCode).Should().BeEquivalentTo("GRAVIS", "JEEKO");
    }

    [Fact]
    public async Task SalesEmployee_creates_customer_for_own_company_ignoring_forged_company()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedAsync(databaseName);

        var currentUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.GravisCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = CreateService(dbContext, currentUser);

        var created = await service.CreateAsync(ValidRequest(SeedIdentifiers.JeekoCompanyId, "GRA-NEW", "9876500999"));

        created.CompanyId.Should().Be(SeedIdentifiers.GravisCompanyId);
    }

    [Fact]
    public async Task Duplicate_mobile_in_same_company_is_rejected()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedAsync(databaseName);

        var currentUser = new TestCurrentUser
        {
            CompanyId = SeedIdentifiers.GravisCompanyId,
            Roles = [RoleNames.CompanyAdmin]
        };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = CreateService(dbContext, currentUser);

        var act = async () => await service.CreateAsync(ValidRequest(SeedIdentifiers.GravisCompanyId, "GRA-DUP", "9876500001"));

        await act.Should().ThrowAsync<BusinessException>()
            .WithMessage("*mobile*");
    }

    private static CustomerWriteRequest ValidRequest(Guid companyId, string code, string mobile)
    {
        return new CustomerWriteRequest
        {
            CompanyId = companyId,
            CustomerCode = code,
            CustomerName = "Demo Customer",
            Mobile = mobile,
            BillingAddress = "Navrangpura",
            BillingCity = "Ahmedabad",
            BillingState = "Gujarat",
            BillingPincode = "380009",
            DeliveryAddress = "Kalawad Road",
            DeliveryCity = "Rajkot",
            DeliveryState = "Gujarat",
            DeliveryPincode = "360005"
        };
    }

    private static CustomerService CreateService(ApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        return new CustomerService(dbContext, currentUser, new TestAuditLogger(), new CustomerWriteRequestValidator());
    }

    private static ApplicationDbContext CreateContext(string databaseName, ICurrentUser currentUser)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
        return new ApplicationDbContext(options, currentUser);
    }

    private static async Task SeedAsync(string databaseName, Action<Customer>? inspect = null)
    {
        await using var dbContext = CreateContext(databaseName, SystemCurrentUser.Instance);
        dbContext.Companies.AddRange(
            new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis India Private Limited", Code = CompanyCodes.Gravis, IsActive = true },
            new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko Agritech LLP", Code = CompanyCodes.Jeeko, IsActive = true });

        var gravis = MasterDataFactory.Customer(SeedIdentifiers.GravisCompanyId, "GRA-001", "Gravis customer", "9876500001");
        var jeeko = MasterDataFactory.Customer(SeedIdentifiers.JeekoCompanyId, "JEE-001", "Jeeko customer", "9876500002");
        inspect?.Invoke(gravis);
        inspect?.Invoke(jeeko);
        dbContext.Customers.AddRange(gravis, jeeko);
        await dbContext.SaveChangesAsync();
    }
}
