using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Companies;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class CompanyServiceTests
{
    [Fact]
    public async Task SuperAdmin_can_see_all_companies()
    {
        await using var dbContext = CreateContext(SystemCurrentUser.Instance);
        await SeedCompaniesAsync(dbContext);

        var service = new CompanyService(
            dbContext,
            new TestCurrentUser { Roles = [RoleNames.SuperAdmin], CompanyId = null });

        var companies = await service.GetAccessibleCompaniesAsync();

        companies.Select(company => company.Code).Should().BeEquivalentTo(
            CompanyCodes.Gravis,
            CompanyCodes.Jeeko,
            CompanyCodes.Shree);
    }

    [Fact]
    public async Task Company_user_only_sees_own_company()
    {
        await using var dbContext = CreateContext(SystemCurrentUser.Instance);
        await SeedCompaniesAsync(dbContext);

        var service = new CompanyService(
            dbContext,
            new TestCurrentUser { CompanyId = SeedIdentifiers.JeekoCompanyId, Roles = [RoleNames.SalesEmployee] });

        var companies = await service.GetAccessibleCompaniesAsync();

        companies.Should().ContainSingle(company => company.Code == CompanyCodes.Jeeko);
    }

    [Fact]
    public async Task Company_user_cannot_load_another_company_by_id()
    {
        await using var dbContext = CreateContext(SystemCurrentUser.Instance);
        await SeedCompaniesAsync(dbContext);

        var service = new CompanyService(
            dbContext,
            new TestCurrentUser { CompanyId = SeedIdentifiers.ShreeCompanyId, Roles = [RoleNames.CompanyAdmin] });

        var otherCompany = await service.GetByIdAsync(SeedIdentifiers.GravisCompanyId);

        otherCompany.Should().BeNull();
    }

    private static ApplicationDbContext CreateContext(ICurrentUser? currentUser = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options, currentUser ?? SystemCurrentUser.Instance);
    }

    private static async Task SeedCompaniesAsync(ApplicationDbContext dbContext)
    {
        dbContext.Companies.AddRange(
            new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis India Private Limited", Code = CompanyCodes.Gravis, IsActive = true },
            new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko Agritech LLP", Code = CompanyCodes.Jeeko, IsActive = true },
            new Company { Id = SeedIdentifiers.ShreeCompanyId, Name = "Shree Agency", Code = CompanyCodes.Shree, IsActive = true });

        await dbContext.SaveChangesAsync();
    }
}
