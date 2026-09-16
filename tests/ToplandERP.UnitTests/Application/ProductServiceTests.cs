using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Products;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;
using ToplandERP.Infrastructure.Data;
using ToplandERP.Infrastructure.Identity;
using ToplandERP.UnitTests.Support;

namespace ToplandERP.UnitTests.Application;

public class ProductServiceTests
{
    [Fact]
    public async Task Company_isolation_hides_other_company_products()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedAsync(databaseName);

        var currentUser = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.CompanyAdmin] };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = new ProductService(dbContext, currentUser, new TestAuditLogger(), new ProductWriteRequestValidator());

        var result = await service.GetPagedAsync(new PagedQuery());
        result.Items.Should().OnlyContain(item => item.CompanyId == SeedIdentifiers.GravisCompanyId);
        result.Items.Should().NotContain(item => item.ProductCode == "JEE-P1");
    }

    [Fact]
    public async Task Cross_company_product_access_is_denied()
    {
        var databaseName = Guid.NewGuid().ToString();
        Guid otherId = Guid.Empty;
        await SeedAsync(databaseName, product =>
        {
            if (product.ProductCode == "JEE-P1")
            {
                otherId = product.Id;
            }
        });

        var currentUser = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.SalesEmployee] };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = new ProductService(dbContext, currentUser, new TestAuditLogger(), new ProductWriteRequestValidator());

        (await service.GetByIdAsync(otherId)).Should().BeNull();
    }

    [Fact]
    public async Task Duplicate_product_code_in_same_company_is_rejected()
    {
        var databaseName = Guid.NewGuid().ToString();
        await SeedAsync(databaseName);
        var currentUser = new TestCurrentUser { CompanyId = SeedIdentifiers.GravisCompanyId, Roles = [RoleNames.CompanyAdmin] };
        await using var dbContext = CreateContext(databaseName, currentUser);
        var service = new ProductService(dbContext, currentUser, new TestAuditLogger(), new ProductWriteRequestValidator());

        var act = async () => await service.CreateAsync(new ProductWriteRequest
        {
            ProductCode = "GRA-P1",
            ProductName = "Duplicate"
        });

        await act.Should().ThrowAsync<BusinessException>().WithMessage("*code*");
    }

    private static ApplicationDbContext CreateContext(string databaseName, ICurrentUser currentUser)
    {
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options,
            currentUser);
    }

    private static async Task SeedAsync(string databaseName, Action<Product>? inspect = null)
    {
        await using var dbContext = CreateContext(databaseName, SystemCurrentUser.Instance);
        dbContext.Companies.AddRange(
            new Company { Id = SeedIdentifiers.GravisCompanyId, Name = "Gravis", Code = CompanyCodes.Gravis, IsActive = true },
            new Company { Id = SeedIdentifiers.JeekoCompanyId, Name = "Jeeko", Code = CompanyCodes.Jeeko, IsActive = true });
        var own = MasterDataFactory.Product(SeedIdentifiers.GravisCompanyId, "GRA-P1", "Gravis product");
        var other = MasterDataFactory.Product(SeedIdentifiers.JeekoCompanyId, "JEE-P1", "Jeeko product");
        inspect?.Invoke(own);
        inspect?.Invoke(other);
        dbContext.Products.AddRange(own, other);
        await dbContext.SaveChangesAsync();
    }
}
