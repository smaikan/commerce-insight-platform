using System.Globalization;
using System.Text;

namespace ECommerce.Application.Common.Services;

public sealed class ProductUrlGenerator : IProductUrlGenerator, IUrlGenerator
{
    public string Generate(string title)
    {
        var normalized = title.Trim().ToLowerInvariant()
            .Replace('ı', 'i')
            .Replace('ğ', 'g')
            .Replace('ü', 'u')
            .Replace('ş', 's')
            .Replace('ö', 'o')
            .Replace('ç', 'c');

        var builder = new StringBuilder();
        var previousWasSeparator = false;

        foreach (var character in normalized.Normalize(NormalizationForm.FormD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        var url = builder.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(url) ? "product" : url;
    }
}
