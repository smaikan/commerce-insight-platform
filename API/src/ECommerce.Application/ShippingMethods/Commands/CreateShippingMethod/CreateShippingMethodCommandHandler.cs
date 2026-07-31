using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ShippingMethods.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Commands.CreateShippingMethod;

public sealed class CreateShippingMethodCommandHandler : IRequestHandler<CreateShippingMethodCommand, ShippingMethodDto>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada kargo yöntemi oluşturma use-case'i için gerekli kalıcılık bağımlılıklarını hazırlıyorum.
    public CreateShippingMethodCommandHandler(
        IShippingMethodRepository shippingMethodRepository,
        IUnitOfWork unitOfWork)
    {
        _shippingMethodRepository = shippingMethodRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ada göre çakışmayı denetleyip yeni kargo yöntemini kalıcı olarak oluşturuyorum.
    public async Task<ShippingMethodDto> Handle(CreateShippingMethodCommand request, CancellationToken cancellationToken)
    {
        if (await _shippingMethodRepository.NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Shipping method name already exists.");
        }

        var shippingMethod = new ShippingMethod(
            request.Name,
            request.FixedFee,
            request.IsActive,
            request.DisplayOrder);
        await _shippingMethodRepository.AddAsync(shippingMethod, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return shippingMethod.ToDto();
    }
}
