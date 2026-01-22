namespace EmailTracker.Models
{
    public sealed class ApiResult<T>
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string? Message { get; init; }
        public T? Data { get; init; }
        public IEnumerable<ApiError>? Errors { get; init; }
    }

    public sealed class ApiError
    {
        public string Code { get; init; } = default!;
        public string Message { get; init; } = default!;
    }
}
