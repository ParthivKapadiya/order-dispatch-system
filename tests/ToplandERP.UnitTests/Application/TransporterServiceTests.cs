using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Transporters;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class TransporterServiceTests
{
    [Fact]
    public async Task Company_isolation_works_for_transporters()
    {
        var databaseName = Guid.NewGuid().ToString();
        await using (var seed = CreateContext(databaseName, SystemCurrentUser.Instance))
        {
            seed.Companies.AddRange(
                new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
                new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true });
            seed.Transporters.AddRange(
                MasterDataFactory.Transporter(SeedIdentifiers.GravisCompanyId, "Mehta Transport"),
                MasterDataFactory.Transporter(SeedIdentifiers.JeekoCompanyId, "Haresh Transport"));
            await seed.SaveChangesAsync();
        }

        var currentUser = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.DispatchUser] };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = new TransporterService(dbContext, currentUser, new TestAuditLogger(), new TransporterWriteRequestValidator());

        var result = await service.GetPagedAsync(new PagedQuery());
        result.Items.Should().ContainSingle(item => item.Name == "Mehta Transport");
        result.Items.Should().NotContain(item => item.Name == "Haresh Transport");
    }

    private static ApplicationDbContext CreateContext(string databaseName, ICurrentUser currentUser)
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options,
            currentUser);
    }
}
