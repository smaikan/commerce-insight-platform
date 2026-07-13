using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Tags.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Tags.Commands.CreateTag;

public sealed class CreateTagCommandHandler : IRequestHandler<CreateTagCommand, TagDto>
{
    private readonly ITagRepository _tagRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateTagCommandHandler(
        ITagRepository tagRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada yeni etiketi oluştururken isim ve URL değerlerini benzersiz tutuyorum.
    public async Task<TagDto> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        var url = string.IsNullOrWhiteSpace(request.Url)
            ? _urlGenerator.Generate(request.Name)
            : request.Url.Trim();

        if (await _tagRepository.NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Tag name already exists.");
        }

        if (await _tagRepository.UrlExistsAsync(url, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Tag url already exists.");
        }

        var tag = new Tag(request.Name, url, request.IsActive);

        await _tagRepository.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tag.ToDto();
    }
}
