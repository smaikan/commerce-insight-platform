using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class AddressRepository : IAddressRepository
{
    private readonly AppDbContext _context;

    // Burada adres sorgu ve değişiklikleri için istek kapsamındaki DbContext'i hazırlıyorum.
    public AddressRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni adresi ilişkili kullanıcıyla birlikte kaydedilmek üzere takibe ekliyorum.
    public async Task AddAsync(Address address, CancellationToken cancellationToken = default)
    {
        await _context.Addresses.AddAsync(address, cancellationToken);
    }

    // Burada sahipliği zaten doğrulanmış adresi silinmek üzere işaretliyorum.
    public void Remove(Address address)
    {
        _context.Addresses.Remove(address);
    }

    // Burada kullanıcının adreslerini varsayılan seçim önce gelecek biçimde takip etmeden listeliyorum.
    public async Task<IReadOnlyList<Address>> GetByUserIdAsync(
        long userId,
        AddressType? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Addresses
            .AsNoTracking()
            .Where(address => address.UserId == userId);

        if (type.HasValue)
        {
            query = query.Where(address => address.Type == type.Value);
        }

        return await query
            .OrderByDescending(address => address.IsDefault)
            .ThenBy(address => address.Type)
            .ThenByDescending(address => address.UpdatedAt ?? address.CreatedAt)
            .ThenBy(address => address.Id)
            .ToListAsync(cancellationToken);
    }

    // Burada adresi yalnız gerçek sahibi için takip etmeden okuyarak kimlik sızmasını önlüyorum.
    public Task<Address?> GetByIdForUserAsync(
        Guid addressId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(
                address => address.Id == addressId && address.UserId == userId,
                cancellationToken);
    }

    // Burada adresi yalnız gerçek sahibi için takipli getirerek güvenli değişiklik akışını destekliyorum.
    public Task<Address?> GetByIdForUserForUpdateAsync(
        Guid addressId,
        long userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Addresses
            .FirstOrDefaultAsync(
                address => address.Id == addressId && address.UserId == userId,
                cancellationToken);
    }

    // Burada varsayılan seçimi değişmeden önce aynı kullanıcı ve türdeki diğer varsayılanları takipli getiriyorum.
    public async Task<IReadOnlyList<Address>> GetDefaultsForUserAndTypeForUpdateAsync(
        long userId,
        AddressType type,
        Guid? excludedAddressId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Addresses.Where(address =>
            address.UserId == userId &&
            address.Type == type &&
            address.IsDefault);

        if (excludedAddressId.HasValue)
        {
            query = query.Where(address => address.Id != excludedAddressId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    // Burada sipariş geçmişine bağlı adresin silinmesinden önce bağı kontrol ediyorum.
    public Task<bool> IsReferencedByOrderAsync(
        Guid addressId,
        CancellationToken cancellationToken = default)
    {
        return _context.Orders.AnyAsync(order => order.AddressId == addressId, cancellationToken);
    }
}
