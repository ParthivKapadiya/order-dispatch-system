using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Security;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Application.PaymentConditions;

public sealed class PaymentConditionService : IPaymentConditionService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IValidator<PaymentConditionWriteRequest> _validator;

    public PaymentConditionService(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        IValidator<PaymentConditionWriteRequest> validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _validator = validator;
    }

    public async Task<PagedResult<PaymentConditionDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);
        var dbQuery = CompanyScope.ApplyOptionalCompanyFilter(_dbContext.PaymentConditions.AsNoTracking(), _currentUser, query.CompanyId);

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(item => item.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(item =>
                item.Name.Contains(term)
                || (item.Description != null && item.Description.Contains(term)));
        }

        var total = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .OrderBy(item => item.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new PaymentConditionDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.Name,
                CompanyCode = item.Company.Code,
                Name = item.Name,
                Description = item.Description,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentConditionDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PaymentConditionDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PaymentConditions
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new PaymentConditionDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.Name,
                CompanyCode = item.Company.Code,
                Name = item.Name,
                Description = item.Description,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PaymentConditionDto> CreateAsync(PaymentConditionWriteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var companyId = CompanyScope.ResolveWriteCompanyId(_currentUser, request.CompanyId);
        await EnsureCompanyExistsAsync(companyId, cancellationToken);
        await EnsureUniqueNameAsync(companyId, request.Name, null, cancellationToken);

        var entity = new PaymentCondition { CompanyId = companyId, IsActive = true };
        Apply(entity, request);
        _dbContext.PaymentConditions.Add(entity);
        _auditLogger.Record("PaymentConditionCreated", nameof(PaymentCondition), entity.Id, companyId, entity.Name);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<PaymentConditionDto> UpdateAsync(Guid id, PaymentConditionWriteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = await _dbContext.PaymentConditions.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Payment condition was not found.");

        await EnsureUniqueNameAsync(entity.CompanyId, request.Name, entity.Id, cancellationToken);
        Apply(entity, request);
        _auditLogger.Record("PaymentConditionUpdated", nameof(PaymentCondition), entity.Id, entity.CompanyId, entity.Name);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.PaymentConditions.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Payment condition was not found.");

        entity.IsActive = isActive;
        _auditLogger.Record(isActive ? "PaymentConditionActivated" : "PaymentConditionDeactivated", nameof(PaymentCondition), entity.Id, entity.CompanyId, entity.Name);
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
        var duplicate = await _dbContext.PaymentConditions.AnyAsync(
            item => item.CompanyId == companyId
                && item.Name == trimmed
                && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);
        if (duplicate)
        {
            throw new BusinessException("A payment condition with this name already exists in the company.");
        }
    }

    private static void Apply(PaymentCondition entity, PaymentConditionWriteRequest request)
    {
        entity.Name = request.Name.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
    }
}
