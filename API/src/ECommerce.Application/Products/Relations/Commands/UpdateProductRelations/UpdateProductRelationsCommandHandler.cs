using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Products.Relations.Commands.UpdateProductRelations;

public sealed class UpdateProductRelationsCommandHandler : IRequestHandler<UpdateProductRelationsCommand>
{
    private readonly IProductRepository _products;
    private readonly ICollectionRepository _collections;
    private readonly ITagRepository _tags;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductRelationsCommandHandler(IProductRepository products, ICollectionRepository collections,
        ITagRepository tags, IUnitOfWork unitOfWork)
    {
        _products = products; _collections = collections; _tags = tags; _unitOfWork = unitOfWork;
    }

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
        product.ProductTags.Clear();
        foreach (var tagId in request.TagIds)
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
