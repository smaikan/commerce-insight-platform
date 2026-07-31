using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Commands.SetShippingMethodActivation;

public sealed class SetShippingMethodActivationCommandHandler : IRequestHandler<SetShippingMethodActivationCommand, ShippingMethodDto>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada kargo yöntemi aktiflik use-case'i için gerekli kalıcılık bağımlılıklarını hazırlıyorum.
    public SetShippingMethodActivationCommandHandler(
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada bulunan kargo yöntemini geçmiş sipariş bağlarını koruyarak istenen aktiflik durumuna getiriyorum.
    public async Task<ShippingMethodDto> Handle(
        SetShippingMethodActivationCommand request,
        CancellationToken cancellationToken)
    {
        var shippingMethod = await _shippingMethodRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (shippingMethod is null)
        {
            throw new NotFoundException("Shipping method was not found.");
        }

        if (request.IsActive)
        {
            shippingMethod.Activate();
        }
        else
        {
            shippingMethod.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return shippingMethod.ToDto();
    }
}
