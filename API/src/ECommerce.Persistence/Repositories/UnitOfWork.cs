using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ECommerce.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    // Burada aynı istek kapsamındaki veritabanı bağlamını hazırlıyorum.
    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    // Burada eşzamanlı güncelleme çakışmalarını Application katmanının anlayacağı hataya çeviriyorum.
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyException(
                "The record was changed by another operation. Refresh the data and try again.",
                exception);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqlException { Number: 547 or 2601 or 2627 })
        {
            throw new ConflictException(
                "A record with the same unique value already exists.",
                exception);
        }
    }

    // Burada kritik iş kurallarını eşzamanlı isteklerde de korumak için serializable transaction çalıştırıyorum.
    public async Task<T> ExecuteInSerializableTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var executionStrategy = _context.Database.CreateExecutionStrategy();
        return await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }
}
