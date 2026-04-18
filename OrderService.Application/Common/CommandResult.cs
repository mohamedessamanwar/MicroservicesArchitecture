namespace OrderService.Application.Common;

public class CommandResult<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public IEnumerable<string>? Errors { get; set; }

    public static CommandResult<T> Ok(T data, string? message = null) => new()
    {
        Success = true,
        Data = data,
        Message = message
    };

    public static CommandResult<T> Fail(IEnumerable<string>? errors, string? message = null) => new()
    {
        Success = false,
        Errors = errors,
        Message = message
    };

    public static CommandResult<T> Fail(string error, string? message = null) => new()
    {
        Success = false,
        Errors = new[] { error },
        Message = message
    };
}