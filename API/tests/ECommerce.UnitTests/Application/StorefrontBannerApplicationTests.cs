using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StorefrontBanners.Commands.UpdateStorefrontBanners;
using ECommerce.Application.StorefrontBanners.Queries.GetStorefrontBanners;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class StorefrontBannerApplicationTests
{
    // Burada beşten fazla alt banner URL değerinin doğrulamadan geçmediğini kontrol ediyorum.
    [Fact]
    public void Validator_Should_Reject_More_Than_Five_Alternate_Banners()
    {
        var validator = new UpdateStorefrontBannersCommandValidator();
        var command = new UpdateStorefrontBannersCommand(
            "https://cdn.example.com/main.jpg",
            Enumerable.Range(1, 6).Select(index => $"https://cdn.example.com/alt-{index}.jpg").ToList());

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item.AltBannerImageUrls);
    }

    // Burada ana ve beş alt bannerın sabit alan sırasıyla tek değişiklik setinde kaydedildiğini doğruluyorum.
    [Fact]
    public async Task Update_Should_Replace_Complete_Banner_Set_And_Save_Once()
    {
        var repository = new Mock<IStorefrontBannerRepository>();
        var unitOfWork = CreateUnitOfWork();
        IReadOnlyCollection<StorefrontBanner>? savedBanners = null;
        repository
            .Setup(item => item.ReplaceAsync(It.IsAny<IReadOnlyCollection<StorefrontBanner>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyCollection<StorefrontBanner>, CancellationToken>((banners, _) => savedBanners = banners)
            .Returns(Task.CompletedTask);
        var handler = new UpdateStorefrontBannersCommandHandler(repository.Object, unitOfWork.Object);
        var alternateUrls = Enumerable.Range(1, 5)
            .Select(index => $"https://cdn.example.com/alt-{index}.jpg")
            .ToList();

        var result = await handler.Handle(
            new UpdateStorefrontBannersCommand("https://cdn.example.com/main.jpg", alternateUrls),
            CancellationToken.None);

        savedBanners.Should().HaveCount(6);
        savedBanners!.Select(item => item.Slot).Should().Equal(
            StorefrontBannerSlot.Main,
            StorefrontBannerSlot.Alternate1,
            StorefrontBannerSlot.Alternate2,
            StorefrontBannerSlot.Alternate3,
            StorefrontBannerSlot.Alternate4,
            StorefrontBannerSlot.Alternate5);
        result.MainBannerImageUrl.Should().Be("https://cdn.example.com/main.jpg");
        result.AltBannerImageUrls.Should().Equal(alternateUrls);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada boş veri tabanının storefront için null ana banner ve boş alt banner listesi döndürdüğünü doğruluyorum.
    [Fact]
    public async Task Get_Should_Return_Empty_Banner_Contract_When_No_Rows_Exist()
    {
        var repository = new Mock<IStorefrontBannerRepository>();
        repository
            .Setup(item => item.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var handler = new GetStorefrontBannersQueryHandler(repository.Object);

        var result = await handler.Handle(new GetStorefrontBannersQuery(), CancellationToken.None);

        result.MainBannerImageUrl.Should().BeNull();
        result.AltBannerImageUrls.Should().BeEmpty();
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
