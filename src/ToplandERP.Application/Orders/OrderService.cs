using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Notifications;
using ToplandERP.Application.Security;
using ToplandERP.Domain.Entities;
using ToplandERP.Domain.Enums;
using ToplandERP.Domain.Security;

namespace ToplandERP.Application.Orders;

public sealed class OrderService : IOrderService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateOrderRequest> _validator;

    public OrderService(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        INotificationService notificationService,
        IValidator<CreateOrderRequest> validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _notificationService = notificationService;
        _validator = validator;
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(OrderListQuery query, CancellationToken cancellationToken = default)
    {
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);
        var dbQuery = CompanyScope.ApplyOptionalCompanyFilter(_dbContext.Orders.AsNoTracking(), _currentUser, query.CompanyId);
        dbQuery = RestrictToOwnOrders(dbQuery);

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(order => order.Status == query.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dbQuery = dbQuery.Where(order =>
                (order.OrderNumber != null && order.OrderNumber.Contains(term))
                || (order.CustomerName != null && order.CustomerName.Contains(term))
                || (order.CustomerCode != null && order.CustomerCode.Contains(term))
                || (order.CustomerMobile != null && order.CustomerMobile.Contains(term)));
        }

        var total = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .OrderByDescending(order => order.OrderedAt)
            .ThenByDescending(order => order.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new OrderDto
            {
                Id = order.Id,
                CompanyId = order.CompanyId,
                CompanyName = order.Company.Name,
                CompanyCode = order.Company.Code,
                OrderNumber = order.OrderNumber ?? string.Empty,
                Status = order.Status,
                StatusLabel = OrderStatusDisplay.Label(order.Status),
                OrderedAt = order.OrderedAt,
                CustomerId = order.CustomerId,
                CustomerCode = order.CustomerCode ?? string.Empty,
                CustomerName = order.CustomerName ?? string.Empty,
                CustomerMobile = order.CustomerMobile ?? string.Empty,
                BillAmount = order.BillAmount,
                PaymentConditionName = order.PaymentCondition != null ? order.PaymentCondition.Name : string.Empty,
                TransporterName = order.Transporter != null ? order.Transporter.Name : string.Empty
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.Company)
            .Include(item => item.PaymentCondition)
            .Include(item => item.Transporter)
            .Include(item => item.ModificationRequests)
            .Include(item => item.Dispatches)
                .ThenInclude(dispatch => dispatch.Documents)
            .Include(item => item.Dispatches)
                .ThenInclude(dispatch => dispatch.Transporter)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (order is null || !CanViewOrder(order))
        {
            return null;
        }

        return ToDto(order);
    }

    public async Task<OrderCreateCatalog> GetCreateCatalogAsync(Guid? companyId, CancellationToken cancellationToken = default)
    {
        var filterCompanyId = _currentUser.IsSuperAdmin ? companyId : _currentUser.CompanyId;
        if (_currentUser.IsSuperAdmin && (!filterCompanyId.HasValue || filterCompanyId.Value == Guid.Empty))
        {
            return new OrderCreateCatalog();
        }

        var customers = _dbContext.Customers.AsNoTracking().Where(item => item.IsActive);
        var products = _dbContext.Products.AsNoTracking().Where(item => item.IsActive);
        var payments = _dbContext.PaymentConditions.AsNoTracking().Where(item => item.IsActive);
        var transporters = _dbContext.Transporters.AsNoTracking().Where(item => item.IsActive);

        if (filterCompanyId.HasValue && filterCompanyId.Value != Guid.Empty)
        {
            customers = customers.Where(item => item.CompanyId == filterCompanyId.Value);
            products = products.Where(item => item.CompanyId == filterCompanyId.Value);
            payments = payments.Where(item => item.CompanyId == filterCompanyId.Value);
            transporters = transporters.Where(item => item.CompanyId == filterCompanyId.Value);
        }

        return new OrderCreateCatalog
        {
            Customers = await customers.OrderBy(item => item.CustomerName).Select(item => new OrderCustomerLookup
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                Name = item.CustomerName,
                Code = item.CustomerCode,
                Extra = item.Mobile,
                Mobile = item.Mobile,
                BillingAddress = item.BillingAddress,
                BillingCity = item.BillingCity,
                BillingState = item.BillingState,
                BillingPincode = item.BillingPincode,
                DeliveryAddress = item.DeliveryAddress,
                DeliveryCity = item.DeliveryCity,
                DeliveryState = item.DeliveryState,
                DeliveryPincode = item.DeliveryPincode
            }).ToListAsync(cancellationToken),
            Products = await products.OrderBy(item => item.ProductName).Select(item => new OrderLookupItem
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                Name = item.ProductName,
                Code = item.ProductCode,
                Extra = item.ModelNumber
            }).ToListAsync(cancellationToken),
            PaymentConditions = await payments.OrderBy(item => item.Name).Select(item => new OrderLookupItem
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                Name = item.Name
            }).ToListAsync(cancellationToken),
            Transporters = await transporters.OrderBy(item => item.Name).Select(item => new OrderLookupItem
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                Name = item.Name
            }).ToListAsync(cancellationToken)
        };
    }

    public async Task<OrderCustomerLookup?> GetCustomerLookupAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Customers
            .AsNoTracking()
            .Where(item => item.Id == customerId && item.IsActive)
            .Select(item => new OrderCustomerLookup
            {
                Id = item.Id,
                CompanyId = item.CompanyId,
                Name = item.CustomerName,
                Code = item.CustomerCode,
                Extra = item.Mobile,
                Mobile = item.Mobile,
                BillingAddress = item.BillingAddress,
                BillingCity = item.BillingCity,
                BillingState = item.BillingState,
                BillingPincode = item.BillingPincode,
                DeliveryAddress = item.DeliveryAddress,
                DeliveryCity = item.DeliveryCity,
                DeliveryState = item.DeliveryState,
                DeliveryPincode = item.DeliveryPincode
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<OrderDto> MarkReadyToDispatchAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (!OrderWorkflowRules.CanMarkReadyToDispatch(_currentUser.IsSuperAdmin, _currentUser.Roles))
        {
            throw new ForbiddenException("You do not have permission to perform this action.");
        }

        OrderDto? result = null;
        await _dbContext.ExecuteInTransactionAsync(async token =>
        {
            var order = await _dbContext.Orders
                .Include(item => item.Items)
                .Include(item => item.Company)
                .Include(item => item.PaymentCondition)
                .Include(item => item.Transporter)
                .Include(item => item.ModificationRequests)
                .Include(item => item.Dispatches)
                    .ThenInclude(dispatch => dispatch.Documents)
                .Include(item => item.Dispatches)
                    .ThenInclude(dispatch => dispatch.Transporter)
                .FirstOrDefaultAsync(item => item.Id == id, token)
                ?? throw new NotFoundException("Order was not found.");

            if (order.Status != OrderStatus.Received)
            {
                throw new BusinessException("Only orders with status Order Received can be marked Ready to Dispatch.");
            }

            var pending = order.ModificationRequests.Any(item => item.Status == ModificationRequestStatus.Pending);
            if (pending)
            {
                throw new BusinessException("Resolve the pending modification request before marking Ready to Dispatch.");
            }

            order.Status = OrderStatus.ReadyToDispatch;
            order.UpdatedAt = DateTime.UtcNow;
            _auditLogger.Record("OrderMarkedReadyToDispatch", nameof(Order), order.Id, order.CompanyId, order.OrderNumber);
            await _notificationService.NotifyOrderReadyToDispatchAsync(order, token);
            await _dbContext.SaveChangesAsync(token);
            result = ToDto(order);
        }, cancellationToken);

        return result!;
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        var companyId = CompanyScope.ResolveWriteCompanyId(_currentUser, request.CompanyId);

        var customer = await _dbContext.Customers
            .FirstOrDefaultAsync(item => item.Id == request.CustomerId, cancellationToken)
            ?? throw new BusinessException("The selected customer was not found.");
        if (!customer.IsActive)
        {
            throw new BusinessException("The selected customer is inactive.");
        }

        if (customer.CompanyId != companyId)
        {
            throw new ForbiddenException("The selected customer does not belong to this company.");
        }

        var payment = await _dbContext.PaymentConditions
            .FirstOrDefaultAsync(item => item.Id == request.PaymentConditionId, cancellationToken)
            ?? throw new BusinessException("The selected payment condition was not found.");
        if (!payment.IsActive || payment.CompanyId != companyId)
        {
            throw new BusinessException("Select an active payment condition for this company.");
        }

        var transporter = await _dbContext.Transporters
            .FirstOrDefaultAsync(item => item.Id == request.TransporterId, cancellationToken)
            ?? throw new BusinessException("The selected transporter was not found.");
        if (!transporter.IsActive || transporter.CompanyId != companyId)
        {
            throw new BusinessException("Select an active transporter for this company.");
        }

        var lines = request.Items
            .Where(item => item.ProductId.HasValue && item.ProductId.Value != Guid.Empty && item.Quantity > 0)
            .ToList();

        var productIds = lines.Select(item => item.ProductId!.Value).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(item => productIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count)
        {
            throw new BusinessException("One or more selected products were not found.");
        }

        if (products.Any(item => !item.IsActive || item.CompanyId != companyId))
        {
            throw new BusinessException("All products must be active and belong to the same company.");
        }

        var company = await _dbContext.Companies.FirstAsync(item => item.Id == companyId, cancellationToken);
        var orderNumber = await NextOrderNumberAsync(company.Code, companyId, cancellationToken);
        var productMap = products.ToDictionary(item => item.Id);

        var order = new Order
        {
            CompanyId = companyId,
            CustomerId = customer.Id,
            PaymentConditionId = payment.Id,
            TransporterId = transporter.Id,
            OrderNumber = orderNumber,
            Status = OrderStatus.Received,
            OrderedAt = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            CustomerMobile = customer.Mobile,
            BillingAddress = request.BillingAddress.Trim(),
            BillingCity = request.BillingCity.Trim(),
            BillingState = request.BillingState.Trim(),
            BillingPincode = request.BillingPincode.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            DeliveryCity = request.DeliveryCity.Trim(),
            DeliveryState = request.DeliveryState.Trim(),
            DeliveryPincode = request.DeliveryPincode.Trim(),
            BillAmount = request.BillAmount,
            BookingNumber = TrimOrNull(request.BookingNumber),
            BookingDate = request.BookingDate,
            BookingFrom = TrimOrNull(request.BookingFrom),
            BookingTo = TrimOrNull(request.BookingTo),
            BookingDetails = TrimOrNull(request.BookingDetails),
            Remarks = TrimOrNull(request.Remarks),
            SpecialInstructions = TrimOrNull(request.SpecialInstructions)
        };

        foreach (var line in lines)
        {
            var product = productMap[line.ProductId!.Value];
            order.Items.Add(new OrderItem
            {
                CompanyId = companyId,
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                ModelNumber = product.ModelNumber,
                Unit = product.Unit,
                Quantity = line.Quantity
            });
        }

        if (order.Id == Guid.Empty)
        {
            order.Id = Guid.NewGuid();
        }

        await _dbContext.ExecuteInTransactionAsync(async token =>
        {
            _dbContext.Orders.Add(order);
            _auditLogger.Record("OrderCreated", nameof(Order), order.Id, companyId, orderNumber);
            await _notificationService.NotifyOrderCreatedAsync(order, token);
            await _dbContext.SaveChangesAsync(token);
        }, cancellationToken);

        return (await GetByIdAsync(order.Id, cancellationToken))!;
    }

    private async Task<string> NextOrderNumberAsync(string companyCode, Guid companyId, CancellationToken cancellationToken)
    {
        var prefix = $"{companyCode}-{DateTime.UtcNow:yyyyMMdd}-";
        var last = await _dbContext.Orders
            .Where(order => order.CompanyId == companyId && order.OrderNumber != null && order.OrderNumber.StartsWith(prefix))
            .Select(order => order.OrderNumber!)
            .OrderByDescending(number => number)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (!string.IsNullOrWhiteSpace(last) && last.Length >= prefix.Length + 4
            && int.TryParse(last[prefix.Length..], out var parsed))
        {
            next = parsed + 1;
        }

        return $"{prefix}{next:0000}";
    }

    private static string? TrimOrNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static OrderDto ToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CompanyId = order.CompanyId,
            CompanyName = order.Company.Name,
            CompanyCode = order.Company.Code,
            OrderNumber = order.OrderNumber ?? string.Empty,
            Status = order.Status,
            StatusLabel = OrderStatusDisplay.Label(order.Status),
            OrderedAt = order.OrderedAt,
            CustomerId = order.CustomerId,
            CustomerCode = order.CustomerCode ?? string.Empty,
            CustomerName = order.CustomerName ?? string.Empty,
            CustomerMobile = order.CustomerMobile ?? string.Empty,
            BillingAddress = order.BillingAddress,
            BillingCity = order.BillingCity,
            BillingState = order.BillingState,
            BillingPincode = order.BillingPincode,
            DeliveryAddress = order.DeliveryAddress,
            DeliveryCity = order.DeliveryCity,
            DeliveryState = order.DeliveryState,
            DeliveryPincode = order.DeliveryPincode,
            BillAmount = order.BillAmount,
            PaymentConditionId = order.PaymentConditionId,
            PaymentConditionName = order.PaymentCondition?.Name ?? string.Empty,
            TransporterId = order.TransporterId,
            TransporterName = order.Transporter?.Name ?? string.Empty,
            BookingNumber = order.BookingNumber,
            BookingDate = order.BookingDate,
            BookingFrom = order.BookingFrom,
            BookingTo = order.BookingTo,
            BookingDetails = order.BookingDetails,
            Remarks = order.Remarks,
            SpecialInstructions = order.SpecialInstructions,
            Items = order.Items
                .OrderBy(item => item.ProductName)
                .Select(item => new OrderItemDto
                {
                    Id = item.Id,
                    ProductId = item.ProductId,
                    ProductCode = item.ProductCode,
                    ProductName = item.ProductName,
                    ModelNumber = item.ModelNumber,
                    Unit = item.Unit,
                    Quantity = item.Quantity
                })
                .ToList(),
            CreatedByUserId = order.CreatedByUserId,
            IsLocked = OrderWorkflowRules.IsLocked(order.Status),
            HasPendingModification = order.ModificationRequests.Any(item => item.Status == ModificationRequestStatus.Pending),
            ModificationHistory = order.ModificationRequests
                .OrderByDescending(item => item.CreatedAt)
                .Select(item => new OrderModificationHistoryDto
                {
                    Id = item.Id,
                    RequestedByName = item.RequestedByName,
                    CreatedAt = item.CreatedAt,
                    Reason = item.Reason ?? string.Empty,
                    Status = item.Status,
                    StatusLabel = item.Status == ModificationRequestStatus.Pending
                        ? "Pending"
                        : item.Status == ModificationRequestStatus.Approved
                            ? "Approved"
                            : "Rejected",
                    ReviewedByName = item.ReviewedByName,
                    ReviewedAt = item.ReviewedAt,
                    RejectionReason = item.RejectionReason
                })
                .ToList(),
            Dispatch = order.Dispatches
                .OrderByDescending(item => item.CreatedAt)
                .Select(dispatch => new OrderDispatchInfoDto
                {
                    Id = dispatch.Id,
                    DispatchDate = dispatch.DispatchDate,
                    DispatchedAt = dispatch.DispatchedAt,
                    DispatchPersonName = dispatch.DispatchPersonName,
                    TransporterName = dispatch.Transporter?.Name ?? order.Transporter?.Name ?? string.Empty,
                    LrNumber = dispatch.LrNumber,
                    BookingNumber = dispatch.BookingNumber,
                    Notes = dispatch.Notes,
                    IsCompleted = dispatch.IsCompleted,
                    Documents = dispatch.Documents
                        .OrderBy(document => document.CreatedAt)
                        .Select(document => new OrderDispatchDocumentDto
                        {
                            Id = document.Id,
                            DocumentTypeLabel = document.DocumentType == DispatchDocumentType.MaterialPhoto
                                ? "Material photo"
                                : document.DocumentType == DispatchDocumentType.TransportReceipt
                                    ? "Transport receipt"
                                    : document.DocumentType == DispatchDocumentType.LrDocument
                                        ? "LR document"
                                        : "Other",
                            OriginalFileName = document.OriginalFileName
                        })
                        .ToList()
                })
                .FirstOrDefault()
        };
    }

    private IQueryable<Order> RestrictToOwnOrders(IQueryable<Order> query)
    {
        if (!OrderWorkflowRules.IsSalesEmployeeOnly(_currentUser.IsSuperAdmin, _currentUser.Roles))
        {
            return query;
        }

        return query.Where(order => order.CreatedByUserId == _currentUser.UserId);
    }

    private bool CanViewOrder(Order order)
    {
        if (!OrderWorkflowRules.IsSalesEmployeeOnly(_currentUser.IsSuperAdmin, _currentUser.Roles))
        {
            return true;
        }

        return order.CreatedByUserId == _currentUser.UserId;
    }
}
