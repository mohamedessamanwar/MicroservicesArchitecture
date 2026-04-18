using Microsoft.Extensions.Logging;
using Micro.Shared.Clients.Common;
using Micro.Shared.Clients.Payment.DTOs;
using Micro.Shared.Http.Models;
using Micro.Shared.Http.Policies;

namespace Micro.Shared.Clients.Payment;

public sealed class PaymentServiceClient : DownstreamApiClientBase, IPaymentServiceClient
{
    public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
        : base(httpClient, logger)
    {
    }

    public Task<ApiResult<PaymentDto>> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        // Write endpoints with side-effects default to no-retry unless explicitly idempotent.
        return PostAsync<CreatePaymentRequest, PaymentDto>(
            endpoint: "api/v1/payments",
            request: request,
            pipeline: ResiliencePipelineKeys.NoRetry,
            useIdempotencyKey: false,
            cancellationToken: cancellationToken);
    }
}