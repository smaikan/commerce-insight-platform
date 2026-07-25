using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.ShippingMethods.Dtos;
using MediatR;

namespace ECommerce.Application.ShippingMethods.Queries.GetShippingMethods;

public sealed class GetShippingMethodsQueryHandler : IRequestHandler<GetShippingMethodsQuery, PagedResult<ShippingMethodDto>>
{
    private readonly IShippingMethodRepository _shippingMethodRepository;

    // Burada kargo yöntemi listeleme use-case'i için repository bağımlılığını hazırlıyorum.
    public GetShippingMethodsQueryHandler(IShippingMethodRepository shippingMethodRepository)
    {
        _shippingMethodRepository = shippingMethodRepository;
    }

    // Burada kargo yöntemi sayfasını filtreyle okuyup DTO modeline dönüştürüyorum.
    public async Task<PagedResult<ShippingMethodDto>> Handle(
        GetShippingMethodsQuery request,
        CancellationToken cancellationToken)
    {
        var shippingMethods = await _shippingMethodRepository.GetListAsync(
            request.PageNumber,
            request.PageSize,
            request.IsActive,
            cancellationToken);
        return shippingMethods.Map(shippingMethod => shippingMethod.ToDto());
    }
}
