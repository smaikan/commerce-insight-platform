using ECommerce.Application.Common.Models;
using ECommerce.Domain.Entities;

namespace ECommerce.Application.Common.Interfaces;

public interface ICartRepository
{
    // Burada yeni sepeti veritabanı takibine ekleme sözleşmesini tanımlıyorum.
    Task AddAsync(Cart cart, CancellationToken cancellationToken = default);

    // Burada birleştirme sonrası gereksiz sepeti silme sözleşmesini tanımlıyorum.
    void Remove(Cart cart);

    // Burada sepeti kimliği ve ekran için gereken ilişkileriyle takip etmeden getirme sözleşmesini tanımlıyorum.
    Task<Cart?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // Burada sepeti güvenli sahibi üzerinden takip etmeden getirme sözleşmesini tanımlıyorum.
    Task<Cart?> GetByOwnerAsync(
        CartOwner owner,
        CancellationToken cancellationToken = default);

    // Burada sepeti güvenli sahibi üzerinden değişiklik takibiyle getirme sözleşmesini tanımlıyorum.
    Task<Cart?> GetByOwnerForUpdateAsync(
        CartOwner owner,
        CancellationToken cancellationToken = default);
}
