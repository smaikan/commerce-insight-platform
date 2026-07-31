using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.Coupons.Commands.CreateCoupon;
using ECommerce.Application.Coupons.Commands.UpdateCoupon;
using ECommerce.Application.Coupons.Queries.GetCoupons;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class CouponApplicationTests
{
    // Burada yeni kuponun benzersiz kodla eklenip tek kayÄ±t iÅŸlemiyle kalÄ±cÄ±laÅŸtÄ±rÄ±ldÄ±ÄŸÄ±nÄ± doÄŸruluyorum.
    [Fact]
    public async Task Create_Should_Add_And_Save_Normalized_Coupon()
    {
        var repository = new Mock<ICouponRepository>();
        var unitOfWork = CreateUnitOfWork();
        Coupon? savedCoupon = null;
        repository.Setup(item => item.CodeExistsAsync("summer10", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository.Setup(item => item.AddAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
            .Callback<Coupon, CancellationToken>((coupon, _) => savedCoupon = coupon)
            .Returns(Task.CompletedTask);
        var handler = new CreateCouponCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new CreateCouponCommand("summer10", CouponDiscountType.Percentage, 10m),
            CancellationToken.None);

        result.Code.Should().Be("SUMMER10");
        savedCoupon.Should().NotBeNull();
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada aynÄ± kodu kullanan yeni kupon isteÄŸinin kaydetmeden Ã§akÄ±ÅŸma hatasÄ± verdiÄŸini doÄŸruluyorum.
    [Fact]
    public async Task Create_Should_Reject_Duplicate_Code()
    {
        var repository = new Mock<ICouponRepository>();
        var unitOfWork = CreateUnitOfWork();
        repository.Setup(item => item.CodeExistsAsync("SUMMER10", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new CreateCouponCommandHandler(repository.Object, unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new CreateCouponCommand("SUMMER10", CouponDiscountType.FixedAmount, 10m),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada bulunan kuponun gÃ¼ncellenip kaydedildiÄŸini doÄŸruluyorum.
    [Fact]
    public async Task Update_Should_Change_Coupon_Details_And_Save()
    {
        var coupon = new Coupon("OLD", CouponDiscountType.FixedAmount, 10m);
        var repository = new Mock<ICouponRepository>();
        var unitOfWork = CreateUnitOfWork();
        repository.Setup(item => item.GetByIdForUpdateAsync(coupon.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coupon);
        repository.Setup(item => item.CodeExistsAsync("NEW", coupon.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new UpdateCouponCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateCouponCommand(coupon.Id, "new", CouponDiscountType.Percentage, 15m),
            CancellationToken.None);

        result.Code.Should().Be("NEW");
        result.DiscountType.Should().Be(CouponDiscountType.Percentage);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada kupon kodu ve sayfa numarası için endpointten önce güvenli sınırların uygulandığını doğruluyorum.
    [Fact]
    public void Validators_Should_Reject_Unsupported_Coupon_Code_And_An_Excessive_Page_Number()
    {
        var createValidation = new CreateCouponCommandValidator().Validate(
            new CreateCouponCommand("SUMMER 20!", CouponDiscountType.FixedAmount, 10m));
        var listValidation = new GetCouponsQueryValidator().Validate(new GetCouponsQuery(10_001));

        createValidation.IsValid.Should().BeFalse();
        listValidation.IsValid.Should().BeFalse();
    }

    // Burada testlerde baÅŸarÄ±lÄ± kayÄ±t davranÄ±ÅŸÄ±nÄ± taklit eden Unit of Work mock'unu hazÄ±rlÄ±yorum.
    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }
}
