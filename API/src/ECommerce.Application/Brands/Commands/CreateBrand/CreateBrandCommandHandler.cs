using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Brands.Commands.CreateBrand;

public sealed class CreateBrandCommandHandler : IRequestHandler<CreateBrandCommand, BrandDto>
{
    private readonly IBrandRepository _brandRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    // Burada marka oluşturma bağımlılıklarını hazırlıyorum.
    public CreateBrandCommandHandler(
        IBrandRepository brandRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada yeni markayı oluştururken URL değerini hazır hale getiriyorum.
    public async Task<BrandDto> Handle(CreateBrandCommand request, CancellationToken cancellationToken)
    {
        var url = string.IsNullOrWhiteSpace(request.Url)
            ? _urlGenerator.Generate(request.Name)
            : request.Url.Trim();

        if (await _brandRepository.UrlExistsAsync(url, cancellationToken: cancellationToken))
        {
            throw new ConflictException("Brand url already exists.");
        }

        var brand = new Brand(request.Name, url, request.Description, request.IsActive, request.ImageUrl);

        await _brandRepository.AddAsync(brand, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return brand.ToDto();
    }
}
