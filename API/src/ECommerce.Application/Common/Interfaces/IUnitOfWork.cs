namespace ECommerce.Application.Common.Interfaces;

public interface IUnitOfWork
{
    // Burada takip edilen değişikliklerin kalıcı olarak kaydedilmesini tanımlıyorum.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    // Burada kritik işlemlerin serializable transaction içinde çalıştırılmasını tanımlıyorum.
    Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);
}
