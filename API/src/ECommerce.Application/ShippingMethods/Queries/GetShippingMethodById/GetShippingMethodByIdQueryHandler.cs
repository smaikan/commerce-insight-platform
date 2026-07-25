using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Queries.GetShippingMethodById;

public sealed class GetShippingMethodByIdQueryHandler : IRequestHandler<GetShippingMethodByIdQuery, ShippingMethodDto>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;

    // Burada tekil kargo yöntemi sorgusu için repository bağımlılığını hazırlıyorum.
    public GetShippingMethodByIdQueryHandler(IShippingMethodRepository shippingMethodRepository)
    {
        _shippingMethodRepository = shippingMethodRepository;
    }

    // Burada istenen kargo yöntemini bulup DTO olarak döndürüyorum.
    public async Task<ShippingMethodDto> Handle(
        GetShippingMethodByIdQuery request,
        CancellationToken cancellationToken)
    {
        var shippingMethod = await _shippingMethodRepository.GetByIdAsync(request.Id, cancellationToken);
        if (shippingMethod is null)
        {
            throw new NotFoundException("Shipping method was not found.");
        }

        return shippingMethod.ToDto();
    }
}
