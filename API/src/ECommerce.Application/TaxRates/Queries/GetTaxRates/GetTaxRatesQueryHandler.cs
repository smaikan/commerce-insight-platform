using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Models;
using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Queries.GetTaxRates;

public sealed class GetTaxRatesQueryHandler : IRequestHandler<GetTaxRatesQuery, PagedResult<TaxRateDto>>
{
    private readonly ITaxRateRepository _taxRateRepository;

    // Burada vergi oranı listeleme use-case'i için repository bağımlılığını hazırlıyorum.
    public GetTaxRatesQueryHandler(ITaxRateRepository taxRateRepository)
    {
        _taxRateRepository = taxRateRepository;
    }

    // Burada vergi oranı sayfasını filtreyle okuyup DTO modeline dönüştürüyorum.
    public async Task<PagedResult<TaxRateDto>> Handle(GetTaxRatesQuery request, CancellationToken cancellationToken)
    {
        var taxRates = await _taxRateRepository.GetListAsync(
            request.PageNumber,
            request.PageSize,
            request.IsActive,
            cancellationToken);
        return taxRates.Map(taxRate => taxRate.ToDto());
    }
}
