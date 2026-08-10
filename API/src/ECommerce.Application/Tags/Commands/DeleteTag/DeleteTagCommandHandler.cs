using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Tags.Commands.DeleteTag;

public sealed class DeleteTagCommandHandler : IRequestHandler<DeleteTagCommand>
{
    private readonly ITagRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada etiket silme akışının depo ve transaction bağımlılıklarını hazırlıyorum.
    public DeleteTagCommandHandler(ITagRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    // Burada etiketi silip yalnız ürün-etiket ara bağlantılarının kaldırılmasını sağlıyorum.
    public async Task Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await _repository.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Tag was not found.");

        _repository.Remove(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
