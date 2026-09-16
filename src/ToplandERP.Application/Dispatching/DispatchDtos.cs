using ToplandERP.Application.Common;
using ToplandERP.Application.Orders;
using ToplandERP.Domain.Enums;

namespace ToplandERP.Application.Dispatching;

public sealed class DispatchListQuery
{
    public Guid? CompanyId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public sealed class DispatchFileUpload
{
    public required Stream Content { get; init; }

    public required string OriginalFileName { get; init; }

    public string ContentType { get; init; } = string.Empty;

    public DispatchDocumentType DocumentType { get; init; } = DispatchDocumentType.Other;
}

public sealed class CompleteDispatchRequest
{
    public DateTime? DispatchDate { get; set; }

    public string DispatchPersonName { get; set; } = string.Empty;

    public Guid? TransporterId { get; set; }

    public string? LrNumber { get; set; }

    public string? BookingNumber { get; set; }

    public string? Notes { get; set; }

    public List<DispatchFileUpload> Files { get; set; } = [];
}

public sealed class DispatchDocumentDto
{
    public Guid Id { get; init; }
    public DispatchDocumentType DocumentType { get; init; }
    public string DocumentTypeLabel { get; init; } = string.Empty;
    public string OriginalFileName { get; init; } = string.Empty;
    public string? ContentType { get; init; }
}

public sealed class DispatchDto
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Guid CompanyId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CompanyName { get; init; } = string.Empty;
    public string CompanyCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public DateTime? OrderedAt { get; init; }
    public OrderStatus OrderStatus { get; init; }
    public string StatusLabel { get; init; } = string.Empty;
    public string ProductSummary { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public DateTime? DispatchDate { get; init; }
    public DateTime? DispatchedAt { get; init; }
    public string DispatchPersonName { get; init; } = string.Empty;
    public Guid? TransporterId { get; init; }
    public string TransporterName { get; init; } = string.Empty;
    public string? LrNumber { get; init; }
    public string? BookingNumber { get; init; }
    public string? Notes { get; init; }
    public bool IsCompleted { get; init; }
    public IReadOnlyList<DispatchDocumentDto> Documents { get; init; } = [];
    public IReadOnlyList<OrderItemDto> Items { get; init; } = [];
}

public interface IDispatchService
{
    Task<PagedResult<DispatchDto>> GetReadyToDispatchAsync(DispatchListQuery query, CancellationToken cancellationToken = default);
    Task<DispatchDto?> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<DispatchDto> CompleteAsync(Guid orderId, CompleteDispatchRequest request, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType, string FileName)> OpenDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
}
