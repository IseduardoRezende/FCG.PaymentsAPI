using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Enums;

namespace FCG.Payments.UnitTests.Domain;

public class PaymentTests
{
    private const decimal Limit = 100m;

    private static Payment ProcessWithAmount(decimal amount) =>
        Payment.Process(orderId: 1, userId: 2, "player@test.com", gameId: 3, "Celeste", amount, Limit, DateTime.UtcNow);

    [Theory]
    [InlineData(0.01)]
    [InlineData(59.90)]
    [InlineData(99.99)]
    public void Process_WithAmountBelowLimit_IsApproved(decimal amount)
    {
        Assert.Equal(PaymentStatus.Approved, ProcessWithAmount(amount).Status);
    }

    [Fact]
    public void Process_WithAmountExactlyAtLimit_IsApproved()
    {
        Assert.Equal(PaymentStatus.Approved, ProcessWithAmount(Limit).Status);
    }

    [Theory]
    [InlineData(100.01)]
    [InlineData(199.90)]
    public void Process_WithAmountAboveLimit_IsRejected(decimal amount)
    {
        Assert.Equal(PaymentStatus.Rejected, ProcessWithAmount(amount).Status);
    }

    [Fact]
    public void Process_ForAnyResult_KeepsOrderDataForTraceability()
    {
        var processedAt = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

        var payment = Payment.Process(10, 20, "player@test.com", 30, "Celeste", 39.90m, Limit, processedAt);

        Assert.Equal(10, payment.OrderId);
        Assert.Equal(20, payment.UserId);
        Assert.Equal("player@test.com", payment.UserEmail);
        Assert.Equal(30, payment.GameId);
        Assert.Equal("Celeste", payment.GameName);
        Assert.Equal(39.90m, payment.Amount);
        Assert.Equal(processedAt, payment.ProcessedAt);
    }
}
