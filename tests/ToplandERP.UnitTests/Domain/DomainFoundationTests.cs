using FluentAssertions;
using ToplandERP.Domain.Constants;
using ToplandERP.Domain.Entities;

namespace ToplandERP.UnitTests.Domain;

public class DomainFoundationTests
{
    [Fact]
    public void Company_codes_include_the_three_confirmed_companies()
    {
        CompanyCodes.All.Should().Equal("GRAVIS", "JEEKO", "SHREE");
    }

    [Fact]
    public void Role_names_include_the_four_confirmed_roles()
    {
        RoleNames.All.Should().Equal(
            RoleNames.SuperAdmin,
            RoleNames.CompanyAdmin,
            RoleNames.SalesEmployee,
            RoleNames.DispatchUser);
    }

    [Fact]
    public void Company_entity_can_be_constructed_with_required_fields()
    {
        var company = new Company
        {
            Id = SeedIdentifiers.GravisCompanyId,
            Name = "Gravis India Private Limited",
            Code = CompanyCodes.Gravis,
            IsActive = true
        };

        company.Code.Should().Be("GRAVIS");
        company.Name.Should().Contain("Gravis");
    }

    [Fact]
    public void Seed_company_identifiers_are_stable()
    {
        SeedIdentifiers.GravisCompanyId.Should().NotBe(Guid.Empty);
        SeedIdentifiers.JeekoCompanyId.Should().NotBe(SeedIdentifiers.ShreeCompanyId);
    }
}
