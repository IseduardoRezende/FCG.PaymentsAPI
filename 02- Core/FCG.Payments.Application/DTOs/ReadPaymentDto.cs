using FCG.Payments.Domain.Enums;

namespace FCG.Payments.Application.DTOs;

public class ReadPaymentDto
{
    public long Id { get; set; }

    public long OrderId { get; set; }

    public long UserId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public long GameId { get; set; }

    public string GameName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public PaymentStatus Status { get; set; }

    public DateTime ProcessedAt { get; set; }
}
