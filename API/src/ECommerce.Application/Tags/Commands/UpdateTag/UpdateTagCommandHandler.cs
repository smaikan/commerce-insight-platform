using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Commands.UpdateTag;

public sealed class UpdateTagCommandHandler : IRequestHandler<UpdateTagCommand, TagDto>
{
    private readonly ITagRepository _tagRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateTagCommandHandler(
        ITagRepository tagRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada etiketi güncellemeden önce kaydı, isim ve URL çakışmasını kontrol ediyorum.
    public async Task<TagDto> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (tag is null)
        {
            throw new NotFoundException("Tag was not found.");
        }

        var url = string.IsNullOrWhiteSpace(request.Url)
            ? _urlGenerator.Generate(request.Name)
            : request.Url.Trim();

        if (await _tagRepository.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            throw new ConflictException("Tag name already exists.");
        }

        if (await _tagRepository.UrlExistsAsync(url, request.Id, cancellationToken))
        {
            throw new ConflictException("Tag url already exists.");
        }

        tag.Rename(request.Name);
        tag.ChangeUrl(url);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tag.ToDto();
    }
}
