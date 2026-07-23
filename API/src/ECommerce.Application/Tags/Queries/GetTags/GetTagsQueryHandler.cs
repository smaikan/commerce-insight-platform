using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Queries.GetTags;

public sealed class GetTagsQueryHandler : IRequestHandler<GetTagsQuery, PagedResult<TagDto>>
{
    private readonly ITagRepository _tagRepository;

    public GetTagsQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    // Burada etiket listesini okuyup DTO olarak hazırlıyorum.
    public async Task<PagedResult<TagDto>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        var tags = await _tagRepository.GetListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return tags.Map(tag => tag.ToDto());
    }
}
