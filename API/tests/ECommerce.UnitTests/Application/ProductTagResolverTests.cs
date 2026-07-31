using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class ProductTagResolverTests
{
    // Burada farklı yazım biçimiyle gelen mevcut etiketin yeniden kullanıldığını doğruluyorum.
    [Fact]
    public async Task ResolveAsync_Should_Reuse_Existing_Tag_By_Name()
    {
        var existingTag = new Tag("Summer", "custom-summer");
        var tagRepository = new Mock<ITagRepository>();
        tagRepository
            .Setup(repository => repository.GetByNamesOrUrlsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingTag]);
        var resolver = new ProductTagResolver(tagRepository.Object, new ProductUrlGenerator());

        var resolution = await resolver.ResolveAsync([" summer ", "SUMMER"]);

        resolution.GetIds(["summer", " SUMMER "]).Should().ContainSingle()
            .Which.Should().Be(existingTag.Id);
        tagRepository.Verify(
            repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Tag>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Burada aynı istekte tekrarlanan yeni etiket adından yalnızca bir kayıt hazırlandığını doğruluyorum.
    [Fact]
    public async Task ResolveAsync_Should_Create_Missing_Tag_Only_Once()
    {
        var tagRepository = new Mock<ITagRepository>();
        tagRepository
            .Setup(repository => repository.GetByNamesOrUrlsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        IReadOnlyCollection<Tag>? addedTags = null;
        tagRepository
            .Setup(repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Tag>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Tag>, CancellationToken>((tags, _) => addedTags = tags)
            .Returns(Task.CompletedTask);
        var resolver = new ProductTagResolver(tagRepository.Object, new ProductUrlGenerator());

        var resolution = await resolver.ResolveAsync(["New Season", " new season "]);

        addedTags.Should().ContainSingle();
        addedTags!.Single().Name.Should().Be("New Season");
        addedTags.Single().Url.Should().Be("new-season");
        resolution.GetIds(["NEW SEASON"]).Should().ContainSingle()
            .Which.Should().Be(addedTags.Single().Id);
    }

    // Burada farklı adların aynı slugı üretmesi durumunda etiketlerin yanlışlıkla birleştirilmediğini doğruluyorum.
    [Fact]
    public async Task ResolveAsync_Should_Create_Collision_Safe_Url_For_Different_Name()
    {
        var existingTag = new Tag("C Sharp", "c");
        var tagRepository = new Mock<ITagRepository>();
        tagRepository
            .Setup(repository => repository.GetByNamesOrUrlsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingTag]);
        IReadOnlyCollection<Tag>? addedTags = null;
        tagRepository
            .Setup(repository => repository.AddRangeAsync(
                It.IsAny<IReadOnlyCollection<Tag>>(),
                It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<Tag>, CancellationToken>((tags, _) => addedTags = tags)
            .Returns(Task.CompletedTask);
        var resolver = new ProductTagResolver(tagRepository.Object, new ProductUrlGenerator());

        var resolution = await resolver.ResolveAsync(["C++"]);

        addedTags.Should().ContainSingle();
        addedTags!.Single().Name.Should().Be("C++");
        addedTags.Single().Url.Should().StartWith("c-");
        addedTags.Single().Url.Should().NotBe(existingTag.Url);
        resolution.GetIds(["C++"]).Should().ContainSingle()
            .Which.Should().Be(addedTags.Single().Id);
    }
}
