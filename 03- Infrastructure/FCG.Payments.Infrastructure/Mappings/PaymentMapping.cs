using FCG.Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FCG.Payments.Infrastructure.Mappings;

public class PaymentMapping : BaseEntityMapping<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);

        builder.Property(c => c.UserEmail).HasMaxLength(150).IsRequired();
        builder.Property(c => c.GameName).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Amount).HasPrecision(18, 2);

        builder.HasIndex(c => c.OrderId).IsUnique();
    }
}
