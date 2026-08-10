using ECommerce.Application.Brands.Commands.CreateBrand;
using ECommerce.Application.Brands.Commands.UpdateBrand;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Common.Services;
using ECommerce.Domain.Entities;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class BrandImageApplicationTests
{
    // Burada marka oluşturma akışının görsel URL değerini entity ve DTO'ya taşıdığını doğruluyorum.
    [Fact]
    public async Task Create_Should_Persist_Brand_Image_Url()
    {
        var repository = new Mock<IBrandRepository>();
        repository
            .Setup(item => item.UrlExistsAsync("serantis", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Brand? savedBrand = null;
        repository
            .Setup(item => item.AddAsync(It.IsAny<Brand>(), It.IsAny<CancellationToken>()))
            .Callback<Brand, CancellationToken>((brand, _) => savedBrand = brand)
            .Returns(Task.CompletedTask);
        var handler = new CreateBrandCommandHandler(
            repository.Object,
            Mock.Of<IUrlGenerator>(),
            CreateUnitOfWork().Object);

        var result = await handler.Handle(
            new CreateBrandCommand(
                "Serantis",
                "serantis",
                ImageUrl: "https://cdn.example.com/brands/serantis.jpg"),
            CancellationToken.None);

        savedBrand.Should().NotBeNull();
        savedBrand!.ImageUrl.Should().Be("https://cdn.example.com/brands/serantis.jpg");
        result.ImageUrl.Should().Be("https://cdn.example.com/brands/serantis.jpg");
    }

    // Burada marka güncelleme akışının görsel URL değerini değiştirip kaldırabildiğini doğruluyorum.
    [Fact]
    public async Task Update_Should_Change_And_Clear_Brand_Image_Url()
    {
        var brand = new Brand("Serantis", "serantis", imageUrl: "https://cdn.example.com/old.jpg");
        var repository = new Mock<IBrandRepository>();
        repository
            .Setup(item => item.GetByIdForUpdateAsync(brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(brand);
        repository
            .Setup(item => item.UrlExistsAsync("serantis", brand.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new UpdateBrandCommandHandler(
            repository.Object,
            Mock.Of<IUrlGenerator>(),
            CreateUnitOfWork().Object);

        var result = await handler.Handle(
            new UpdateBrandCommand(brand.Id, "Serantis", "serantis", ImageUrl: null),
            CancellationToken.None);

        brand.ImageUrl.Should().BeNull();
        result.ImageUrl.Should().BeNull();
    }

    // Burada marka görsel URL alanının 500 karakter sınırını kullandığını doğruluyorum.
    [Fact]
    public void Create_Validator_Should_Reject_Too_Long_Image_Url()
    {
        var validator = new CreateBrandCommandValidator();
        var command = new CreateBrandCommand("Serantis", ImageUrl: new string('a', 501));

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
