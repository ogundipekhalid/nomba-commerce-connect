namespace NombaCommerceConnect.Application.Common;

/// <summary>
/// Lightweight result wrapper so handlers can return expected failures (validation,
/// insufficient stock, Nomba API errors) as data instead of throwing for control flow.
/// Unexpected/infra failures should still throw and be handled by middleware.
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(bool isSuccess, T? value, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result<T> Success(T value) => new(true, value, null, null);

    public static Result<T> Failure(string error, string? errorCode = null) =>
        new(false, default, error, errorCode);
}

/// <summary>Non-generic variant for operations that don't return a value.</summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    private Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);

    public static Result Failure(string error, string? errorCode = null) =>
        new(false, error, errorCode);
}
