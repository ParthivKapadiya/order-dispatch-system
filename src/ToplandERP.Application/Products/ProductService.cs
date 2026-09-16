using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Security;
using ToplandERP.Domain.Entities;

namespace ToplandERP.Application.Products;

public sealed class ProductService : IProductService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly IValidator<ProductWriteRequest> _validator;

    public ProductService(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        IValidator<ProductWriteRequest> validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _validator = validator;
    }

    public async Task<PagedResult<ProductDto>> GetPagedAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);
        var dbQuery = CompanyScope.ApplyOptionalCompanyFilter(_dbContext.Products.AsNoTracking(), _currentUser, query.CompanyId);

        if (query.IsActive.HasValue)
        {
            dbQuery = dbQuery.Where(item => item.IsActive == query.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(item =>
                item.ProductCode.Contains(term)
                || item.ProductName.Contains(term)
                || (item.ModelNumber != null && item.ModelNumber.Contains(term))
                || (item.Category != null && item.Category.Contains(term)));
        }

        var total = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .OrderBy(item => item.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ProductDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.Name,
                CompanyCode = item.Company.Code,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Category = item.Category,
                ModelNumber = item.ModelNumber,
                Description = item.Description,
                Unit = item.Unit,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ProductDto
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                CompanyName = item.Company.Name,
                CompanyCode = item.Company.Code,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                Category = item.Category,
                ModelNumber = item.ModelNumber,
                Description = item.Description,
                Unit = item.Unit,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProductDto> CreateAsync(ProductWriteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var companyId = CompanyScope.ResolveWriteCompanyId(_currentUser, request.CompanyId);
        await EnsureCompanyExistsAsync(companyId, cancellationToken);
        await EnsureUniqueCodeAsync(companyId, request.ProductCode, null, cancellationToken);

        var entity = new Product { CompanyId = companyId, IsActive = true };
        Apply(entity, request);
        _dbContext.Products.Add(entity);
        _auditLogger.Record("ProductCreated", nameof(Product), entity.Id, companyId, entity.ProductCode);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ProductDto> UpdateAsync(Guid id, ProductWriteRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var entity = await _dbContext.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        await EnsureUniqueCodeAsync(entity.CompanyId, request.ProductCode, entity.Id, cancellationToken);
        Apply(entity, request);
        _auditLogger.Record("ProductUpdated", nameof(Product), entity.Id, entity.CompanyId, entity.ProductCode);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Products.FirstOrDefaultAsync(item => item.Id == id, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        entity.IsActive = isActive;
        _auditLogger.Record(isActive ? "ProductActivated" : "ProductDeactivated", nameof(Product), entity.Id, entity.CompanyId, entity.ProductCode);
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

    private async Task EnsureUniqueCodeAsync(Guid companyId, string productCode, Guid? excludeId, CancellationToken cancellationToken)
    {
        var code = productCode.Trim();
        var duplicate = await _dbContext.Products.AnyAsync(
            item => item.CompanyId == companyId
                && item.ProductCode == code
                && (!excludeId.HasValue || item.Id != excludeId.Value),
            cancellationToken);
        if (duplicate)
        {
            throw new BusinessException("A product with this code already exists in the company.");
        }
    }

    private static void Apply(Product entity, ProductWriteRequest request)
    {
        entity.ProductCode = request.ProductCode.Trim();
        entity.ProductName = request.ProductName.Trim();
        entity.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        entity.ModelNumber = string.IsNullOrWhiteSpace(request.ModelNumber) ? null : request.ModelNumber.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.Unit = string.IsNullOrWhiteSpace(request.Unit) ? null : request.Unit.Trim();
    }
}
