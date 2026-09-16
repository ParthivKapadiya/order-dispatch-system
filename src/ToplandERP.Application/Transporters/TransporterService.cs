using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Security;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Application.Transporters;

public sealed class TransporterService : ITransporterService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IValidator<TransporterWriteRequest> _validator;

    public TransporterService(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        IValidator<TransporterWriteRequest> validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _validator = validator;
    }

    public async Task<PagedResult<TransporterDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);
        var dbQuery = CompanyScope.ApplyOptionalCompanyFilter(_dbContext.Transporters.AsNoTracking(), _currentUser, query.CompanyId);

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(item => item.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(item =>
                item.Name.Contains(term)
                || (item.ContactPerson != null && item.ContactPerson.Contains(term))
                || (item.Mobile != null && item.Mobile.Contains(term)));
        }

        var total = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .OrderBy(item => item.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new TransporterDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.Name,
                CompanyCode = item.Company.Code,
                Name = item.Name,
                ContactPerson = item.ContactPerson,
                Mobile = item.Mobile,
                Address = item.Address,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<TransporterDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<TransporterDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Transporters
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new TransporterDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.Name,
                CompanyCode = item.Company.Code,
                Name = item.Name,
                ContactPerson = item.ContactPerson,
                Mobile = item.Mobile,
                Address = item.Address,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TransporterDto> CreateAsync(TransporterWriteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var companyId = CompanyScope.ResolveWriteCompanyId(_currentUser, request.CompanyId);
        await EnsureCompanyExistsAsync(companyId, cancellationToken);
        await EnsureUniqueNameAsync(companyId, request.Name, null, cancellationToken);

        var entity = new Transporter { CompanyId = companyId, IsActive = true };
        Apply(entity, request);
        _dbContext.Transporters.Add(entity);
        _auditLogger.Record("TransporterCreated", nameof(Transporter), entity.Id, companyId, entity.Name);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<TransporterDto> UpdateAsync(Guid id, TransporterWriteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = await _dbContext.Transporters.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Transporter was not found.");

        await EnsureUniqueNameAsync(entity.CompanyId, request.Name, entity.Id, cancellationToken);
        Apply(entity, request);
        _auditLogger.Record("TransporterUpdated", nameof(Transporter), entity.Id, entity.CompanyId, entity.Name);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Transporters.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Transporter was not found.");

        entity.IsActive = isActive;
        _auditLogger.Record(isActive ? "TransporterActivated" : "TransporterDeactivated", nameof(Transporter), entity.Id, entity.CompanyId, entity.Name);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureCompanyExistsAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Companies.AnyAsync(company => company.Id == companyId && company.IsActive, cancellationToken);
        if (!exists)
        {
            throw new BusinessException("The selected company is not available.");
        }
    }

    private async Task EnsureUniqueNameAsync(Guid companyId, string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var trimmed = name.Trim();
        var duplicate = await _dbContext.Transporters.AnyAsync(
            item => item.CompanyId == companyId
                && item.Name == trimmed
                && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);
        if (duplicate)
        {
            throw new BusinessException("A transporter with this name already exists in the company.");
        }
    }

    private static void Apply(Transporter entity, TransporterWriteRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.ContactPerson = string.IsNullOrWhiteSpace(request.ContactPerson) ? null : request.ContactPerson.Trim();
        entity.Mobile = string.IsNullOrWhiteSpace(request.Mobile) ? null : request.Mobile.Trim();
        entity.Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim();
    }
}
