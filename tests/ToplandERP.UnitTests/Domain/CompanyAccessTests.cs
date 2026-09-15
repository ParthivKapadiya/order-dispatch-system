using FluentAssertions;
using ToplandERP.Domain.Common;

namespace ToplandERP.UnitTests.Domain;

public class CompanyAccessTests
{
    [Fact]
    public void SuperAdmin_can_access_any_company()
    {
        var companyId = Guid.NewGuid();

        CompanyAccess.CanAccessCompany(true, null, companyId).Should().BeTrue();
        CompanyAccess.CanAccessCompany(true, Guid.NewGuid(), companyId).Should().BeTrue();
    }

    [Fact]
    public void Company_user_can_access_own_company_only()
    {
        var companyId = Guid.NewGuid();

        CompanyAccess.CanAccessCompany(false, companyId, companyId).Should().BeTrue();
        CompanyAccess.CanAccessCompany(false, companyId, Guid.NewGuid()).Should().BeFalse();
        CompanyAccess.CanAccessCompany(false, null, companyId).Should().BeFalse();
    }
}
