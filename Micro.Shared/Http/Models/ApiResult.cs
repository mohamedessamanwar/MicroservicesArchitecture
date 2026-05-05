namespace Micro.Shared.Http.Models;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public bool TransportSuccess { get; init; }
    public T? Data { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public Dictionary<string, object>? ErrorDetails { get; init; }
    public int? StatusCode { get; init; }
    public string? RawBody { get; init; }
    public static ApiResult<T> Ok(T data, int statusCode = 200, string? rawBody = null)
        => new()
        {
            Success = true,
            TransportSuccess = true,
            Data = data,
            StatusCode = statusCode,
            RawBody = rawBody
        };
    public static ApiResult<T> Fail(
        string errorCode,
        string errorMessage,
   int? statusCode = null,
   Dictionary<string, object>? errorDetails = null,
   string? rawBody = null,
   bool transportSuccess = false)
      => new()
      {
          Success = false,
          TransportSuccess = transportSuccess,
          ErrorCode = errorCode,
          ErrorMessage = errorMessage,
          StatusCode = statusCode,
          ErrorDetails = errorDetails,
          RawBody = rawBody
      };
    public static ApiResult<T> Fail(Exception exception, int statusCode = 500)
     => new()
     {
         Success = false,
         TransportSuccess = false,
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
          TransportSuccess = true,
          StatusCode = statusCode
      };
    public static ApiResult FailResult(
        string errorCode,
     string errorMessage,
        int? statusCode = null,
        Dictionary<string, object>? errorDetails = null,
        string? rawBody = null,
        bool transportSuccess = false)
        => new()
        {
            Success = false,
            TransportSuccess = transportSuccess,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            StatusCode = statusCode,
            ErrorDetails = errorDetails,
            RawBody = rawBody
        };
}