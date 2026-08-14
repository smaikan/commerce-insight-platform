namespace ECommerce.Application.Common.Models;

// Burada normalize edilmiş public arama metnini ve SQL aday gramlarını taşıyorum.
public sealed record PublishedProductSearchFilter(
    string NormalizedQuery,
    IReadOnlyList<string> Tokens,
    IReadOnlyList<string> CandidateGrams,
    int Limit);
