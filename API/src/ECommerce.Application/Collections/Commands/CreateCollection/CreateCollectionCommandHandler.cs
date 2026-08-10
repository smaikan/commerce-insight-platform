using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Collections.Commands.CreateCollection;

public sealed class CreateCollectionCommandHandler : IRequestHandler<CreateCollectionCommand, CollectionDto>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    // Burada koleksiyon oluşturma bağımlılıklarını hazırlıyorum.
    public CreateCollectionCommandHandler(
        ICollectionRepository collectionRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _collectionRepository = collectionRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada yeni koleksiyonu oluştururken URL değerini hazır hale getiriyorum.
    public async Task<CollectionDto> Handle(CreateCollectionCommand request, CancellationToken cancellationToken)
    {
        var url = string.IsNullOrWhiteSpace(request.Url)
            ? _urlGenerator.Generate(request.Name)
            : request.Url.Trim();

        if (await _collectionRepository.UrlExistsAsync(url, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Collection url already exists.");
        }

        var collection = new Collection(
            request.Name,
            url,
            request.Description,
            request.IsActive,
            request.IsFeatured,
            request.DisplayOrder,
            request.ImageUrl);

        await _collectionRepository.AddAsync(collection, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return collection.ToDto();
    }
}
