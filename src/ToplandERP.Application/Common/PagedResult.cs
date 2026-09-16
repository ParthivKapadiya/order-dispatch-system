namespace ToplandERP.Application.Common;

public sealed class PagedQuery
{
    public string? Search { get; set; }

    public bool? IsActive { get; set; }

    public Guid? CompanyId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;
}

public interface IPagedResult
{
    int TotalCount { get; }
    int Page { get; }
    int PageSize { get; }
    int TotalPages { get; }
    bool HasPrevious { get; }
    bool HasNext { get; }
}

public sealed class PagedResult<T> : IPagedResult
{
    public required IReadOnlyList<T> Items { get; init; }

    public int TotalCount { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
