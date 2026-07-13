using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Tags.Dtos;
using ECommerce.Domain.Entities;
using MediatR;

namespace ECommerce.Application.Tags.Commands.BulkCreateTags;

public sealed class BulkCreateTagsCommandHandler : IRequestHandler<BulkCreateTagsCommand, IReadOnlyList<TagDto>>
{
    private readonly ITagRepository _tagRepository;
    private readonly IUrlGenerator _urlGenerator;
    private readonly IUnitOfWork _unitOfWork;

    public BulkCreateTagsCommandHandler(
        ITagRepository tagRepository,
        IUrlGenerator urlGenerator,
        IUnitOfWork unitOfWork)
    {
        _tagRepository = tagRepository;
        _urlGenerator = urlGenerator;
        _unitOfWork = unitOfWork;
    }

    // Burada etiketleri toplu şekilde oluşturmadan önce isim ve URL çakışmalarını kontrol ediyorum.
    public async Task<IReadOnlyList<TagDto>> Handle(BulkCreateTagsCommand request, CancellationToken cancellationToken)
    {
        var preparedItems = request.Tags
            .Select(item => new PreparedTagItem(
                item,
                string.IsNullOrWhiteSpace(item.Url) ? _urlGenerator.Generate(item.Name) : item.Url.Trim()))
            .ToList();

        EnsureNoDuplicateNames(preparedItems.Select(item => item.Item.Name));
        EnsureNoDuplicateUrls(preparedItems.Select(item => item.Url));

        var existingNames = await _tagRepository.GetExistingNamesAsync(
            preparedItems.Select(item => item.Item.Name),
            cancellationToken);
        var existingUrls = await _tagRepository.GetExistingUrlsAsync(
            preparedItems.Select(item => item.Url),
            cancellationToken);

        if (existingNames.Count > 0)
        {
            throw new ConflictException($"Tag name already exists: {string.Join(", ", existingNames)}.");
        }

        if (existingUrls.Count > 0)
        {
            throw new ConflictException($"Tag url already exists: {string.Join(", ", existingUrls)}.");
        }

        var tags = preparedItems
            .Select(item => new Tag(item.Item.Name, item.Url, item.Item.IsActive))
            .ToList();

        await _tagRepository.AddRangeAsync(tags, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return tags.Select(tag => tag.ToDto()).ToList();
    }

    private static void EnsureNoDuplicateNames(IEnumerable<string> names)
    {
        var duplicates = names
            .GroupBy(name => name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToList();

        if (duplicates.Count > 0)
        {
            throw new ConflictException($"Tag name is duplicated in the request: {string.Join(", ", duplicates)}.");
        }
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
            throw new ConflictException($"Tag url is duplicated in the request: {string.Join(", ", duplicates)}.");
        }
    }

    private sealed record PreparedTagItem(BulkCreateTagItem Item, string Url);
}
