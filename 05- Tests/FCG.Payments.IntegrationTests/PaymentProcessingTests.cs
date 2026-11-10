using System.Net;
using FCG.Contracts.Events;
using FCG.Payments.Domain.Entities;
using FCG.Payments.Infrastructure.DbContexts;
using FCG.Payments.IntegrationTests.Infrastructure;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FCG.Payments.IntegrationTests;

public class PaymentProcessingTests
{
    private readonly PaymentsApiFactory _factory;

    public PaymentProcessingTests(PaymentsApiFactory factory)
    {
        _factory = factory;
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private ITestHarness Harness => _factory.Services.GetTestHarness();

    private async Task<OrderPlacedEvent> PublishOrderAsync(decimal price, long? userId = null)
    {
        var order = new OrderPlacedEvent(TestClient.NewId(), userId ?? TestClient.NewId(), "player@test.com", 10, "Celeste", price);
        await Harness.Bus.Publish(order, Ct);
        return order;
    }

    private async Task<List<Payment>> GetPaymentsAsync(long orderId)
    {
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentsDbContext>();
        return await dbContext.Set<Payment>().Where(p => p.OrderId == orderId).ToListAsync(Ct);
    }

    private async Task WaitForPaymentAsync(long orderId) =>
        await TestClient.WaitUntilAsync(async () => (await GetPaymentsAsync(orderId)).Count > 0, Ct);

    private int ConsumedOrders(long orderId) =>
        Harness.Consumed.Select<OrderPlacedEvent>(m => m.Context.Message.OrderId == orderId).Count();

    private int PublishedPayments(long orderId) =>
        Harness.Published.Select<PaymentProcessedEvent>(m => m.Context.Message.OrderId == orderId).Count();

    [Fact]
    public async Task OrderPlaced_WithPriceWithinLimit_PersistsApprovedPaymentAndPublishesApproved()
    {
        var order = await PublishOrderAsync(59.90m);

        await WaitForPaymentAsync(order.OrderId);

        Assert.Equal(FCG.Payments.Domain.Enums.PaymentStatus.Approved, Assert.Single(await GetPaymentsAsync(order.OrderId)).Status);
        Assert.True(await Harness.Published.Any<PaymentProcessedEvent>(m =>
            m.Context.Message.OrderId == order.OrderId && m.Context.Message.Status == PaymentStatus.Approved, Ct));
    }

    [Fact]
    public async Task OrderPlaced_WithPriceExactlyAtLimit_IsApproved()
    {
        var order = await PublishOrderAsync(PaymentsApiFactory.MaxApprovedAmount);

        Assert.True(await Harness.Published.Any<PaymentProcessedEvent>(m =>
            m.Context.Message.OrderId == order.OrderId && m.Context.Message.Status == PaymentStatus.Approved, Ct));
    }

    [Fact]
    public async Task OrderPlaced_WithPriceAboveLimit_PersistsRejectedPaymentAndPublishesRejected()
    {
        var order = await PublishOrderAsync(199.90m);

        await WaitForPaymentAsync(order.OrderId);

        Assert.Equal(FCG.Payments.Domain.Enums.PaymentStatus.Rejected, Assert.Single(await GetPaymentsAsync(order.OrderId)).Status);
        Assert.True(await Harness.Published.Any<PaymentProcessedEvent>(m =>
            m.Context.Message.OrderId == order.OrderId && m.Context.Message.Status == PaymentStatus.Rejected, Ct));
    }

    [Fact]
    public async Task OrderPlaced_DeliveredTwice_ProcessesAndPublishesOnlyOnce()
    {
        var order = await PublishOrderAsync(10m);
        await Harness.Bus.Publish(order, Ct);

        await TestClient.WaitUntilAsync(() => Task.FromResult(ConsumedOrders(order.OrderId) == 2), Ct);

        Assert.Single(await GetPaymentsAsync(order.OrderId));
        Assert.Equal(1, PublishedPayments(order.OrderId));
    }

    [Fact]
    public async Task GetPaymentByOrder_AsOwner_Returns200WithStatus()
    {
        var userId = TestClient.NewId();
        var order = await PublishOrderAsync(10m, userId);
        await WaitForPaymentAsync(order.OrderId);

        var response = await _factory.AsUser(userId).GetAsync($"/api/v1/payments/orders/{order.OrderId}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Approved", (await response.ReadValueAsync(Ct)).GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetPaymentByOrder_AsAnotherUser_Returns403()
    {
        var order = await PublishOrderAsync(10m);
        await WaitForPaymentAsync(order.OrderId);

        var response = await _factory.AsUser(TestClient.NewId()).GetAsync($"/api/v1/payments/orders/{order.OrderId}", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentByOrder_WithoutToken_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/v1/payments/orders/1", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPaymentByOrder_ForUnprocessedOrder_Returns404()
    {
        var response = await _factory.AsUser(TestClient.NewId(), "Administrator").GetAsync($"/api/v1/payments/orders/{TestClient.NewId()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Health_WhenDatabaseIsReachable_Returns200()
    {
        var response = await _factory.CreateClient().GetAsync("/health", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
