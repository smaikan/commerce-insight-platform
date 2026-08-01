using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Queries.GetPublishedProductByUrl;

public sealed record GetPublishedProductByUrlQuery(string Url) : IRequest<ProductSeoDto>;
