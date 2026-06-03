namespace TimeTracker.Contracts;

/// <summary>A page of results plus the total count for pager UIs.</summary>
public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int Total);
