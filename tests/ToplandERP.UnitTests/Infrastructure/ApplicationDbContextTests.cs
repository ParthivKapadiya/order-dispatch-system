using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;
using ToplandERP.Domain.Constants;

namespace ToplandERP.UnitTests.Infrastructure;

public class ApplicationDbContextTests
{
    [Fact]
    public async Task Company_query_filter_hides_other_company_customers()
    {
        var ownCompanyId = SeedIdentifiers.GravisCompanyId;
        var otherCompanyId = SeedIdentifiers.JeekoCompanyId;

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var seedContext = new ApplicationDbContext(options, SystemCurrentUser.Instance))
        {
            seedContext.Companies.AddRange(
                new Company { Id = ownCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
                new Company { Id = otherCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true });
            seedContext.Customers.AddRange(
                new Customer
                {
                    Id = Guid.NewGuid(),
                    CompanyId = ownCompanyId,
                    CustomerCode = "CUST-OWN",
                    CustomerName = "Own customer",
                    Mobile = "9876543210",
                    BillingAddress = "Billing street",
                    BillingCity = "Ahmedabad",
                    BillingState = "Gujarat",
                    BillingPincode = "380001",
                    DeliveryAddress = "Delivery street",
                    DeliveryCity = "Rajkot",
                    DeliveryState = "Gujarat",
                    DeliveryPincode = "360001"
                },
                new Customer
                {
                    Id = Guid.NewGuid(),
                    CompanyId = otherCompanyId,
                    CustomerCode = "CUST-OTH",
                    CustomerName = "Other customer",
                    Mobile = "9876543211",
                    BillingAddress = "Billing street",
                    BillingCity = "Ahmedabad",
                    BillingState = "Gujarat",
                    BillingPincode = "380001",
                    DeliveryAddress = "Delivery street",
                    DeliveryCity = "Rajkot",
                    DeliveryState = "Gujarat",
                    DeliveryPincode = "360001"
                });
            await seedContext.SaveChangesAsync();
        }

        var currentUser = new TestCurrentUser
        {
            CompanyId = ownCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };

        await using var queryContext = new ApplicationDbContext(options, currentUser);
        var customers = await queryContext.Customers.ToListAsync();

        customers.Should().ContainSingle(customer => customer.CustomerName == "Own customer");
    }

    [Fact]
    public async Task SuperAdmin_query_filter_returns_all_company_customers()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using (var seedContext = new ApplicationDbContext(options, SystemCurrentUser.Instance))
        {
            seedContext.Companies.AddRange(
                new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
                new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true });
            seedContext.Customers.AddRange(
                new Customer
                {
                    Id = Guid.NewGuid(),
                    CompanyId = SeedIdentifiers.GravisCompanyId,
                    CustomerCode = "A1",
                    CustomerName = "A",
                    Mobile = "9876500001",
                    BillingAddress = "Addr",
                    BillingCity = "Ahmedabad",
                    BillingState = "Gujarat",
                    BillingPincode = "380001",
                    DeliveryAddress = "Addr",
                    DeliveryCity = "Rajkot",
                    DeliveryState = "Gujarat",
                    DeliveryPincode = "360001"
                },
                new Customer
                {
                    Id = Guid.NewGuid(),
                    CompanyId = SeedIdentifiers.JeekoCompanyId,
                    CustomerCode = "B1",
                    CustomerName = "B",
                    Mobile = "9876500002",
                    BillingAddress = "Addr",
                    BillingCity = "Ahmedabad",
                    BillingState = "Gujarat",
                    BillingPincode = "380001",
                    DeliveryAddress = "Addr",
                    DeliveryCity = "Rajkot",
                    DeliveryState = "Gujarat",
                    DeliveryPincode = "360001"
                });
            await seedContext.SaveChangesAsync();
        }

        var superAdmin = new TestCurrentUser { Roles = [RoleNames.SuperAdmin], CompanyId = null };
        await using var queryContext = new ApplicationDbContext(options, superAdmin);
        (await queryContext.Customers.CountAsync()).Should().Be(2);
    }
}
