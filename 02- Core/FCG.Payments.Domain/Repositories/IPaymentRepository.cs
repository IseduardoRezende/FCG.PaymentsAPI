using FCG.Payments.Domain.Entities;

namespace FCG.Payments.Domain.Repositories;

public interface IPaymentRepository
{
    Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);

    Task<bool> ExistsByOrderIdAsync(long orderId, CancellationToken cancellationToken = default);

    Task AddAsync(Payment payment, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
