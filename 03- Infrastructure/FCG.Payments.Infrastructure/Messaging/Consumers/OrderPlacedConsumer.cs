using FCG.Contracts.Events;
using FCG.Payments.Application.Services.Interfaces;
using MassTransit;

namespace FCG.Payments.Infrastructure.Messaging.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly IPaymentService _paymentService;

    public OrderPlacedConsumer(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    public Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        return _paymentService.ProcessAsync(context.Message, context.CancellationToken);
    }
}
