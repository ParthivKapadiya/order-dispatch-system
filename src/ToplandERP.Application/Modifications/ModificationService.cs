using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ToplandERP.Application.Abstractions;
using ToplandERP.Application.Common;
using ToplandERP.Application.Notifications;
using ToplandERP.Application.Orders;
using ToplandERP.Application.Security;
using ToplandERP.Domain.Entities;
using ToplandERP.Domain.Enums;
using ToplandERP.Domain.Security;

namespace ToplandERP.Application.Modifications;

public sealed class ModificationService : IModificationService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly INotificationService _notificationService;
    private readonly IValidator<CreateModificationRequest> _createValidator;
    private readonly IValidator<RejectModificationRequest> _rejectValidator;

    public ModificationService(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        INotificationService notificationService,
        IValidator<CreateModificationRequest> createValidator,
        IValidator<RejectModificationRequest> rejectValidator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _notificationService = notificationService;
        _createValidator = createValidator;
        _rejectValidator = rejectValidator;
    }

    public async Task<PagedResult<ModificationRequestSummaryDto>> GetPagedAsync(
        ModificationListQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureCanReview();
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);
        var dbQuery = CompanyScope.ApplyOptionalCompanyFilter(
            _dbContext.OrderModificationRequests.AsNoTracking(),
            _currentUser,
            query.CompanyId);

        if (query.Status.HasValue)
        {
            dbQuery = dbQuery.Where(item => item.Status == query.Status.Value);
        }

        var total = await dbQuery.CountAsync(cancellationToken);
        var items = await dbQuery
            .OrderByDescending(item => item.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new ModificationRequestSummaryDto
            {
                Id = item.Id,
                OrderId = item.OrderId,
                OrderNumber = item.Order.OrderNumber ?? string.Empty,
                CompanyName = item.Order.Company.Name,
                CompanyCode = item.Order.Company.Code,
                CustomerName = item.Order.CustomerName ?? string.Empty,
                RequestedByName = item.RequestedByName,
                CreatedAt = item.CreatedAt,
                Reason = item.Reason,
                Status = item.Status,
                StatusLabel = StatusLabel(item.Status),
                ReviewedByName = item.ReviewedByName,
                ReviewedAt = item.ReviewedAt,
                RejectionReason = item.RejectionReason
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ModificationRequestSummaryDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ModificationRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.OrderModificationRequests
            .AsNoTracking()
            .Include(item => item.Order)
                .ThenInclude(order => order.Company)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (entity is null)
        {
            return null;
        }

        EnsureCanView(entity);
        return ToDto(entity);
    }

    public async Task<IReadOnlyList<ModificationRequestSummaryDto>> GetForOrderAsync(
        Guid orderId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.OrderModificationRequests
            .AsNoTracking()
            .Where(item => item.OrderId == orderId)
            .OrderByDescending(item => item.CreatedAt)
            .Select(item => new ModificationRequestSummaryDto
            {
                Id = item.Id,
                OrderId = item.OrderId,
                OrderNumber = item.Order.OrderNumber ?? string.Empty,
                CompanyName = item.Order.Company.Name,
                CompanyCode = item.Order.Company.Code,
                CustomerName = item.Order.CustomerName ?? string.Empty,
                RequestedByName = item.RequestedByName,
                CreatedAt = item.CreatedAt,
                Reason = item.Reason,
                Status = item.Status,
                StatusLabel = StatusLabel(item.Status),
                ReviewedByName = item.ReviewedByName,
                ReviewedAt = item.ReviewedAt,
                RejectionReason = item.RejectionReason
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ModificationRequestDto> RequestAsync(
        Guid orderId,
        CreateModificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!OrderWorkflowRules.CanRequestModification(_currentUser.IsSuperAdmin, _currentUser.Roles))
        {
            throw new ForbiddenException("You do not have permission to perform this action.");
        }

        await _createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await LoadOrderForUpdateAsync(orderId, cancellationToken);
        EnsureSalesOwnsOrder(order);

        if (OrderWorkflowRules.IsLocked(order.Status))
        {
            throw new BusinessException("This order has already been dispatched and is locked.");
        }

        if (!OrderWorkflowRules.CanModifyOrderStatus(order.Status))
        {
            throw new BusinessException("Order is no longer available for modification.");
        }

        var hasPending = await _dbContext.OrderModificationRequests
            .AnyAsync(
                item => item.OrderId == order.Id && item.Status == ModificationRequestStatus.Pending,
                cancellationToken);
        if (hasPending)
        {
            throw new BusinessException("This order already has a pending modification request.");
        }

        var requested = await BuildRequestedChangeSetAsync(order.CompanyId, request, cancellationToken);
        var entity = new OrderModificationRequest
        {
            CompanyId = order.CompanyId,
            OrderId = order.Id,
            RequestedByUserId = _currentUser.UserId ?? Guid.Empty,
            RequestedByName = string.IsNullOrWhiteSpace(_currentUser.FullName)
                ? _currentUser.UserName ?? "Sales employee"
                : _currentUser.FullName,
            Reason = request.Reason.Trim(),
            CurrentSnapshotJson = OrderChangeMapper.Serialize(OrderChangeMapper.FromOrder(order)),
            RequestedChangesJson = OrderChangeMapper.Serialize(requested),
            Status = ModificationRequestStatus.Pending
        };

        if (entity.Id == Guid.Empty)
        {
            entity.Id = Guid.NewGuid();
        }

        await _dbContext.ExecuteInTransactionAsync(async token =>
        {
            _dbContext.OrderModificationRequests.Add(entity);
            _auditLogger.Record("OrderModificationRequested", nameof(OrderModificationRequest), entity.Id, order.CompanyId, order.OrderNumber);
            await _notificationService.NotifyModificationRequestedAsync(entity, order, token);
            await _dbContext.SaveChangesAsync(token);
        }, cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ModificationRequestDto> ApproveAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureCanReview();
        ModificationRequestDto? result = null;

        await _dbContext.ExecuteInTransactionAsync(async token =>
        {
            var entity = await _dbContext.OrderModificationRequests
                .Include(item => item.Order)
                    .ThenInclude(order => order.Items)
                .Include(item => item.Order)
                    .ThenInclude(order => order.PaymentCondition)
                .Include(item => item.Order)
                    .ThenInclude(order => order.Transporter)
                .Include(item => item.Order)
                    .ThenInclude(order => order.Company)
                .FirstOrDefaultAsync(item => item.Id == id, token)
                ?? throw new NotFoundException("Modification request was not found.");

            if (entity.Status != ModificationRequestStatus.Pending)
            {
                throw new BusinessException("This modification request has already been processed.");
            }

            if (OrderWorkflowRules.IsLocked(entity.Order.Status))
            {
                throw new BusinessException("This order has already been dispatched and is locked.");
            }

            if (!OrderWorkflowRules.CanModifyOrderStatus(entity.Order.Status))
            {
                throw new BusinessException("Order is no longer available for modification.");
            }

            var requested = OrderChangeMapper.Deserialize(entity.RequestedChangesJson);
            await ApplyRequestedChangesAsync(entity.Order, requested, token);

            entity.Status = ModificationRequestStatus.Approved;
            entity.ReviewedByUserId = _currentUser.UserId;
            entity.ReviewedByName = ReviewerName();
            entity.ReviewedAt = DateTime.UtcNow;
            entity.Order.UpdatedAt = DateTime.UtcNow;

            _auditLogger.Record(
                "OrderModificationApproved",
                nameof(OrderModificationRequest),
                entity.Id,
                entity.CompanyId,
                $"{entity.Order.OrderNumber}: approved");
            await _notificationService.NotifyModificationApprovedAsync(entity, entity.Order, token);
            await _dbContext.SaveChangesAsync(token);
            result = ToDto(entity);
        }, cancellationToken);

        return result!;
    }

    public async Task<ModificationRequestDto> RejectAsync(
        Guid id,
        RejectModificationRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanReview();
        await _rejectValidator.ValidateAndThrowAsync(request, cancellationToken);
        ModificationRequestDto? result = null;

        await _dbContext.ExecuteInTransactionAsync(async token =>
        {
            var entity = await _dbContext.OrderModificationRequests
                .Include(item => item.Order)
                    .ThenInclude(order => order.Company)
                .FirstOrDefaultAsync(item => item.Id == id, token)
                ?? throw new NotFoundException("Modification request was not found.");

            if (entity.Status != ModificationRequestStatus.Pending)
            {
                throw new BusinessException("This modification request has already been processed.");
            }

            entity.Status = ModificationRequestStatus.Rejected;
            entity.ReviewedByUserId = _currentUser.UserId;
            entity.ReviewedByName = ReviewerName();
            entity.ReviewedAt = DateTime.UtcNow;
            entity.RejectionReason = request.RejectionReason.Trim();

            _auditLogger.Record(
                "OrderModificationRejected",
                nameof(OrderModificationRequest),
                entity.Id,
                entity.CompanyId,
                $"{entity.Order.OrderNumber}: {entity.RejectionReason}");
            await _notificationService.NotifyModificationRejectedAsync(entity, entity.Order, token);
            await _dbContext.SaveChangesAsync(token);
            result = ToDto(entity);
        }, cancellationToken);

        return result!;
    }

    private async Task<Order> LoadOrderForUpdateAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return await _dbContext.Orders
            .Include(order => order.Items)
            .Include(order => order.PaymentCondition)
            .Include(order => order.Transporter)
            .Include(order => order.Company)
            .FirstOrDefaultAsync(order => order.Id == orderId, cancellationToken)
            ?? throw new NotFoundException("Order was not found.");
    }

    private void EnsureSalesOwnsOrder(Order order)
    {
        if (order.CreatedByUserId != _currentUser.UserId)
        {
            throw new ForbiddenException("You can only request modification for your own orders.");
        }
    }

    private void EnsureCanReview()
    {
        if (!OrderWorkflowRules.CanReviewModification(_currentUser.IsSuperAdmin, _currentUser.Roles))
        {
            throw new ForbiddenException("You do not have permission to perform this action.");
        }
    }

    private void EnsureCanView(OrderModificationRequest entity)
    {
        var isRequester = entity.RequestedByUserId == _currentUser.UserId;
        if (isRequester)
        {
            return;
        }

        EnsureCanReview();
    }

    private async Task<OrderChangeSet> BuildRequestedChangeSetAsync(
        Guid companyId,
        CreateModificationRequest request,
        CancellationToken cancellationToken)
    {
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

        var productMap = products.ToDictionary(item => item.Id);
        return new OrderChangeSet
        {
            BillAmount = request.BillAmount,
            PaymentConditionId = payment.Id,
            PaymentConditionName = payment.Name,
            TransporterId = transporter.Id,
            TransporterName = transporter.Name,
            BillingAddress = request.BillingAddress.Trim(),
            BillingCity = request.BillingCity.Trim(),
            BillingState = request.BillingState.Trim(),
            BillingPincode = request.BillingPincode.Trim(),
            DeliveryAddress = request.DeliveryAddress.Trim(),
            DeliveryCity = request.DeliveryCity.Trim(),
            DeliveryState = request.DeliveryState.Trim(),
            DeliveryPincode = request.DeliveryPincode.Trim(),
            BookingNumber = OrderChangeMapper.TrimOrNull(request.BookingNumber),
            BookingDate = request.BookingDate,
            BookingFrom = OrderChangeMapper.TrimOrNull(request.BookingFrom),
            BookingTo = OrderChangeMapper.TrimOrNull(request.BookingTo),
            BookingDetails = OrderChangeMapper.TrimOrNull(request.BookingDetails),
            Remarks = OrderChangeMapper.TrimOrNull(request.Remarks),
            SpecialInstructions = OrderChangeMapper.TrimOrNull(request.SpecialInstructions),
            Items = lines.Select(line =>
            {
                var product = productMap[line.ProductId!.Value];
                return new OrderChangeLine
                {
                    ProductId = product.Id,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    ModelNumber = product.ModelNumber,
                    Unit = product.Unit,
                    Quantity = line.Quantity
                };
            }).ToList()
        };
    }

    private async Task ApplyRequestedChangesAsync(Order order, OrderChangeSet requested, CancellationToken cancellationToken)
    {
        var productIds = requested.Items.Select(item => item.ProductId).Distinct().ToList();
        var products = await _dbContext.Products
            .Where(item => productIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        if (products.Count != productIds.Count || products.Any(item => !item.IsActive || item.CompanyId != order.CompanyId))
        {
            throw new BusinessException("All products must be active and belong to the same company.");
        }

        var payment = await _dbContext.PaymentConditions
            .FirstOrDefaultAsync(item => item.Id == requested.PaymentConditionId, cancellationToken)
            ?? throw new BusinessException("The selected payment condition was not found.");
        if (!payment.IsActive || payment.CompanyId != order.CompanyId)
        {
            throw new BusinessException("Select an active payment condition for this company.");
        }

        var transporter = await _dbContext.Transporters
            .FirstOrDefaultAsync(item => item.Id == requested.TransporterId, cancellationToken)
            ?? throw new BusinessException("The selected transporter was not found.");
        if (!transporter.IsActive || transporter.CompanyId != order.CompanyId)
        {
            throw new BusinessException("Select an active transporter for this company.");
        }

        order.BillAmount = requested.BillAmount;
        order.PaymentConditionId = payment.Id;
        order.TransporterId = transporter.Id;
        order.BillingAddress = requested.BillingAddress;
        order.BillingCity = requested.BillingCity;
        order.BillingState = requested.BillingState;
        order.BillingPincode = requested.BillingPincode;
        order.DeliveryAddress = requested.DeliveryAddress;
        order.DeliveryCity = requested.DeliveryCity;
        order.DeliveryState = requested.DeliveryState;
        order.DeliveryPincode = requested.DeliveryPincode;
        order.BookingNumber = requested.BookingNumber;
        order.BookingDate = requested.BookingDate;
        order.BookingFrom = requested.BookingFrom;
        order.BookingTo = requested.BookingTo;
        order.BookingDetails = requested.BookingDetails;
        order.Remarks = requested.Remarks;
        order.SpecialInstructions = requested.SpecialInstructions;

        _dbContext.OrderItems.RemoveRange(order.Items);
        order.Items.Clear();
        var productMap = products.ToDictionary(item => item.Id);
        foreach (var line in requested.Items)
        {
            var product = productMap[line.ProductId];
            order.Items.Add(new OrderItem
            {
                CompanyId = order.CompanyId,
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                ModelNumber = product.ModelNumber,
                Unit = product.Unit,
                Quantity = line.Quantity
            });
        }
    }

    private string ReviewerName() =>
        string.IsNullOrWhiteSpace(_currentUser.FullName)
            ? _currentUser.UserName ?? "Reviewer"
            : _currentUser.FullName;

    private static ModificationRequestDto ToDto(OrderModificationRequest entity)
    {
        return new ModificationRequestDto
        {
            Id = entity.Id,
            OrderId = entity.OrderId,
            CompanyId = entity.CompanyId,
            OrderNumber = entity.Order.OrderNumber ?? string.Empty,
            CompanyName = entity.Order.Company.Name,
            CompanyCode = entity.Order.Company.Code,
            CustomerName = entity.Order.CustomerName ?? string.Empty,
            CustomerCode = entity.Order.CustomerCode ?? string.Empty,
            RequestedByUserId = entity.RequestedByUserId,
            RequestedByName = entity.RequestedByName,
            CreatedAt = entity.CreatedAt,
            Reason = entity.Reason ?? string.Empty,
            Status = entity.Status,
            StatusLabel = StatusLabel(entity.Status),
            ReviewedByName = entity.ReviewedByName,
            ReviewedAt = entity.ReviewedAt,
            RejectionReason = entity.RejectionReason,
            Current = OrderChangeMapper.Deserialize(entity.CurrentSnapshotJson),
            Requested = OrderChangeMapper.Deserialize(entity.RequestedChangesJson)
        };
    }

    private static string StatusLabel(ModificationRequestStatus status) => status switch
    {
        ModificationRequestStatus.Pending => "Pending",
        ModificationRequestStatus.Approved => "Approved",
        ModificationRequestStatus.Rejected => "Rejected",
        _ => status.ToString()
    };
}
