namespace ToplandERP.Application.Companies;

public interface ICompanyService
{
    Task<IReadOnlyList<CompanyDto>> GetAccessibleCompaniesAsync(CancellationToken cancellationToken = default);

    Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
