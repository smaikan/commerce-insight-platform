using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Application.Products.Relations.Commands.UpdateProductRelations;
using ECommerce.Domain.Entities;
using ECommerce.UnitTests.Testing;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class UpdateProductRelationsCommandHandlerTests
{
    // Burada mevcut ilişkiyi koruyup kimlik ve adla çözümlenen etiketleri birlikte ürüne bağlıyorum.
    [Fact]
    public async Task Handle_Should_Use_Existing_And_Auto_Resolved_Tags_Together()
    {
        var product = new Product("Product", "product", "PRODUCT-MAIN").WithId(1);
        var existingTagId = Guid.NewGuid();
        var dynamicTagId = Guid.NewGuid();
        var existingProductTag = new ProductTag(product.Id, existingTagId);
        product.ProductTags.Add(existingProductTag);
        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(repository => repository.GetWithRelationsForUpdateAsync(
                product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);
        productRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var collectionRepository = new Mock<ICollectionRepository>();
        collectionRepository
            .Setup(repository => repository.GetExistingIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid>());
        var tagRepository = new Mock<ITagRepository>();
        tagRepository
            .Setup(repository => repository.GetExistingIdsAsync(
                It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HashSet<Guid> { existingTagId });
        var tagResolver = new Mock<IProductTagResolver>();
        tagResolver
            .Setup(resolver => resolver.ResolveAsync(
                It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProductTagResolution(
                new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase)
                {
                    ["New Season"] = dynamicTagId
                }));
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var handler = new UpdateProductRelationsCommandHandler(
            productRepository.Object,
            collectionRepository.Object,
            tagRepository.Object,
            tagResolver.Object,
            unitOfWork.Object);

        await handler.Handle(
            new UpdateProductRelationsCommand(
                product.Id,
                [],
                [existingTagId],
                [],
                ["New Season"]),
            CancellationToken.None);

        product.ProductTags.Select(tag => tag.TagId)
            .Should().BeEquivalentTo([existingTagId, dynamicTagId]);
        product.ProductTags.Should().ContainSingle(tag => ReferenceEquals(tag, existingProductTag));
        unitOfWork.Verify(
            unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // Burada boş etiket adının ilişki isteği doğrulamasından geçmediğini kontrol ediyorum.
    [Fact]
    public void Validator_Should_Reject_Empty_Tag_Name()
    {
        var result = new UpdateProductRelationsCommandValidator().Validate(
            new UpdateProductRelationsCommand(1, [], [], [], [" "]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName.Contains(nameof(UpdateProductRelationsCommand.Tags)));
    }
}
