using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Security;
using MediatR;

namespace ECommerce.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _clock;

    // Burada ürün silme akışının depo ve transaction bağımlılıklarını hazırlıyorum.
    public DeleteProductCommandHandler(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider clock)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    // Burada ürünü operasyonel geçmişinden bağımsız ve idempotent biçimde soft delete yapıyorum.
    public async Task Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdForDeletionAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");

        if (!product.SoftDelete(_clock.UtcNow))
        {
            return;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
