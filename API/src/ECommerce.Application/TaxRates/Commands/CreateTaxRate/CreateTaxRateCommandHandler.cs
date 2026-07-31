using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.TaxRates.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.TaxRates.Commands.CreateTaxRate;

public sealed class CreateTaxRateCommandHandler : IRequestHandler<CreateTaxRateCommand, TaxRateDto>
{
    private readonly ITaxRateRepository _taxRateRepository;
    private readonly IUnitOfWork _unitOfWork;

    // Burada vergi oranı oluşturma use-case'i için gerekli kalıcılık bağımlılıklarını hazırlıyorum.
    public CreateTaxRateCommandHandler(ITaxRateRepository taxRateRepository, IUnitOfWork unitOfWork)
    {
        _taxRateRepository = taxRateRepository;
        _unitOfWork = unitOfWork;
    }

    // Burada ada göre çakışmayı denetleyip yeni vergi oranını kalıcı olarak oluşturuyorum.
    public async Task<TaxRateDto> Handle(CreateTaxRateCommand request, CancellationToken cancellationToken)
    {
        if (await _taxRateRepository.NameExistsAsync(request.Name, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Tax rate name already exists.");
        }

        var taxRate = new TaxRate(request.Name, request.Rate, request.IsActive);
        await _taxRateRepository.AddAsync(taxRate, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return taxRate.ToDto();
    }
}
