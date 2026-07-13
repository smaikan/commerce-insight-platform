using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Queries.GetTags;

public sealed class GetTagsQueryHandler : IRequestHandler<GetTagsQuery, IReadOnlyList<TagDto>>
{
    private readonly ITagRepository _tagRepository;

    public GetTagsQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    // Burada etiket listesini okuyup DTO olarak hazırlıyorum.
    public async Task<IReadOnlyList<TagDto>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        var tags = await _tagRepository.GetListAsync(cancellationToken);
        return tags.Select(tag => tag.ToDto()).ToList();
    }
}
