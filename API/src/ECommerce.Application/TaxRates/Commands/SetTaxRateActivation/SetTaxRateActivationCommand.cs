using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Commands.SetTaxRateActivation;

// Burada yöneticinin vergi oranını yeni seçimlere açma veya kapatma isteğini taşıyorum.
public sealed record SetTaxRateActivationCommand(Guid Id, bool IsActive) : IRequest<TaxRateDto>;
