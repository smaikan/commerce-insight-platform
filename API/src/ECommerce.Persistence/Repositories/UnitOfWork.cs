using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ECommerce.Persistence.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly ILogger<UnitOfWork> _logger;

    // Burada aynı istek kapsamındaki veritabanı bağlamını hazırlıyorum.
    public UnitOfWork(AppDbContext context, ILogger<UnitOfWork>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<UnitOfWork>.Instance;
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
            LogConcurrencyConflict(exception);
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
        var attempt = 0;
        return await executionStrategy.ExecuteAsync(async () =>
        {
            if (attempt++ > 0)
            {
                _context.ChangeTracker.Clear();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        });
    }

    // Burada concurrency hatasına katılan entity ve tokenları hassas değerleri açığa çıkarmadan kaydediyorum.
    private void LogConcurrencyConflict(DbUpdateConcurrencyException exception)
    {
        var innerException = exception.InnerException;
        var innerMessageFingerprint = innerException is null
            ? "none"
            : CreateSafeFingerprint(innerException.Message);

        foreach (var entry in exception.Entries)
        {
            var tokenFingerprints = entry.Metadata
                .GetProperties()
                .Where(property => property.IsConcurrencyToken)
                .Select(property =>
                {
                    var propertyEntry = entry.Property(property.Name);
                    return $"{property.Name}:original={CreateSafeFingerprint(propertyEntry.OriginalValue)}," +
                           $"current={CreateSafeFingerprint(propertyEntry.CurrentValue)}";
                })
                .ToArray();

            _logger.LogWarning(
                "EF concurrency conflict. Entity={EntityType}, State={EntityState}, Tokens={TokenFingerprints}, " +
                "InnerExceptionType={InnerExceptionType}, InnerExceptionMessageFingerprint={InnerExceptionMessageFingerprint}",
                entry.Metadata.ClrType.FullName,
                entry.State,
                tokenFingerprints,
                innerException?.GetType().FullName ?? "none",
                innerMessageFingerprint);
        }
    }

    // Burada tanı değerini SHA-256 ile tek yönlü ve kısa bir parmak izine dönüştürüyorum.
    private static string CreateSafeFingerprint(object? value)
    {
        var canonicalValue = value switch
        {
            null => "<null>",
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? "<null>"
        };
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalValue));
        return Convert.ToHexString(hash.AsSpan(0, 8));
    }
}
