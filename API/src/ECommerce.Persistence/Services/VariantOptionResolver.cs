using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Persistence.Services;

public sealed class VariantOptionResolver : IVariantOptionResolver
{
    private readonly AppDbContext _context;

    // Burada varyant adı ve değerini aynı DbContext içinde çözmek için bağımlılığı hazırlıyorum.
    public VariantOptionResolver(AppDbContext context)
    {
        _context = context;
    }

    // Burada merkezi varyant adını ve ona bağlı değeri tam yazımıyla bulup eksikleri kayda ekliyorum.
    public async Task<VariantOptionSelection> ResolveAsync(
        string name,
        string value,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        var normalizedValue = value.Trim();
        var optionName = _context.VariantOptionNames.Local
            .FirstOrDefault(item => string.Equals(item.Name, normalizedName, StringComparison.Ordinal))
            ?? await _context.VariantOptionNames
                .FirstOrDefaultAsync(item => item.Name == normalizedName, cancellationToken);

        if (optionName is null)
        {
            optionName = new VariantOptionName(normalizedName);
            await _context.VariantOptionNames.AddAsync(optionName, cancellationToken);
        }

        var optionValue = _context.VariantOptionValues.Local
            .FirstOrDefault(item =>
                item.VariantOptionNameId == optionName.Id &&
                string.Equals(item.Value, normalizedValue, StringComparison.Ordinal))
            ?? await _context.VariantOptionValues
                .FirstOrDefaultAsync(item =>
                    item.VariantOptionNameId == optionName.Id && item.Value == normalizedValue,
                    cancellationToken);

        if (optionValue is null)
        {
            optionValue = new VariantOptionValue(optionName, normalizedValue);
            await _context.VariantOptionValues.AddAsync(optionValue, cancellationToken);
        }

        return new VariantOptionSelection(optionName, optionValue);
    }

    // Burada slash ile birleşmiş ad ve değerleri sırasıyla çözerek merkezi kayıtlara bağlıyorum.
    public async Task<IReadOnlyList<VariantOptionSelection>> ResolveCompositeAsync(string name, string value, CancellationToken cancellationToken = default)
    {
        var names = name.Split('/').Select(x => x.Trim()).ToArray(); var values = value.Split('/').Select(x => x.Trim()).ToArray();
        if (names.Length is < 1 or > 3 || names.Length != values.Length || names.Any(string.IsNullOrWhiteSpace) || values.Any(string.IsNullOrWhiteSpace) || names.Distinct(StringComparer.Ordinal).Count() != names.Length)
            throw new ArgumentException("Variant name and value must contain one to three matching unique parts.");
        var result = new List<VariantOptionSelection>();
        for (var i = 0; i < names.Length; i++) result.Add(await ResolveAsync(names[i], values[i], cancellationToken));
        return result;
    }
}
