using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using MediatR;

namespace ECommerce.Application.Brands.Commands.UpdateBrand;

public sealed class UpdateBrandCommandHandler : IRequestHandler<UpdateBrandCommand, BrandDto>
{
    private readonly IBrandRepository _brandRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateBrandCommandHandler(
        IBrandRepository brandRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada markayı güncellemeden önce kaydı ve URL çakışmasını kontrol ediyorum.
    public async Task<BrandDto> Handle(UpdateBrandCommand request, CancellationToken cancellationToken)
    {
        var brand = await _brandRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

        if (brand is null)
        {
            throw new NotFoundException("Brand was not found.");
        }

        var url = string.IsNullOrWhiteSpace(request.Url)
            ? _urlGenerator.Generate(request.Name)
            : request.Url.Trim();

        if (await _brandRepository.UrlExistsAsync(url, request.Id, cancellationToken))
        {
            throw new ConflictException("Brand url already exists.");
        }

        brand.Rename(request.Name);
        brand.ChangeUrl(url);
        brand.SetDescription(request.Description);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return brand.ToDto();
    }
}
