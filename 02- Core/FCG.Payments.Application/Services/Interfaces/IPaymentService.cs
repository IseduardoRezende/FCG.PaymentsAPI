using FCG.Contracts.Events;
using FCG.Payments.Application.DTOs;
using FCG.Payments.Domain.Commons.Result;

namespace FCG.Payments.Application.Services.Interfaces;

public interface IPaymentService
{
    Task<bool> ProcessAsync(OrderPlacedEvent order, CancellationToken cancellationToken = default);

    Task<Result<ReadPaymentDto>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);
}
