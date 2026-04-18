using Micro.Shared.Clients.Payment.DTOs;
using Micro.Shared.Http.Models;

namespace Micro.Shared.Clients.Payment;

public interface IPaymentServiceClient
{
    Task<ApiResult<PaymentDto>> CreatePaymentAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);
}