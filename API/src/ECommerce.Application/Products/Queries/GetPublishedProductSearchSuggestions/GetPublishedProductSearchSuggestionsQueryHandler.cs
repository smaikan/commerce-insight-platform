using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Products.Dtos;
using ECommerce.Application.Products.Services;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetPublishedProductSearchSuggestions;

public sealed class GetPublishedProductSearchSuggestionsQueryHandler
    : IRequestHandler<GetPublishedProductSearchSuggestionsQuery, PublishedProductSearchSuggestionsDto>
{
    private readonly IPublishedProductSearchReader _reader;

    // Burada öneri sorgusunun salt-okunur veri kaynağını hazırlıyorum.
    public GetPublishedProductSearchSuggestionsQueryHandler(IPublishedProductSearchReader reader)
    {
        _reader = reader;
    }

    // Burada normalize edilmiş arama tokenlarını ve aday gramlarını tek SQL okumasına iletiyorum.
    public Task<PublishedProductSearchSuggestionsDto> Handle(
        GetPublishedProductSearchSuggestionsQuery request,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = ProductSearchTextNormalizer.Normalize(request.Query);
        var tokens = ProductSearchTextNormalizer.Tokenize(normalizedQuery);
        return _reader.GetSuggestionsAsync(
            new PublishedProductSearchFilter(
                normalizedQuery,
                tokens,
                ProductSearchTextNormalizer.CreateCandidateGrams(tokens),
                request.Limit),
            cancellationToken);
    }
}
