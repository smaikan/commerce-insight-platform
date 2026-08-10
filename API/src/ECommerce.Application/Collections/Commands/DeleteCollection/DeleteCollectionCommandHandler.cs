using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Collections.Commands.DeleteCollection;

public sealed class DeleteCollectionCommandHandler : IRequestHandler<DeleteCollectionCommand>
{
    private readonly ICollectionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada koleksiyon silme akışının depo ve transaction bağımlılıklarını hazırlıyorum.
    public DeleteCollectionCommandHandler(ICollectionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    // Burada koleksiyonu silip yalnız ürün-koleksiyon ara bağlantılarının kaldırılmasını sağlıyorum.
    public async Task Handle(DeleteCollectionCommand request, CancellationToken cancellationToken)
    {
        var collection = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Collection was not found.");

        _repository.Remove(collection);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
