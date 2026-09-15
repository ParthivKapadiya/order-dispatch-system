using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Domain.Common;

namespace ToplandERP.Application.Companies;

public sealed class CompanyService : ICompanyService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;

    public CompanyService(IApplicationDbContext dbContext, ICurrentUser currentUser)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<CompanyDto>> GetAccessibleCompaniesAsync(CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Companies.AsNoTracking();

        if (!_currentUser.IsSuperAdmin)
        {
            if (!_currentUser.CompanyId.HasValue)
            {
                return [];
            }

            query = query.Where(company => company.Id == _currentUser.CompanyId.Value);
        }

        return await query
            .OrderBy(company => company.Name)
            .Select(company => new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Code = company.Code,
                Address = company.Address,
                Phone = company.Phone,
                Email = company.Email,
                IsActive = company.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<CompanyDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!CompanyAccess.CanAccessCompany(_currentUser.IsSuperAdmin, _currentUser.CompanyId, id))
        {
            return null;
        }

        return await _dbContext.Companies
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(company => new CompanyDto
            {
                Id = company.Id,
                Name = company.Name,
                Code = company.Code,
                Address = company.Address,
                Phone = company.Phone,
                Email = company.Email,
                IsActive = company.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
