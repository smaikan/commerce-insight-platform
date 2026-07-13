using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using MediatR;

namespace ECommerce.Application.Brands.Commands.SetBrandActivation;

public sealed class SetBrandActivationCommandHandler : IRequestHandler<SetBrandActivationCommand, BrandDto>
{
    private readonly IBrandRepository _brandRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetBrandActivationCommandHandler(IBrandRepository brandRepository, IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada markanın aktiflik durumunu değiştiriyorum.
    public async Task<BrandDto> Handle(SetBrandActivationCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (brand is null)
        {
            throw new NotFoundException("Brand was not found.");
        }

        if (request.IsActive)
        {
            brand.Activate();
        }
        else
        {
            brand.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return brand.ToDto();
    }
}
