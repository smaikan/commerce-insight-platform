using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Repositories;

public sealed class TagRepository : ITagRepository
{
    private readonly AppDbContext _context;

    public TagRepository(AppDbContext context)
    {
        _context = context;
    }

    // Burada yeni etiketi veritabanı takibine ekliyorum.
    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
    {
        await _context.Tags.AddAsync(tag, cancellationToken);
    }

    // Burada birden fazla etiketi tek seferde veritabanı takibine ekliyorum.
    public async Task AddRangeAsync(IReadOnlyCollection<Tag> tags, CancellationToken cancellationToken = default)
    {
        await _context.Tags.AddRangeAsync(tags, cancellationToken);
    }

    // Burada etiketi okuma amaçlı takip etmeden getiriyorum.
    public Task<Tag?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Tags
            .AsNoTracking()
            .FirstOrDefaultAsync(tag => tag.Id == id, cancellationToken);
    }

    // Burada etiketi güncelleme için takipli şekilde getiriyorum.
    public Task<Tag?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Tags
            .FirstOrDefaultAsync(tag => tag.Id == id, cancellationToken);
    }

    // Burada etiketleri ada göre sıralı şekilde getiriyorum.
    public async Task<IReadOnlyList<Tag>> GetListAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Tags
            .AsNoTracking()
            .OrderBy(tag => tag.Name)
            .ToListAsync(cancellationToken);
    }

    // Burada verilen id listesindeki mevcut etiket idlerini buluyorum.
    public async Task<IReadOnlySet<Guid>> GetExistingIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var tagIds = ids
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        var existingIds = await _context.Tags
            .AsNoTracking()
            .Where(tag => tagIds.Contains(tag.Id))
            .Select(tag => tag.Id)
            .ToListAsync(cancellationToken);

        return existingIds.ToHashSet();
    }

    // Burada verilen isimlerden veritabanında olan etiket isimlerini buluyorum.
    public async Task<IReadOnlySet<string>> GetExistingNamesAsync(IEnumerable<string> names, CancellationToken cancellationToken = default)
    {
        var normalizedNames = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingNames = await _context.Tags
            .AsNoTracking()
            .Where(tag => normalizedNames.Contains(tag.Name))
            .Select(tag => tag.Name)
            .ToListAsync(cancellationToken);

        return existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // Burada listedeki URL değerlerinden veritabanında olan etiket URL'lerini buluyorum.
    public async Task<IReadOnlySet<string>> GetExistingUrlsAsync(IEnumerable<string> urls, CancellationToken cancellationToken = default)
    {
        var normalizedUrls = urls
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .Select(url => url.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingUrls = await _context.Tags
            .AsNoTracking()
            .Where(tag => normalizedUrls.Contains(tag.Url))
            .Select(tag => tag.Url)
            .ToListAsync(cancellationToken);

        return existingUrls.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    // Burada etiket adının başka bir etikette kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> NameExistsAsync(string name, Guid? excludedTagId = null, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();

        return _context.Tags.AnyAsync(
            tag => tag.Name == normalizedName && (!excludedTagId.HasValue || tag.Id != excludedTagId.Value),
            cancellationToken);
    }

    // Burada etiket URL bilgisinin başka bir etikette kullanılıp kullanılmadığını kontrol ediyorum.
    public Task<bool> UrlExistsAsync(string url, Guid? excludedTagId = null, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = url.Trim();

        return _context.Tags.AnyAsync(
            tag => tag.Url == normalizedUrl && (!excludedTagId.HasValue || tag.Id != excludedTagId.Value),
            cancellationToken);
    }
}
