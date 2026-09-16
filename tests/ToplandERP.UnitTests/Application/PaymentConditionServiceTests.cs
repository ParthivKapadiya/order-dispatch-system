using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.PaymentConditions;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class PaymentConditionServiceTests
{
    [Fact]
    public async Task Company_isolation_works_for_payment_conditions()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(databaseName, SystemCurrentUser.Instance))
        {
            seed.Companies.AddRange(
                new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
                new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true });
            seed.PaymentConditions.AddRange(
                MasterDataFactory.PaymentCondition(SeedIdentifiers.GravisCompanyId, "Cash"),
                MasterDataFactory.PaymentCondition(SeedIdentifiers.JeekoCompanyId, "Credit"));
            await seed.SaveChangesAsync();
        }

        var currentUser = new TestCurrentUser { CompanyId = SeedIdentifiers.ShreeCompanyId, Roles = [RoleNames.SalesEmployee] };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = new PaymentConditionService(dbContext, currentUser, new TestAuditLogger(), new PaymentConditionWriteRequestValidator());

        (await service.GetPagedAsync(new PagedQuery())).Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Duplicate_name_in_same_company_is_rejected()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(databaseName, SystemCurrentUser.Instance))
        {
            seed.Companies.Add(new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true });
            seed.PaymentConditions.Add(MasterDataFactory.PaymentCondition(SeedIdentifiers.GravisCompanyId, "Cash"));
            await seed.SaveChangesAsync();
        }

        var currentUser = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.CompanyAdmin] };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = new PaymentConditionService(dbContext, currentUser, new TestAuditLogger(), new PaymentConditionWriteRequestValidator());

        var act = async () => await service.CreateAsync(new PaymentConditionWriteRequest { Name = "Cash" });
        await act.Should().ThrowAsync<BusinessException>().WithMessage("*name*");
    }

    private static ApplicationDbContext CreateContext(string databaseName, ICurrentUser currentUser)
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options,
            currentUser);
    }
}
