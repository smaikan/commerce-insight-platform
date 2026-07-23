using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Products.Images.Commands.DeleteProductImage;

public sealed class DeleteProductImageCommandHandler : IRequestHandler<DeleteProductImageCommand>
{
    private readonly IProductImageRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    public DeleteProductImageCommandHandler(IProductImageRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository; _unitOfWork = unitOfWork;
    }
    public async Task Handle(DeleteProductImageCommand request, CancellationToken cancellationToken)
    {
        var image = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product image was not found.");
        _repository.Remove(image);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
