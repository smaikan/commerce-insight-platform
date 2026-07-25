using ECommerce.Application.Common.Models;
using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Queries.GetTaxRates;

// Burada vergi oranlarını sayfalama ve isteğe bağlı aktiflik filtresiyle okuma isteğini taşıyorum.
public sealed record GetTaxRatesQuery(
    int PageNumber = 1,
    int PageSize = 20,
    bool? IsActive = null) : IRequest<PagedResult<TaxRateDto>>;
