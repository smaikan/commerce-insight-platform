using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using MediatR;

namespace ECommerce.Application.Products.Variants.Commands.DeleteProductVariant;

public sealed class DeleteProductVariantCommandHandler : IRequestHandler<DeleteProductVariantCommand>
{
    private readonly IProductVariantRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    // Burada varyant silme akışının ürün, varyant ve transaction bağımlılıklarını hazırlıyorum.
    public DeleteProductVariantCommandHandler(
        IProductVariantRepository repository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    // Burada son varyantı koruyup diğer varyantları stok hareketlerinden bağımsız olarak mantıksal siliyorum.
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
        variant.SoftDelete(_clock.UtcNow);
        product.MarkRelationsChanged();

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
