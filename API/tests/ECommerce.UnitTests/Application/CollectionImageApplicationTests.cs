using ECommerce.Application.Collections.Commands.CreateCollection;
using ECommerce.Application.Collections.Commands.UpdateCollection;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Domain.Entities;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class CollectionImageApplicationTests
{
    // Burada koleksiyon oluşturma akışının görsel URL değerini entity ve DTO'ya taşıdığını doğruluyorum.
    [Fact]
    public async Task Create_Should_Persist_Collection_Image_Url()
    {
        var repository = new Mock<ICollectionRepository>();
        var unitOfWork = CreateUnitOfWork();
        repository
            .Setup(item => item.UrlExistsAsync("yaz", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Collection? savedCollection = null;
        repository
            .Setup(item => item.AddAsync(It.IsAny<Collection>(), It.IsAny<CancellationToken>()))
            .Callback<Collection, CancellationToken>((collection, _) => savedCollection = collection)
            .Returns(Task.CompletedTask);
        var handler = new CreateCollectionCommandHandler(
            repository.Object,
            Mock.Of<IUrlGenerator>(),
            unitOfWork.Object);

        var result = await handler.Handle(
            new CreateCollectionCommand(
                "Yaz",
                "yaz",
                ImageUrl: "https://cdn.example.com/collections/yaz.jpg"),
            CancellationToken.None);

        savedCollection.Should().NotBeNull();
        savedCollection!.ImageUrl.Should().Be("https://cdn.example.com/collections/yaz.jpg");
        result.ImageUrl.Should().Be("https://cdn.example.com/collections/yaz.jpg");
    }

    // Burada koleksiyon güncelleme akışının görsel URL değerini değiştirdiğini doğruluyorum.
    [Fact]
    public async Task Update_Should_Change_Collection_Image_Url()
    {
        var collection = new Collection("Yaz", "yaz");
        var repository = new Mock<ICollectionRepository>();
        repository
            .Setup(item => item.GetByIdForUpdateAsync(collection.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(collection);
        repository
            .Setup(item => item.UrlExistsAsync("yaz", collection.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new UpdateCollectionCommandHandler(
            repository.Object,
            Mock.Of<IUrlGenerator>(),
            CreateUnitOfWork().Object);

        var result = await handler.Handle(
            new UpdateCollectionCommand(
                collection.Id,
                "Yaz",
                "yaz",
                ImageUrl: "https://cdn.example.com/collections/yaz-new.jpg"),
            CancellationToken.None);

        collection.ImageUrl.Should().Be("https://cdn.example.com/collections/yaz-new.jpg");
        result.ImageUrl.Should().Be("https://cdn.example.com/collections/yaz-new.jpg");
    }

    // Burada koleksiyon görsel URL alanının ürün görseliyle aynı 500 karakter sınırını kullandığını doğruluyorum.
    [Fact]
    public void Create_Validator_Should_Reject_Too_Long_Image_Url()
    {
        var validator = new CreateCollectionCommandValidator();
        var command = new CreateCollectionCommand("Yaz", ImageUrl: new string('a', 501));

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.ImageUrl);
    }

    // Burada başarılı kaydı taklit eden Unit of Work bağımlılığını hazırlıyorum.
    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }
}
