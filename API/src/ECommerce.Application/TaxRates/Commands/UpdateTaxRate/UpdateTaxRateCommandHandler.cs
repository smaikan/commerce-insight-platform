using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Commands.UpdateTaxRate;

public sealed class UpdateTaxRateCommandHandler : IRequestHandler<UpdateTaxRateCommand, TaxRateDto>
{
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada vergi oranı güncelleme use-case'i için gerekli kalıcılık bağımlılıklarını hazırlıyorum.
    public UpdateTaxRateCommandHandler(
        ITaxRateRepository taxRateRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _taxRateRepository = taxRateRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada hedef kaydı ve ad çakışmasını denetleyip vergi oranını güncelliyorum.
    public async Task<TaxRateDto> Handle(UpdateTaxRateCommand request, CancellationToken cancellationToken)
    {
        var taxRate = await _taxRateRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (taxRate is null)
        {
            throw new NotFoundException("Tax rate was not found.");
        }

        if (await _taxRateRepository.NameExistsAsync(request.Name, request.Id, cancellationToken))
        {
            throw new ConflictException("Tax rate name already exists.");
        }

        taxRate.Rename(request.Name);
        taxRate.ChangeRate(request.Rate);
        var products = await _productRepository.GetByTaxRateIdForUpdateAsync(taxRate.Id, cancellationToken);
        foreach (var product in products)
        {
            foreach (var variant in product.Variants)
            {
                variant.RecalculateNetPrice(taxRate);
            }
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return taxRate.ToDto();
    }
}
