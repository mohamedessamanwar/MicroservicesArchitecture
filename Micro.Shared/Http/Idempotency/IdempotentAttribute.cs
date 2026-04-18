using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Net;

namespace Micro.Shared.Http.Idempotency;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class IdempotentAttribute : Attribute, IFilterFactory
{
    public bool IsReusable => true;

    public int ExpirationHours { get; set; } = 24;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        return new IdempotencyFilter(serviceProvider.GetRequiredService<IdempotencyService>(), ExpirationHours);
    }
}