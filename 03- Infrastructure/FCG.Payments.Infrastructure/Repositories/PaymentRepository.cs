using FCG.Payments.Domain.Entities;
using FCG.Payments.Domain.Repositories;
using FCG.Payments.Infrastructure.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace FCG.Payments.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentsDbContext _dbContext;

    public PaymentRepository(PaymentsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Payment?> GetByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Payment>().FirstOrDefaultAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task<bool> ExistsByOrderIdAsync(long orderId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<Payment>().AnyAsync(p => p.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _dbContext.Set<Payment>().AddAsync(payment, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(cancellationToken);
        _dbContext.ChangeTracker.Clear();
    }
}
