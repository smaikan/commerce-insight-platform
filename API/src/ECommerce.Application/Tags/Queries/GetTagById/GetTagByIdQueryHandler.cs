using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Queries.GetTagById;

public sealed class GetTagByIdQueryHandler : IRequestHandler<GetTagByIdQuery, TagDto>
{
    private readonly ITagRepository _tagRepository;

    public GetTagByIdQueryHandler(ITagRepository tagRepository)
    {
        _tagRepository = tagRepository;
    }

    // Burada istenen etiketi bulup detay cevabına çeviriyorum.
    public async Task<TagDto> Handle(GetTagByIdQuery request, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdAsync(request.Id, cancellationToken);

        if (tag is null)
        {
            throw new NotFoundException("Tag was not found.");
        }

        return tag.ToDto();
    }
}
