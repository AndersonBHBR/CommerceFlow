namespace Inventory.Application.Common;

public sealed record InventoryError(InventoryErrorCode Code, string Message);

public enum InventoryErrorCode
{
    Validation,
    NotFound,
    DuplicateSku,
    Conflict,
    ConcurrencyConflict
}

public sealed record InventoryResult<T>(T? Value, InventoryError? Error)
{
    public bool IsSuccess => Error is null;
}

public static class InventoryResult
{
    public static InventoryResult<T> Success<T>(T value) => new(value, null);

    public static InventoryResult<T> Failure<T>(InventoryErrorCode code, string message) =>
        new(default, new InventoryError(code, message));
}

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}
