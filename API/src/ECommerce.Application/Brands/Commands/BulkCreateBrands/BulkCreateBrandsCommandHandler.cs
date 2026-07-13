using ECommerce.Application.Brands.Dtos;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Brands.Commands.BulkCreateBrands;

public sealed class BulkCreateBrandsCommandHandler : IRequestHandler<BulkCreateBrandsCommand, IReadOnlyList<BrandDto>>
{
    private readonly IBrandRepository _brandRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public BulkCreateBrandsCommandHandler(
        IBrandRepository brandRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _brandRepository = brandRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada markaları toplu şekilde oluşturmadan önce URL çakışmalarını kontrol ediyorum.
    public async Task<IReadOnlyList<BrandDto>> Handle(BulkCreateBrandsCommand request, CancellationToken cancellationToken)
    {
        var preparedItems = request.Brands
            .Select(item => new PreparedBrandItem(
                item,
                string.IsNullOrWhiteSpace(item.Url) ? _urlGenerator.Generate(item.Name) : item.Url.Trim()))
            .ToList();

        EnsureNoDuplicateUrls(preparedItems.Select(item => item.Url));

        var existingUrls = await _brandRepository.GetExistingUrlsAsync(
            preparedItems.Select(item => item.Url),
            cancellationToken);

        if (existingUrls.Count > 0)
        {
            throw new ConflictException($"Brand url already exists: {string.Join(", ", existingUrls)}.");
        }

        var brands = preparedItems
            .Select(item => new Brand(item.Item.Name, item.Url, item.Item.Description, item.Item.IsActive))
            .ToList();

        await _brandRepository.AddRangeAsync(brands, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return brands.Select(brand => brand.ToDto()).ToList();
    }

    private static void EnsureNoDuplicateUrls(IEnumerable<string> urls)
    {
        var duplicates = urls
            .GroupBy(url => url.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ConflictException($"Brand url is duplicated in the request: {string.Join(", ", duplicates)}.");
        }
    }

    private sealed record PreparedBrandItem(BulkCreateBrandItem Item, string Url);
}
