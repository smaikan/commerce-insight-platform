using ECommerce.Application.TaxRates.Dtos;
using MediatR;

namespace ECommerce.Application.TaxRates.Commands.CreateTaxRate;

// Burada yöneticinin yeni vergi oranı oluşturma isteğini taşıyorum.
public sealed record CreateTaxRateCommand(
    string Name,
    decimal Rate,
    bool IsActive = true) : IRequest<TaxRateDto>;
