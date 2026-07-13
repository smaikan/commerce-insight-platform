using ECommerce.Application.Collections.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Collections.Commands.BulkCreateCollections;

public sealed class BulkCreateCollectionsCommandHandler
    : IRequestHandler<BulkCreateCollectionsCommand, IReadOnlyList<CollectionDto>>
{
    private readonly ICollectionRepository _collectionRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public BulkCreateCollectionsCommandHandler(
        ICollectionRepository collectionRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _collectionRepository = collectionRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada koleksiyonları toplu şekilde oluşturmadan önce URL çakışmalarını kontrol ediyorum.
    public async Task<IReadOnlyList<CollectionDto>> Handle(
        BulkCreateCollectionsCommand request,
        CancellationToken cancellationToken)
    {
        var preparedItems = request.Collections
            .Select(item => new PreparedCollectionItem(
                item,
                string.IsNullOrWhiteSpace(item.Url) ? _urlGenerator.Generate(item.Name) : item.Url.Trim()))
            .ToList();

        EnsureNoDuplicateUrls(preparedItems.Select(item => item.Url));

        var existingUrls = await _collectionRepository.GetExistingUrlsAsync(
            preparedItems.Select(item => item.Url),
            cancellationToken);

        if (existingUrls.Count > 0)
        {
            throw new ConflictException($"Collection url already exists: {string.Join(", ", existingUrls)}.");
        }

        var collections = preparedItems
            .Select(item => new Collection(
                item.Item.Name,
                item.Url,
                item.Item.Description,
                item.Item.IsActive,
                item.Item.IsFeatured,
                item.Item.DisplayOrder))
            .ToList();

        await _collectionRepository.AddRangeAsync(collections, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return collections.Select(collection => collection.ToDto()).ToList();
    }

    private static void EnsureNoDuplicateUrls(IEnumerable<string> urls)
    {
        var duplicates = urls
            .GroupBy(url => url.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ConflictException($"Collection url is duplicated in the request: {string.Join(", ", duplicates)}.");
        }
    }

    private sealed record PreparedCollectionItem(BulkCreateCollectionItem Item, string Url);
}
