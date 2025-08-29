namespace DotNet9.Api.Common;

public sealed record ApiResponse<T>(bool Success, T? Data, string[]? Errors = null)
{
    public static ApiResponse<T> Ok(T data) => new(true, data, null);
    public static ApiResponse<T> Fail(params string[] errors) => new(false, default, errors);
}

public sealed record ApiError(string Code, string Message);
