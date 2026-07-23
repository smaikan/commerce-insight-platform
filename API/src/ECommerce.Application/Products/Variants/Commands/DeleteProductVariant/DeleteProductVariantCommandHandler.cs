using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.DeleteProductVariant;

public sealed class DeleteProductVariantCommandHandler : IRequestHandler<DeleteProductVariantCommand>
{
    private readonly IProductVariantRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteProductVariantCommandHandler(
        IProductVariantRepository repository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }
    public async Task Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product variant was not found.");

        if (await _repository.CountByProductIdAsync(variant.ProductId, cancellationToken) <= 1)
        {
            throw new ConflictException("A product must have at least one variant.");
        }

        var product = await _productRepository.GetByIdForUpdateAsync(variant.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        product.MarkRelationsChanged();

        _repository.Remove(variant);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
