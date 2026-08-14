using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetPublishedProductSearchSuggestions;

// Burada navbar için public ürün önerisi isteğini tanımlıyorum.
public sealed record GetPublishedProductSearchSuggestionsQuery(
    string? Query,
    int Limit = 10) : IRequest<PublishedProductSearchSuggestionsDto>;
