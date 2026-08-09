using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Products.Dtos;
using MediatR;

namespace ECommerce.Application.Products.Commands.SetProductHasVariants;

public sealed class SetProductHasVariantsCommandHandler : IRequestHandler<SetProductHasVariantsCommand, ProductDto>
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public SetProductHasVariantsCommandHandler(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(SetProductHasVariantsCommand request, CancellationToken cancellationToken)
    {
        var product = await _products.GetByIdForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Product was not found.");
        product.SetHasVariants(request.HasVariants);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (await _products.GetByIdAsync(product.Id, cancellationToken))?.ToDto() ?? product.ToDto();
    }
}
