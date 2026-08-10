using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.ProductTypes.Commands.DeleteProductType;

public sealed class DeleteProductTypeCommandHandler : IRequestHandler<DeleteProductTypeCommand>
{
    private readonly IProductTypeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada ürün türü silme akışının depo ve transaction bağımlılıklarını hazırlıyorum.
    public DeleteProductTypeCommandHandler(IProductTypeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    // Burada ürün türünü silip bağlı ürünlerin tür referanslarının veritabanında null yapılmasını sağlıyorum.
    public async Task Handle(DeleteProductTypeCommand request, CancellationToken cancellationToken)
    {
        var productType = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product type was not found.");

        _repository.Remove(productType);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
