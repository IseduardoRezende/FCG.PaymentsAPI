using FCG.Contracts.Events;
using FCG.Payments.Application.Services.Interfaces;
using FCG.Payments.Infrastructure.Messaging.Consumers;
using MassTransit;
using Moq;

namespace FCG.Payments.UnitTests.Messaging;

public class OrderPlacedConsumerTests
{
    [Fact]
    public async Task Consume_WithOrderPlacedEvent_DelegatesToPaymentService()
    {
        var paymentService = new Mock<IPaymentService>();
        var message = new OrderPlacedEvent(1, 2, "player@test.com", 3, "Celeste", 39.90m);
        var context = new Mock<ConsumeContext<OrderPlacedEvent>>();
        context.Setup(x => x.Message).Returns(message);
        context.Setup(x => x.CancellationToken).Returns(TestContext.Current.CancellationToken);

        await new OrderPlacedConsumer(paymentService.Object).Consume(context.Object);

        paymentService.Verify(x => x.ProcessAsync(message, It.IsAny<CancellationToken>()), Times.Once);
    }
}
