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

namespace ToplandERP.Application.Dispatching;

public sealed class DispatchService : IDispatchService
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUser _currentUser;
    private readonly IAuditLogger _auditLogger;
    private readonly INotificationService _notificationService;
    private readonly IFileStorage _fileStorage;
    private readonly IValidator<CompleteDispatchRequest> _validator;

    public DispatchService(
        IApplicationDbContext dbContext,
        ICurrentUser currentUser,
        IAuditLogger auditLogger,
        INotificationService notificationService,
        IFileStorage fileStorage,
        IValidator<CompleteDispatchRequest> validator)
    {
        _dbContext = dbContext;
        _currentUser = currentUser;
        _auditLogger = auditLogger;
        _notificationService = notificationService;
        _fileStorage = fileStorage;
        _validator = validator;
    }

    public async Task<PagedResult<DispatchDto>> GetReadyToDispatchAsync(
        DispatchListQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureCanDispatch();
        var page = CompanyScope.NormalizePage(query.Page);
        var pageSize = CompanyScope.NormalizePageSize(query.PageSize);
        var dbQuery = CompanyScope.ApplyOptionalCompanyFilter(
            _dbContext.Orders.AsNoTracking().Where(order => order.Status == OrderStatus.ReadyToDispatch),
            _currentUser,
            query.CompanyId);

        var total = await dbQuery.CountAsync(cancellationToken);
        var rows = await dbQuery
            .OrderBy(order => order.OrderedAt)
            .ThenBy(order => order.OrderNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(order => new
            {
                order.Id,
                order.CompanyId,
                OrderNumber = order.OrderNumber ?? string.Empty,
                CompanyName = order.Company.Name,
                CompanyCode = order.Company.Code,
                CustomerName = order.CustomerName ?? string.Empty,
                order.OrderedAt,
                order.Status,
                TransporterName = order.Transporter != null ? order.Transporter.Name : string.Empty,
                order.BookingNumber,
                Items = order.Items.Select(item => new { item.ProductName, item.Quantity }).ToList()
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(order => new DispatchDto
        {
            Id = order.Id,
            OrderId = order.Id,
            CompanyId = order.CompanyId,
            OrderNumber = order.OrderNumber,
            CompanyName = order.CompanyName,
            CompanyCode = order.CompanyCode,
            CustomerName = order.CustomerName,
            OrderedAt = order.OrderedAt,
            OrderStatus = order.Status,
            StatusLabel = OrderStatusDisplay.Label(order.Status),
            ProductSummary = string.Join(", ", order.Items.OrderBy(item => item.ProductName).Select(item => item.ProductName + " × " + item.Quantity.ToString("0.###"))),
            ItemCount = order.Items.Count,
            TransporterName = order.TransporterName,
            BookingNumber = order.BookingNumber,
            IsCompleted = false
        }).ToList();

        return new PagedResult<DispatchDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<DispatchDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        EnsureCanDispatch();
        var order = await _dbContext.Orders
            .AsNoTracking()
            .Include(item => item.Items)
            .Include(item => item.Company)
            .Include(item => item.Transporter)
            .Include(item => item.Dispatches)
                .ThenInclude(dispatch => dispatch.Documents)
            .Include(item => item.Dispatches)
                .ThenInclude(dispatch => dispatch.Transporter)
            .FirstOrDefaultAsync(item => item.Id == orderId, cancellationToken);

        return order is null ? null : ToDto(order);
    }

    public async Task<DispatchDto> CompleteAsync(
        Guid orderId,
        CompleteDispatchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCanDispatch();
        await _validator.ValidateAndThrowAsync(request, cancellationToken);
        DispatchDto? result = null;
        var storedFiles = new List<StoredFile>();

        try
        {
            foreach (var file in request.Files)
            {
                try
                {
                    storedFiles.Add(await _fileStorage.SaveAsync(
                        file.Content,
                        file.OriginalFileName,
                        file.ContentType,
                        cancellationToken));
                }
                catch (InvalidOperationException exception)
                {
                    throw new BusinessException(exception.Message);
                }
            }

            await _dbContext.ExecuteInTransactionAsync(async token =>
            {
                var order = await _dbContext.Orders
                    .Include(item => item.Items)
                    .Include(item => item.Company)
                    .Include(item => item.Transporter)
                    .Include(item => item.Dispatches)
                        .ThenInclude(dispatch => dispatch.Documents)
                    .FirstOrDefaultAsync(item => item.Id == orderId, token)
                    ?? throw new NotFoundException("Order was not found.");

                if (order.Status == OrderStatus.DispatchDone || order.Dispatches.Any(item => item.IsCompleted))
                {
                    throw new BusinessException("This order has already been dispatched.");
                }

                if (order.Status != OrderStatus.ReadyToDispatch)
                {
                    throw new BusinessException("Only orders that are Ready to Dispatch can be dispatched.");
                }

                var pending = await _dbContext.OrderModificationRequests.AnyAsync(
                    item => item.OrderId == order.Id && item.Status == ModificationRequestStatus.Pending,
                    token);
                if (pending)
                {
                    throw new BusinessException("Resolve the pending modification request before dispatch.");
                }

                var transporter = await _dbContext.Transporters
                    .FirstOrDefaultAsync(item => item.Id == request.TransporterId, token)
                    ?? throw new BusinessException("The selected transporter was not found.");
                if (!transporter.IsActive || transporter.CompanyId != order.CompanyId)
                {
                    throw new BusinessException("Select an active transporter for this company.");
                }

                var hasLrReference = !string.IsNullOrWhiteSpace(request.LrNumber)
                    || !string.IsNullOrWhiteSpace(request.BookingNumber);
                var hasTransportDocument = request.Files.Any(file =>
                    file.DocumentType is DispatchDocumentType.TransportReceipt or DispatchDocumentType.LrDocument);
                if (!hasLrReference && !hasTransportDocument)
                {
                    throw new BusinessException("Enter an LR/booking number or upload a transport receipt / LR document.");
                }

                var dispatch = order.Dispatches.FirstOrDefault();
                if (dispatch is null)
                {
                    dispatch = new Dispatch
                    {
                        CompanyId = order.CompanyId,
                        OrderId = order.Id
                    };
                    order.Dispatches.Add(dispatch);
                    _dbContext.Dispatches.Add(dispatch);
                }

                dispatch.TransporterId = transporter.Id;
                dispatch.Transporter = transporter;
                dispatch.DispatchDate = ToUtcDate(request.DispatchDate);
                dispatch.DispatchedAt = DateTime.UtcNow;
                dispatch.DispatchedByUserId = _currentUser.UserId;
                dispatch.DispatchPersonName = request.DispatchPersonName.Trim();
                dispatch.LrNumber = OrderChangeMapper.TrimOrNull(request.LrNumber);
                dispatch.BookingNumber = OrderChangeMapper.TrimOrNull(request.BookingNumber);
                dispatch.Notes = OrderChangeMapper.TrimOrNull(request.Notes);
                dispatch.IsCompleted = true;

                for (var index = 0; index < storedFiles.Count; index++)
                {
                    var stored = storedFiles[index];
                    var upload = request.Files[index];
                    var document = new DispatchDocument
                    {
                        CompanyId = order.CompanyId,
                        FilePath = stored.StoredPath,
                        OriginalFileName = stored.OriginalFileName,
                        ContentType = stored.ContentType,
                        DocumentType = upload.DocumentType,
                        UploadedByUserId = _currentUser.UserId
                    };
                    dispatch.Documents.Add(document);
                    _auditLogger.Record(
                        "DispatchDocumentUploaded",
                        nameof(DispatchDocument),
                        document.Id,
                        order.CompanyId,
                        $"{upload.DocumentType}:{stored.OriginalFileName}");
                }

                order.Status = OrderStatus.DispatchDone;
                order.UpdatedAt = DateTime.UtcNow;

                _auditLogger.Record("DispatchCreated", nameof(Dispatch), dispatch.Id, order.CompanyId, order.OrderNumber);
                _auditLogger.Record("OrderMarkedDispatchDone", nameof(Order), order.Id, order.CompanyId, order.OrderNumber);
                _auditLogger.Record("OrderLocked", nameof(Order), order.Id, order.CompanyId, order.OrderNumber);
                await _notificationService.NotifyOrderDispatchedAsync(
                    order,
                    transporter.Name,
                    _currentUser.UserId,
                    token);
                await _dbContext.SaveChangesAsync(token);
                result = ToDto(order);
            }, cancellationToken);
        }
        catch
        {
            foreach (var stored in storedFiles)
            {
                await _fileStorage.DeleteAsync(stored.StoredPath, cancellationToken);
            }

            throw;
        }

        return result!;
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> OpenDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _dbContext.DispatchDocuments
            .AsNoTracking()
            .Include(item => item.Dispatch)
                .ThenInclude(dispatch => dispatch.Order)
            .FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken)
            ?? throw new NotFoundException("Document was not found.");

        if (OrderWorkflowRules.IsSalesEmployeeOnly(_currentUser.IsSuperAdmin, _currentUser.Roles)
            && document.Dispatch.Order.CreatedByUserId != _currentUser.UserId)
        {
            throw new NotFoundException("Document was not found.");
        }

        var stream = await _fileStorage.OpenReadAsync(document.FilePath, cancellationToken);
        return (stream, document.ContentType ?? "application/octet-stream", document.OriginalFileName);
    }

    private void EnsureCanDispatch()
    {
        if (!OrderWorkflowRules.CanDispatch(_currentUser.IsSuperAdmin, _currentUser.Roles))
        {
            throw new ForbiddenException("You do not have permission to perform this action.");
        }
    }

    private static DateTime? ToUtcDate(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc);
    }

    private static DispatchDto ToDto(Order order)
    {
        var dispatch = order.Dispatches.OrderByDescending(item => item.CreatedAt).FirstOrDefault();
        return new DispatchDto
        {
            Id = dispatch?.Id ?? order.Id,
            OrderId = order.Id,
            CompanyId = order.CompanyId,
            OrderNumber = order.OrderNumber ?? string.Empty,
            CompanyName = order.Company.Name,
            CompanyCode = order.Company.Code,
            CustomerName = order.CustomerName ?? string.Empty,
            OrderedAt = order.OrderedAt,
            OrderStatus = order.Status,
            StatusLabel = OrderStatusDisplay.Label(order.Status),
            ProductSummary = string.Join(
                ", ",
                order.Items.OrderBy(item => item.ProductName).Select(item => $"{item.ProductName} × {item.Quantity:0.###}")),
            ItemCount = order.Items.Count,
            DispatchDate = dispatch?.DispatchDate,
            DispatchedAt = dispatch?.DispatchedAt,
            DispatchPersonName = dispatch?.DispatchPersonName ?? string.Empty,
            TransporterId = dispatch?.TransporterId ?? order.TransporterId,
            TransporterName = dispatch?.Transporter?.Name ?? order.Transporter?.Name ?? string.Empty,
            LrNumber = dispatch?.LrNumber,
            BookingNumber = dispatch?.BookingNumber ?? order.BookingNumber,
            Notes = dispatch?.Notes,
            IsCompleted = dispatch?.IsCompleted == true,
            Documents = (dispatch?.Documents ?? [])
                .OrderBy(item => item.CreatedAt)
                .Select(item => new DispatchDocumentDto
                {
                    Id = item.Id,
                    DocumentType = item.DocumentType,
                    DocumentTypeLabel = DocumentTypeLabel(item.DocumentType),
                    OriginalFileName = item.OriginalFileName,
                    ContentType = item.ContentType
                })
                .ToList(),
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
                .ToList()
        };
    }

    private static string DocumentTypeLabel(DispatchDocumentType type) => type switch
    {
        DispatchDocumentType.MaterialPhoto => "Material photo",
        DispatchDocumentType.TransportReceipt => "Transport receipt",
        DispatchDocumentType.LrDocument => "LR document",
        _ => "Other"
    };
}
