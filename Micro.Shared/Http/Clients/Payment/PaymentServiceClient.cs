using Microsoft.Extensions.Logging;
using Micro.Shared.Http.Models;
using Micro.Shared.Http.Policies;
using Micro.Shared.Http.Clients.Common;
using Micro.Shared.Http.Clients.Payment.DTOs;

namespace Micro.Shared.Http.Clients.Payment;

public sealed class PaymentServiceClient : DownstreamApiClientBase, IPaymentServiceClient
{
     public PaymentServiceClient(HttpClient httpClient, ILogger<PaymentServiceClient> logger)
         : base(httpClient, logger)
     {
     }

     public Task<ApiResult<PaymentDto>> CreatePaymentAsync(
         CreatePaymentRequest request,
         string? idempotencyKey = null,
         CancellationToken cancellationToken = default)
     {
          var options = new OutboundHttpRequestOptions();
          if (!string.IsNullOrEmpty(idempotencyKey))
          {
              options.Headers["X-Idempotency-Key"] = idempotencyKey;
          }

          return PostAsync<CreatePaymentRequest, PaymentDto>(
              endpoint: "api/v1/payments",
              request: request,
              pipeline: ResiliencePipelineKeys.NoRetry,
              useIdempotencyKey: true,
              requestOptions: options,
              cancellationToken: cancellationToken);
     }
}