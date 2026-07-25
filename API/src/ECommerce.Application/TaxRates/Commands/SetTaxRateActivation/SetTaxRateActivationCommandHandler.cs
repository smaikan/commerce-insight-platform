using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Commands.SetTaxRateActivation;

public sealed class SetTaxRateActivationCommandHandler : IRequestHandler<SetTaxRateActivationCommand, TaxRateDto>
{
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada vergi oranı aktiflik use-case'i için gerekli kalıcılık bağımlılıklarını hazırlıyorum.
    public SetTaxRateActivationCommandHandler(ITaxRateRepository taxRateRepository, IUnitOfWork unitOfWork)
    {
        _taxRateRepository = taxRateRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada bulunan vergi oranını geçmiş ürün bağlarını koruyarak istenen aktiflik durumuna getiriyorum.
    public async Task<TaxRateDto> Handle(SetTaxRateActivationCommand request, CancellationToken cancellationToken)
    {
        var taxRate = await _taxRateRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);
        if (taxRate is null)
        {
            throw new NotFoundException("Tax rate was not found.");
        }

        if (request.IsActive)
        {
            taxRate.Activate();
        }
        else
        {
            taxRate.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return taxRate.ToDto();
    }
}
