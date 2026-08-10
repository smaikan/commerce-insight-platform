using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Brands.Commands.DeleteBrand;

public sealed class DeleteBrandCommandHandler : IRequestHandler<DeleteBrandCommand>
{
    private readonly IBrandRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada marka silme akışının depo ve transaction bağımlılıklarını hazırlıyorum.
    public DeleteBrandCommandHandler(IBrandRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    // Burada markayı silip bağlı ürünlerin marka referanslarının veritabanında null yapılmasını sağlıyorum.
    public async Task Handle(DeleteBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Brand was not found.");

        _repository.Remove(brand);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
