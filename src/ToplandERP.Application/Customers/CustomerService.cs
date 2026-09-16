using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Security;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Application.Customers;

public sealed class CustomerService : ICustomerService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IValidator<CustomerWriteRequest> _validator;

    public CustomerService(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        IValidator<CustomerWriteRequest> validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _validator = validator;
    }

    public async Task<PagedResult<CustomerDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);
        var dbQuery = CompanyScope.ApplyOptionalCompanyFilter(_dbContext.Customers.AsNoTracking(), _currentUser, query.CompanyId);

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(item => item.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(item =>
                item.CustomerName.Contains(term)
                || item.CustomerCode.Contains(term)
                || item.Mobile.Contains(term)
                || (item.Email != null && item.Email.Contains(term))
                || item.BillingCity.Contains(term)
                || item.DeliveryCity.Contains(term));
        }

        var total = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .OrderBy(item => item.CustomerName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new CustomerDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.Name,
                CompanyCode = item.Company.Code,
                CustomerCode = item.CustomerCode,
                CustomerName = item.CustomerName,
                Mobile = item.Mobile,
                Email = item.Email,
                BillingAddress = item.BillingAddress,
                BillingCity = item.BillingCity,
                BillingState = item.BillingState,
                BillingPincode = item.BillingPincode,
                DeliveryAddress = item.DeliveryAddress,
                DeliveryCity = item.DeliveryCity,
                DeliveryState = item.DeliveryState,
                DeliveryPincode = item.DeliveryPincode,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<CustomerDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new CustomerDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.Name,
                CompanyCode = item.Company.Code,
                CustomerCode = item.CustomerCode,
                CustomerName = item.CustomerName,
                Mobile = item.Mobile,
                Email = item.Email,
                BillingAddress = item.BillingAddress,
                BillingCity = item.BillingCity,
                BillingState = item.BillingState,
                BillingPincode = item.BillingPincode,
                DeliveryAddress = item.DeliveryAddress,
                DeliveryCity = item.DeliveryCity,
                DeliveryState = item.DeliveryState,
                DeliveryPincode = item.DeliveryPincode,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<CustomerDto> CreateAsync(CustomerWriteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var companyId = CompanyScope.ResolveWriteCompanyId(_currentUser, request.CompanyId);
        await EnsureCompanyExistsAsync(companyId, cancellationToken);
        await EnsureUniqueAsync(companyId, request, excludeId: null, cancellationToken);

        var entity = new Customer { CompanyId = companyId, IsActive = true };
        Apply(entity, request);
        _dbContext.Customers.Add(entity);
        _auditLogger.Record("CustomerCreated", nameof(Customer), entity.Id, companyId, entity.CustomerCode);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<CustomerDto> UpdateAsync(Guid id, CustomerWriteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = await _dbContext.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Customer was not found.");

        var companyId = entity.CompanyId;
        await EnsureUniqueAsync(companyId, request, entity.Id, cancellationToken);
        Apply(entity, request);
        _auditLogger.Record("CustomerUpdated", nameof(Customer), entity.Id, companyId, entity.CustomerCode);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Customers.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Customer was not found.");

        entity.IsActive = isActive;
        _auditLogger.Record(isActive ? "CustomerActivated" : "CustomerDeactivated", nameof(Customer), entity.Id, entity.CompanyId, entity.CustomerCode);
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

    private async Task EnsureUniqueAsync(Guid companyId, CustomerWriteRequest request, Guid? excludeId, CancellationToken cancellationToken)
    {
        var code = request.CustomerCode.Trim();
        var mobile = request.Mobile.Trim();
        var duplicateCode = await _dbContext.Customers.AnyAsync(
            item => item.CompanyId == companyId
                && item.CustomerCode == code
                && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);
        if (duplicateCode)
        {
            throw new BusinessException("A customer with this code already exists in the company.");
        }

        var duplicateMobile = await _dbContext.Customers.AnyAsync(
            item => item.CompanyId == companyId
                && item.Mobile == mobile
                && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);
        if (duplicateMobile)
        {
            throw new BusinessException("A customer with this mobile number already exists in the company.");
        }
    }

    private static void Apply(Customer entity, CustomerWriteRequest request)
    {
        entity.CustomerCode = request.CustomerCode.Trim();
        entity.CustomerName = request.CustomerName.Trim();
        entity.Mobile = request.Mobile.Trim();
        entity.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        entity.BillingAddress = request.BillingAddress.Trim();
        entity.BillingCity = request.BillingCity.Trim();
        entity.BillingState = request.BillingState.Trim();
        entity.BillingPincode = request.BillingPincode.Trim();
        entity.DeliveryAddress = request.DeliveryAddress.Trim();
        entity.DeliveryCity = request.DeliveryCity.Trim();
        entity.DeliveryState = request.DeliveryState.Trim();
        entity.DeliveryPincode = request.DeliveryPincode.Trim();
    }
}
