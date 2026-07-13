using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using MediatR;

namespace ECommerce.Application.Collections.Commands.UpdateCollection;

public sealed class UpdateCollectionCommandHandler : IRequestHandler<UpdateCollectionCommand, CollectionDto>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCollectionCommandHandler(
        ICollectionRepository collectionRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _collectionRepository = collectionRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada koleksiyonu güncellemeden önce kaydı ve URL çakışmasını kontrol ediyorum.
    public async Task<CollectionDto> Handle(UpdateCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _collectionRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (collection is null)
        {
            throw new NotFoundException("Collection was not found.");
        }

        var url = string.IsNullOrWhiteSpace(request.Url)
            ? _urlGenerator.Generate(request.Name)
            : request.Url.Trim();

        if (await _collectionRepository.UrlExistsAsync(url, request.Id, cancellationToken))
        {
            throw new ConflictException("Collection url already exists.");
        }

        collection.Rename(request.Name);
        collection.ChangeUrl(url);
        collection.SetDescription(request.Description);
        collection.SetDisplayOrder(request.DisplayOrder);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return collection.ToDto();
    }
}
