namespace FCG.Contracts.Events;

// Integration contracts shared by all FCG microservices.
// Keep this file identical in every repository: MassTransit routes messages by the full type name.

public record UserCreatedEvent(long UserId, string Name, string Email);

public record OrderPlacedEvent(long OrderId, long UserId, string UserEmail, long GameId, string GameName, decimal Price);

public record PaymentProcessedEvent(long OrderId, long UserId, string UserEmail, long GameId, string GameName, decimal Price, PaymentStatus Status);

public enum PaymentStatus
{
    Approved = 1,
    Rejected = 2
}
