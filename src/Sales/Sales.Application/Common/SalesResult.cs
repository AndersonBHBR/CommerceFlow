namespace Sales.Application.Common;

public sealed record SalesError(SalesErrorCode Code, string Message);

public enum SalesErrorCode
{
    Validation,
    NotFound,
    Conflict,
    DuplicateExternalReference,
    ConcurrencyConflict,
    InventoryUnavailable
}

public sealed record SalesResult<T>(T? Value, SalesError? Error)
{
    public bool IsSuccess => Error is null;
}

public static class SalesResult
{
    public static SalesResult<T> Success<T>(T value) => new(value, null);

    public static SalesResult<T> Failure<T>(SalesErrorCode code, string message) =>
        new(default, new SalesError(code, message));
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
