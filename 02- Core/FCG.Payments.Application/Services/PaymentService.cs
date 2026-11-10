using FCG.Contracts.Events;
using FCG.Payments.Application.Abstractions;
using FCG.Payments.Application.DTOs;
using FCG.Payments.Application.Services.Interfaces;
using FCG.Payments.Domain.Commons;
using FCG.Payments.Domain.Commons.Result;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Repositories;
using FCG.Payments.Domain.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ContractPaymentStatus = FCG.Contracts.Events.PaymentStatus;

namespace FCG.Payments.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICurrentUser _currentUser;
    private readonly PaymentSettings _settings;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IEventPublisher eventPublisher,
        ICurrentUser currentUser,
        IOptions<PaymentSettings> settings,
        ILogger<PaymentService> logger)
    {
        _paymentRepository = paymentRepository;
        _eventPublisher = eventPublisher;
        _currentUser = currentUser;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> ProcessAsync(OrderPlacedEvent order, CancellationToken cancellationToken = default)
    {
        if (await _paymentRepository.ExistsByOrderIdAsync(order.OrderId, cancellationToken))
        {
            _logger.LogInformation("Order {OrderId} was already processed. Duplicated message ignored.", order.OrderId);
            return false;
        }

        var payment = Payment.Process(
            order.OrderId,
            order.UserId,
            order.UserEmail,
            order.GameId,
            order.GameName,
            order.Price,
            _settings.MaxApprovedAmount,
            DateTime.UtcNow);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _paymentRepository.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync(new PaymentProcessedEvent(
            payment.OrderId,
            payment.UserId,
            payment.UserEmail,
            payment.GameId,
            payment.GameName,
            payment.Amount,
            (ContractPaymentStatus)payment.Status), cancellationToken);

        _logger.LogInformation("Payment of order {OrderId} ({Amount}) was {Status}.", payment.OrderId, payment.Amount, payment.Status);
        return true;
    }

    public async Task<Result<ReadPaymentDto>> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        if (payment is null)
            return NotFoundResult<ReadPaymentDto>.Create(new Error("Payment not found for this order."));

        if (payment.UserId != _currentUser.UserId && !_currentUser.IsAdministrator)
            return ForbiddenResult<ReadPaymentDto>.Create(new Error("You can only access your own payments."));

        return SuccessResult<ReadPaymentDto>.Create(new ReadPaymentDto
        {
            Id = payment.Id,
            OrderId = payment.OrderId,
            UserId = payment.UserId,
            UserEmail = payment.UserEmail,
            GameId = payment.GameId,
            GameName = payment.GameName,
            Amount = payment.Amount,
            Status = payment.Status,
            ProcessedAt = payment.ProcessedAt
        });
    }
}
