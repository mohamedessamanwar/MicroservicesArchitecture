namespace Micro.Shared.Http.Models;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? ErrorDetails { get; init; }
    public int? StatusCode { get; init; }
    public static ApiResult<T> Ok(T data, int statusCode = 200)
        => new()
        {
            Success = true,
            Data = data,
            StatusCode = statusCode
        };
    public static ApiResult<T> Fail(
        string errorCode,
        string errorMessage,
   int? statusCode = null,
   Dictionary<string, object>? errorDetails = null)
      => new()
      {
          Success = false,
          ErrorCode = errorCode,
          ErrorMessage = errorMessage,
          StatusCode = statusCode,
          ErrorDetails = errorDetails
      };
    public static ApiResult<T> Fail(Exception exception, int statusCode = 500)
     => new()
     {
         Success = false,
         ErrorCode = "EXCEPTION",
         ErrorMessage = exception.Message,
         StatusCode = statusCode,
         ErrorDetails = new Dictionary<string, object>
         {
             ["ExceptionType"] = exception.GetType().Name,
             ["StackTrace"] = exception.StackTrace ?? string.Empty
         }
     };
}
public class ApiResult : ApiResult<object>
{
    public static ApiResult OkResult(int statusCode = 200)
      => new()
      {
          Success = true,
          StatusCode = statusCode
      };
    public static ApiResult FailResult(
        string errorCode,
     string errorMessage,
        int? statusCode = null,
        Dictionary<string, object>? errorDetails = null)
        => new()
        {
            Success = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            StatusCode = statusCode,
            ErrorDetails = errorDetails
        };
}