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
                new Customer { Id = Guid.NewGuid(), CompanyId = ownCompanyId, Name = "Own customer" },
                new Customer { Id = Guid.NewGuid(), CompanyId = otherCompanyId, Name = "Other customer" });
            await seedContext.SaveChangesAsync();
        }

        var currentUser = new TestCurrentUser
        {
            CompanyId = ownCompanyId,
            Roles = [RoleNames.SalesEmployee]
        };

        await using var queryContext = new ApplicationDbContext(options, currentUser);
        var customers = await queryContext.Customers.ToListAsync();

        customers.Should().ContainSingle(customer => customer.Name == "Own customer");
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
                new Customer { Id = Guid.NewGuid(), CompanyId = SeedIdentifiers.GravisCompanyId, Name = "A" },
                new Customer { Id = Guid.NewGuid(), CompanyId = SeedIdentifiers.JeekoCompanyId, Name = "B" });
            await seedContext.SaveChangesAsync();
        }

        var superAdmin = new TestCurrentUser { Roles = [RoleNames.SuperAdmin], CompanyId = null };
        await using var queryContext = new ApplicationDbContext(options, superAdmin);
        (await queryContext.Customers.CountAsync()).Should().Be(2);
    }
}
