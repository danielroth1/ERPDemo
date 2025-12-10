namespace SalesManagement.Models.DTOs;

public class ApiResponse<T>
{
    public T Data { get; set; } = default!;
    public string? Message { get; set; }
    public bool Success { get; set; } = true;

    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Data = data,
            Message = message,
            Success = true
        };
    }

    public static ApiResponse<T> ErrorResponse(string message)
    {
        return new ApiResponse<T>
        {
            Message = message,
            Success = false
        };
    }
}

public class PaginatedResponse<T>
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
