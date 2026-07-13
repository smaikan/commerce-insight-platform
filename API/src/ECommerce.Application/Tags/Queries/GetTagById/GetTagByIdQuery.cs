using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Queries.GetTagById;

public sealed record GetTagByIdQuery(Guid Id) : IRequest<TagDto>;
