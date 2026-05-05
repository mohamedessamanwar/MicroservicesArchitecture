using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;

namespace Micro.Shared.Idempotency;

public class IdempotencyFilter : IAsyncActionFilter
{
    private readonly IdempotencyService _idempotencyService;
    private readonly int _expirationHours;
    private const string IdempotencyHeader = "X-Idempotency-Key";

    public IdempotencyFilter(IdempotencyService idempotencyService, int expirationHours)
    {
        _idempotencyService = idempotencyService;
        _expirationHours = expirationHours;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // 1. Check if the header exists
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyHeader, out var idempotencyKey) || string.IsNullOrEmpty(idempotencyKey))
        {
            context.Result = new BadRequestObjectResult(new { error = $"Idempotency header '{IdempotencyHeader}' is required for this operation." });
            return;
        }

        // 2. Generate a unique key for the request scope (Action + Key)
        var actionName = context.ActionDescriptor.DisplayName;
        var fullKey = $"{actionName}:{idempotencyKey}";

        // 3. Check if we have a result for this key
        var existingResult = await _idempotencyService.GetResultAsync(fullKey);

        if (existingResult != null)
        {
            if (existingResult.IsProcessing)
            {
                context.Result = new ConflictObjectResult(new { error = "A request with the same idempotency key is already being processed." });
                return;
            }
            var cachedResult = new ContentResult
            {
                Content = existingResult.Body,
                ContentType = existingResult.ContentType,
                StatusCode = existingResult.StatusCode
            };
            context.Result = cachedResult;
            return;
        }

        // 4. Mark as "Processing"
        var canProceed = await _idempotencyService.TrySetProcessingAsync(fullKey, TimeSpan.FromHours(_expirationHours));
        if (!canProceed)
        {
            context.Result = new ConflictObjectResult(new { error = "A request with the same idempotency key is already being processed." });
            return;
        }

        try
        {
            // 5. Execute the action
            var executedContext = await next();

            if (executedContext.Exception != null && !executedContext.ExceptionHandled)
            {
                await _idempotencyService.RemoveAsync(fullKey);
                return;
            }

            // 6. Cache the result
            if (executedContext.Result is ObjectResult objectResult)
            {
                var result = new IdempotencyResult
                {
                    StatusCode = objectResult.StatusCode ?? 200,
                    ContentType = "application/json",
                    Body = JsonSerializer.Serialize(objectResult.Value),
                    IsProcessing = false
                };

                await _idempotencyService.SetResultAsync(fullKey, result, TimeSpan.FromHours(_expirationHours));
            }
            else if (executedContext.Result is StatusCodeResult statusCodeResult)
            {
                var result = new IdempotencyResult
                {
                    StatusCode = statusCodeResult.StatusCode,
                    IsProcessing = false
                };

                await _idempotencyService.SetResultAsync(fullKey, result, TimeSpan.FromHours(_expirationHours));
            }
        }
        catch (Exception)
        {
            await _idempotencyService.RemoveAsync(fullKey);
            throw;
        }
    }
}