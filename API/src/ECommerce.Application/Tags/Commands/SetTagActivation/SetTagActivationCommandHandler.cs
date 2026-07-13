using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Tags.Dtos;
using MediatR;

namespace ECommerce.Application.Tags.Commands.SetTagActivation;

public sealed class SetTagActivationCommandHandler : IRequestHandler<SetTagActivationCommand, TagDto>
{
    private readonly ITagRepository _tagRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetTagActivationCommandHandler(ITagRepository tagRepository, IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada etiketin aktiflik durumunu değiştiriyorum.
    public async Task<TagDto> Handle(SetTagActivationCommand request, CancellationToken cancellationToken)
    {
        var tag = await _tagRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (tag is null)
        {
            throw new NotFoundException("Tag was not found.");
        }

        if (request.IsActive)
        {
            tag.Activate();
        }
        else
        {
            tag.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tag.ToDto();
    }
}
