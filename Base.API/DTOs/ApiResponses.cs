namespace Base.API.DTOs;

public sealed record ApiError(string Code, string Message, string Type);

public sealed record ApiResponse<T>(
    bool Success,
    string? Message,
    T? Data,
    IReadOnlyList<ApiError> Errors
)
{
    public static ApiResponse<T> Ok(T? data, string? message = null)
        => new(true, message, data, Array.Empty<ApiError>());

    public static ApiResponse<T> Fail(string message, params ApiError[] errors)
        => new(false, message, default, errors);
}

public sealed record ApiListResponse<T>(
    bool Success,
    string? Message,
    IReadOnlyList<T> Data,
    PaginationMeta? Meta,
    IReadOnlyList<ApiError> Errors
)
{
    public static ApiListResponse<T> Ok(IReadOnlyList<T> data, PaginationMeta? meta = null, string? message = null)
        => new(true, message, data, meta, Array.Empty<ApiError>());

    public static ApiListResponse<T> Fail(string message, params ApiError[] errors)
        => new(false, message, Array.Empty<T>(), null, errors);
}

public sealed record PaginationMeta(int Total, int Page, int PageSize);

