using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Relations.Commands.UpdateProductRelations;

public sealed class UpdateProductRelationsCommandHandler : IRequestHandler<UpdateProductRelationsCommand>
{
    private readonly IProductRepository _products;
    private readonly ICollectionRepository _collections;
    private readonly ITagRepository _tags;
    private readonly IProductTagResolver _productTagResolver;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ürün ilişkilerini güncellemek için gereken repository ve etiket çözümleyicilerini hazırlıyorum.
    public UpdateProductRelationsCommandHandler(
        IProductRepository products,
        ICollectionRepository collections,
        ITagRepository tags,
        IProductTagResolver productTagResolver,
        IUnitOfWork unitOfWork)
    {
        _products = products;
        _collections = collections;
        _tags = tags;
        _productTagResolver = productTagResolver;
        _unitOfWork = unitOfWork;
    }

    // Burada koleksiyon, etiket ve bundle ilişkilerini tek işlemde yeniliyorum.
    public async Task Handle(UpdateProductRelationsCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetWithRelationsForUpdateAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        if (request.BundleItems.Any(item => item.ProductId == product.Id))
        {
            throw new ConflictException("A bundle cannot include itself.");
        }

        var existingCollections = await _collections.GetExistingIdsAsync(request.CollectionIds, cancellationToken);
        if (existingCollections.Count != request.CollectionIds.Count)
        {
            throw new NotFoundException("One or more collections were not found.");
        }

        var existingTags = await _tags.GetExistingIdsAsync(request.TagIds, cancellationToken);
        if (existingTags.Count != request.TagIds.Count)
        {
            throw new NotFoundException("One or more tags were not found.");
        }

        var resolvedTags = request.Tags is { Count: > 0 }
            ? await _productTagResolver.ResolveAsync(request.Tags, cancellationToken)
            : ProductTagResolution.Empty;
        var includedIds = request.BundleItems.Select(item => item.ProductId).ToList();
        if ((await _products.GetByIdsAsync(includedIds, cancellationToken)).Count != includedIds.Count)
        {
            throw new NotFoundException("One or more included products were not found.");
        }

        product.ProductCollections.Clear();
        foreach (var collectionId in request.CollectionIds)
        {
            product.ProductCollections.Add(new ProductCollection(product.Id, collectionId));
        }

        var tagIds = request.TagIds
            .Concat(resolvedTags.GetIds(request.Tags))
            .ToHashSet();
        var removedProductTags = product.ProductTags
            .Where(productTag => !tagIds.Contains(productTag.TagId))
            .ToList();
        foreach (var productTag in removedProductTags)
        {
            product.ProductTags.Remove(productTag);
        }

        var currentTagIds = product.ProductTags
            .Select(productTag => productTag.TagId)
            .ToHashSet();
        foreach (var tagId in tagIds.Where(tagId => !currentTagIds.Contains(tagId)))
        {
            product.ProductTags.Add(new ProductTag(product.Id, tagId));
        }

        product.BundleItems.Clear();
        foreach (var item in request.BundleItems)
        {
            product.BundleItems.Add(new ProductBundleItem(product.Id, item.ProductId, item.Quantity));
        }

        product.MarkRelationsChanged();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
