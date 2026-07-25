using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Queries.GetTaxRateById;

// Burada tek vergi oranını kimliğiyle okuma isteğini taşıyorum.
public sealed record GetTaxRateByIdQuery(Guid Id) : IRequest<TaxRateDto>;
