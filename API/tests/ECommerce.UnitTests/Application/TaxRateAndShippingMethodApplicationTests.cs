using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.ShippingMethods.Commands.CreateShippingMethod;
using ECommerce.Application.ShippingMethods.Commands.UpdateShippingMethod;
using ECommerce.Application.TaxRates.Commands.CreateTaxRate;
using ECommerce.Application.TaxRates.Commands.SetTaxRateActivation;
using ECommerce.Domain.Entities;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class TaxRateAndShippingMethodApplicationTests
{
    // Burada benzersiz ada sahip vergi oranının eklenip kaydedildiğini doğruluyorum.
    [Fact]
    public async Task CreateTaxRate_Should_Add_And_Save_Tax_Rate()
    {
        var repository = new Mock<ITaxRateRepository>();
        var unitOfWork = CreateUnitOfWork();
        TaxRate? addedTaxRate = null;
        repository
            .Setup(item => item.NameExistsAsync("KDV", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        repository
            .Setup(item => item.AddAsync(It.IsAny<TaxRate>(), It.IsAny<CancellationToken>()))
            .Callback<TaxRate, CancellationToken>((taxRate, _) => addedTaxRate = taxRate)
            .Returns(Task.CompletedTask);
        var handler = new CreateTaxRateCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(new CreateTaxRateCommand(" KDV ", 20m), CancellationToken.None);

        result.Name.Should().Be("KDV");
        result.Rate.Should().Be(20m);
        addedTaxRate.Should().NotBeNull();
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada aynı ada sahip vergi oranının kayda geçmeden çakışma hatası verdiğini doğruluyorum.
    [Fact]
    public async Task CreateTaxRate_Should_Reject_Duplicate_Name()
    {
        var repository = new Mock<ITaxRateRepository>();
        repository
            .Setup(item => item.NameExistsAsync("KDV", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var handler = new CreateTaxRateCommandHandler(repository.Object, CreateUnitOfWork().Object);

        Func<Task> act = () => handler.Handle(new CreateTaxRateCommand("KDV", 20m), CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        repository.Verify(item => item.AddAsync(It.IsAny<TaxRate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada kargo yöntemi güncellemesinin ad, ücret ve sıra değişikliklerini tek kayıtta topladığını doğruluyorum.
    [Fact]
    public async Task UpdateShippingMethod_Should_Change_Editable_Values_And_Save()
    {
        var shippingMethod = new ShippingMethod("Standart", 49.90m, displayOrder: 1);
        var repository = new Mock<IShippingMethodRepository>();
        var unitOfWork = CreateUnitOfWork();
        repository
            .Setup(item => item.GetByIdForUpdateAsync(shippingMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(shippingMethod);
        repository
            .Setup(item => item.NameExistsAsync("Ekspres", shippingMethod.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new UpdateShippingMethodCommandHandler(repository.Object, unitOfWork.Object);

        var result = await handler.Handle(
            new UpdateShippingMethodCommand(shippingMethod.Id, "Ekspres", 99.90m, 2),
            CancellationToken.None);

        result.Name.Should().Be("Ekspres");
        result.FixedFee.Should().Be(99.90m);
        result.DisplayOrder.Should().Be(2);
        unitOfWork.Verify(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada aktiflik komutunun geçmiş bağları silmeden vergi oranını pasifleştirdiğini doğruluyorum.
    [Fact]
    public async Task SetTaxRateActivation_Should_Deactivate_Existing_Tax_Rate()
    {
        var taxRate = new TaxRate("KDV", 20m);
        var repository = new Mock<ITaxRateRepository>();
        repository
            .Setup(item => item.GetByIdForUpdateAsync(taxRate.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(taxRate);
        var handler = new SetTaxRateActivationCommandHandler(repository.Object, CreateUnitOfWork().Object);

        var result = await handler.Handle(
            new SetTaxRateActivationCommand(taxRate.Id, false),
            CancellationToken.None);

        result.IsActive.Should().BeFalse();
        taxRate.IsActive.Should().BeFalse();
    }

    // Burada testlerde başarılı kayıt davranışını taklit eden Unit of Work mock'unu hazırlıyorum.
    private static Mock<IUnitOfWork> CreateUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork
            .Setup(item => item.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        return unitOfWork;
    }
}
