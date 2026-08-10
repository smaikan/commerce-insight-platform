using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StorefrontBanners.Commands.ReplaceBannerSection;
using ECommerce.Application.StorefrontBanners.Dtos;
using ECommerce.Application.StorefrontBanners.Queries.GetBannerSection;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class StorefrontBannerApplicationTests
{
    // Burada bir bölümde beşten fazla banner kaydının doğrulamadan geçmediğini kontrol ediyorum.
    [Fact]
    public void Validator_Should_Reject_More_Than_Five_Items()
    {
        var validator = new ReplaceBannerSectionCommandValidator();
        var items = Enumerable.Range(1, 6).Select(index => CreateInput($"banner-{index}", index)).ToList();

        var result = validator.TestValidate(new ReplaceBannerSectionCommand(StorefrontBannerSection.AltBanner1, items));

        result.ShouldHaveValidationErrorFor(command => command.Items);
    }

    // Burada dolu main bölümünde tam olarak bir aktif ana kayıt bulunmasını zorunlu tutuyorum.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void Validator_Should_Reject_Invalid_Main_Selection(bool firstIsMain, bool secondIsMain)
    {
        var validator = new ReplaceBannerSectionCommandValidator();
        var command = new ReplaceBannerSectionCommand(StorefrontBannerSection.Main, [
            CreateInput("first", 0, isMain: firstIsMain),
            CreateInput("second", 1, isMain: secondIsMain)
        ]);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item);
    }

    // Burada alt banner bölümlerinde isMain seçiminin reddedildiğini doğruluyorum.
    [Fact]
    public void Validator_Should_Reject_Main_Selection_In_Alternate_Section()
    {
        var validator = new ReplaceBannerSectionCommandValidator();
        var command = new ReplaceBannerSectionCommand(
            StorefrontBannerSection.AltBanner2,
            [CreateInput("alternate", 0, isMain: true)]);

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(item => item);
    }

    // Burada seçilen main kaydının ilk sıraya taşınıp bölümün tek transaction içinde kaydedildiğini doğruluyorum.
    [Fact]
    public async Task Replace_Should_Normalize_Main_Order_And_Save_Atomically()
    {
        var repository = new Mock<IStorefrontBannerRepository>();
        var unitOfWork = CreateUnitOfWork();
        IReadOnlyCollection<StorefrontBanner> savedBanners = [];
        repository
            .Setup(item => item.ReplaceSectionAsync(
                StorefrontBannerSection.Main,
                It.IsAny<IReadOnlyCollection<StorefrontBanner>>(),
                It.IsAny<CancellationToken>()))
            .Callback<StorefrontBannerSection, IReadOnlyCollection<StorefrontBanner>, CancellationToken>(
                (_, banners, _) => savedBanners = banners)
            .Returns(Task.CompletedTask);
        repository
            .Setup(item => item.GetSectionAsync(
                StorefrontBannerSection.Main,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => savedBanners.ToList());
        var handler = new ReplaceBannerSectionCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(new ReplaceBannerSectionCommand(StorefrontBannerSection.Main, [
            CreateInput("first", 0),
            CreateInput("selected", 5, isMain: true),
            CreateInput("second", 2)
        ]), CancellationToken.None);

        result.Items.Select(item => item.Key).Should().Equal("selected", "first", "second");
        result.Items.Select(item => item.DisplayOrder).Should().Equal(0, 1, 2);
        result.Items[0].IsMain.Should().BeTrue();
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        unitOfWork.Verify(item => item.ExecuteInSerializableTransactionAsync(
            It.IsAny<Func<CancellationToken, Task<BannerSectionDto>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada boş bölüm okumasının bölüm name/key bilgisiyle boş öğe listesi döndürdüğünü doğruluyorum.
    [Fact]
    public async Task Get_Should_Return_Empty_Section_Contract_When_No_Rows_Exist()
    {
        var repository = new Mock<IStorefrontBannerRepository>();
        repository
            .Setup(item => item.GetSectionAsync(StorefrontBannerSection.AltBanner5, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        var handler = new GetBannerSectionQueryHandler(repository.Object);

        var result = await handler.Handle(
            new GetBannerSectionQuery(StorefrontBannerSection.AltBanner5, ActiveOnly: true),
            CancellationToken.None);

        result.Name.Should().Be("Alt Banner 5");
        result.Key.Should().Be("alt-banner-5");
        result.Items.Should().BeEmpty();
    }

    // Burada application testleri için geçerli banner giriş kaydı hazırlıyorum.
    private static BannerItemInput CreateInput(string key, int displayOrder, bool isMain = false) =>
        new(
            $"Banner {key}",
            key,
            $"https://cdn.example.com/{key}.jpg",
            BannerMediaType.Image,
            "/collections/summer",
            $"Banner {key}",
            displayOrder,
            true,
            isMain);

    // Burada serializable callback'i çalıştırıp başarılı kaydı taklit eden Unit of Work bağımlılığını hazırlıyorum.
    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        unitOfWork
            .Setup(item => item.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<BannerSectionDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns((Func<CancellationToken, Task<BannerSectionDto>> operation, CancellationToken cancellationToken) =>
                operation(cancellationToken));
        return unitOfWork;
    }
}
