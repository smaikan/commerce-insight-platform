using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Commands.UpdateTaxRate;

// Burada yöneticinin mevcut vergi oranını güncelleme isteğini taşıyorum.
public sealed record UpdateTaxRateCommand(
    Guid Id,
    string Name,
    decimal Rate) : IRequest<TaxRateDto>;
