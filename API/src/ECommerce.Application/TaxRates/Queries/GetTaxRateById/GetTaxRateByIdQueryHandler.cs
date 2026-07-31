using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Queries.GetTaxRateById;

public sealed class GetTaxRateByIdQueryHandler : IRequestHandler<GetTaxRateByIdQuery, TaxRateDto>
{
    private readonly ITaxRateRepository _taxRateRepository;

    // Burada tekil vergi oranı sorgusu için repository bağımlılığını hazırlıyorum.
    public GetTaxRateByIdQueryHandler(ITaxRateRepository taxRateRepository)
    {
        _taxRateRepository = taxRateRepository;
    }

    // Burada istenen vergi oranını bulup DTO olarak döndürüyorum.
    public async Task<TaxRateDto> Handle(GetTaxRateByIdQuery request, CancellationToken cancellationToken)
    {
        var taxRate = await _taxRateRepository.GetByIdAsync(request.Id, cancellationToken);
        if (taxRate is null)
        {
            throw new NotFoundException("Tax rate was not found.");
        }

        return taxRate.ToDto();
    }
}
