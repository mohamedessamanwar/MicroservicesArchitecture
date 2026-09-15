using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OrderService.Api.Filters;

[AttributeUsage(AttributeTargets.Method)]
public class RequireIdempotencyAttribute : ActionFilterAttribute
{
    private const string IdempotencyHeaderName = "X-Idempotency-Key";

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(IdempotencyHeaderName, out var value) || string.IsNullOrWhiteSpace(value))
        {
            context.Result = new BadRequestObjectResult(new
            {
                Message = $"Missing required header: {IdempotencyHeaderName}",
                ErrorCode = "MISSING_IDEMPOTENCY_KEY"
            });
            return;
        }

        // Store it in HttpContext.Items for easy retrieval if needed, 
        // though the controller can also just grab it from headers.
        context.HttpContext.Items[IdempotencyHeaderName] = value.ToString();

        base.OnActionExecuting(context);
    }
}
