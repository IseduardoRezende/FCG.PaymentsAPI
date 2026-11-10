using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Domain.Entities;

public class Payment : BaseEntity
{
    public long OrderId { get; set; }

    public long UserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public long GameId { get; set; }

    public string GameName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime ProcessedAt { get; set; }

    public static Payment Process(
        long orderId,
        long userId,
        string userEmail,
        long gameId,
        string gameName,
        decimal amount,
        decimal maxApprovedAmount,
        DateTime processedAt)
    {
        return new Payment
        {
            OrderId = orderId,
            UserId = userId,
            UserEmail = userEmail,
            GameId = gameId,
            GameName = gameName,
            Amount = amount,
            Status = amount <= maxApprovedAmount ? PaymentStatus.Approved : PaymentStatus.Rejected,
            ProcessedAt = processedAt
        };
    }
}
