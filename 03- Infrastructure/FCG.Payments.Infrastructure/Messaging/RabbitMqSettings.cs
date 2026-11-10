namespace FCG.Payments.Infrastructure.Messaging;

public class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public string VirtualHost { get; set; } = "/";

    public string Username { get; set; } = "fcg";

    public string Password { get; set; } = "fcg";

    public string OrderPlacedQueue { get; set; } = "payments-order-placed";
}
