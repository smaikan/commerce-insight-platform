using System.Globalization;
using System.Text;

namespace ECommerce.Application.Products.Services;

// Burada kullanıcı arama metnini SQL arama dokümanıyla aynı kararlı biçime dönüştürüyorum.
public static class ProductSearchTextNormalizer
{
    // Burada metni kırpıp boşlukları tekleştiriyor ve Türkçe harfleri karşılaştırılabilir ASCII biçimine katlıyorum.
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var collapsed = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var decomposed = collapsed.ToLower(new CultureInfo("tr-TR")).Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character == 'ı' ? 'i' : character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    // Burada çok kelimeli sorguyu AND semantiğinde kullanılacak benzersiz tokenlara ayırıyorum.
    public static IReadOnlyList<string> Tokenize(string normalizedQuery) =>
        normalizedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    // Burada her token için indeksli aday kümesini daraltacak iki veya üç karakterli ilk gramı üretiyorum.
    public static IReadOnlyList<string> CreateCandidateGrams(IReadOnlyList<string> tokens) =>
        tokens.Select(token => token[..Math.Min(3, token.Length)])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
}
