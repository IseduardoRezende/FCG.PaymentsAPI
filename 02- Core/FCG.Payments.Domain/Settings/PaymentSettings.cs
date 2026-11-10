namespace FCG.Payments.Domain.Settings;

public class PaymentSettings
{
    public const string SectionName = "Payments";

    public decimal MaxApprovedAmount { get; set; } = 100m;
}
