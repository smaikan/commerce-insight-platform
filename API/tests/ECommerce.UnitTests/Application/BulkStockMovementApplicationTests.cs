using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Application.StockMovements.Commands.BulkCreateStockMovements;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class BulkStockMovementApplicationTests
{
    // Burada geçerli yönetim hareketlerinden oluşan toplu isteğin kabul edildiğini doğruluyorum.
    [Fact]
    public void Validator_Should_Accept_Valid_Administrative_Batch()
    {
        var validator = new BulkCreateStockMovementsCommandValidator();
        var command = new BulkCreateStockMovementsCommand(
        [
            new BulkStockMovementItem(
                Guid.NewGuid(),
                12,
                StockMovementType.Purchase,
                "Tedarikçi teslimatı"),
            new BulkStockMovementItem(
                Guid.NewGuid(),
                -2,
                StockMovementType.Damage,
                "Hasarlı ürün")
        ]);

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // Burada toplu stok hareketindeki açıklamanın gönderilmemesinin geçerli olduğunu doğruluyorum.
    [Fact]
    public void Validator_Should_Accept_Null_Reason()
    {
        var validator = new BulkCreateStockMovementsCommandValidator();
        var command = new BulkCreateStockMovementsCommand(
        [
            new BulkStockMovementItem(
                Guid.NewGuid(),
                -1,
                StockMovementType.Damage,
                Reason: null)
        ]);

        var result = validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    // Burada hiç hareket içermeyen toplu isteğin reddedildiğini doğruluyorum.
    [Fact]
    public void Validator_Should_Reject_Empty_Batch()
    {
        var validator = new BulkCreateStockMovementsCommandValidator();

        var result = validator.TestValidate(new BulkCreateStockMovementsCommand([]));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Movements");
    }

    // Burada JSON veya başka bir istemciden gelen null liste öğesinin doğrulama hatasına dönüştüğünü doğruluyorum.
    [Fact]
    public void Validator_Should_Reject_Null_Movement_Item()
    {
        var validator = new BulkCreateStockMovementsCommandValidator();
        var command = new BulkCreateStockMovementsCommand([null!]);

        var result = validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Movements[0]");
    }

    // Burada tek istekte beş yüzden fazla stok hareketi işlenmesini engelliyorum.
    [Fact]
    public void Validator_Should_Reject_Batches_Larger_Than_Limit()
    {
        var validator = new BulkCreateStockMovementsCommandValidator();
        var movements = Enumerable.Range(0, 501)
            .Select(_ => new BulkStockMovementItem(
                Guid.NewGuid(),
                1,
                StockMovementType.Purchase,
                "Toplu stok girişi"))
            .ToArray();

        var result = validator.TestValidate(new BulkCreateStockMovementsCommand(movements));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Movements");
    }

    // Burada satış gibi iş akışına ait bir hareketin yönetim API'sinden oluşturulmasını engelliyorum.
    [Fact]
    public void Validator_Should_Reject_Operational_Movement_Type()
    {
        var validator = new BulkCreateStockMovementsCommandValidator();
        var command = new BulkCreateStockMovementsCommand(
        [
            new BulkStockMovementItem(
                Guid.NewGuid(),
                -1,
                StockMovementType.Sale,
                "Manuel satış hareketi")
        ]);

        var result = validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.PropertyName.EndsWith(".Type", StringComparison.Ordinal));
    }

    // Burada hareket türüyle imzalı miktarın ters yönde olmasını reddediyorum.
    [Fact]
    public void Validator_Should_Reject_Incompatible_Direction()
    {
        var validator = new BulkCreateStockMovementsCommandValidator();
        var command = new BulkCreateStockMovementsCommand(
        [
            new BulkStockMovementItem(
                Guid.NewGuid(),
                -3,
                StockMovementType.Purchase,
                "Ters yönlü tedarik girişi")
        ]);

        var result = validator.TestValidate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage.Contains("direction", StringComparison.OrdinalIgnoreCase));
    }

    // Burada aynı varyanta sıralı hareketlerin ve başka varyantın tek transaction içinde kaydedildiğini doğruluyorum.
    [Fact]
    public async Task Handler_Should_Apply_Ordered_Movements_In_One_Transaction_And_Save()
    {
        var firstVariant = new ProductVariant(1, "Small", "SKU-BULK-1", 100m, 10);
        var secondVariant = new ProductVariant(1, "Large", "SKU-BULK-2", 120m, 4);
        var repository = new Mock<IProductVariantRepository>();
        repository.Setup(item => item.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([firstVariant, secondVariant]);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new BulkCreateStockMovementsCommandHandler(
            repository.Object,
            unitOfWork);
        var command = new BulkCreateStockMovementsCommand(
        [
            new BulkStockMovementItem(
                firstVariant.Id,
                5,
                StockMovementType.Purchase,
                "Mal kabul"),
            new BulkStockMovementItem(
                firstVariant.Id,
                -2,
                StockMovementType.Damage,
                "Hasarlı koli"),
            new BulkStockMovementItem(
                secondVariant.Id,
                3,
                StockMovementType.TransferIn,
                "Depolar arası transfer")
        ]);

        var result = await handler.Handle(command, CancellationToken.None);

        result.MovementCount.Should().Be(3);
        result.Movements.Select(movement => movement.QuantityDelta)
            .Should().Equal(5, -2, 3);
        result.Movements.Select(movement => movement.StockAfterMovement)
            .Should().Equal(15, 13, 7);
        firstVariant.Stock.Should().Be(13);
        secondVariant.Stock.Should().Be(7);
        firstVariant.StockMovements
            .Where(movement => movement.Type != StockMovementType.OpeningBalance)
            .Select(movement => movement.QuantityDelta)
            .Should().Equal(5, -2);
        secondVariant.StockMovements
            .Where(movement => movement.Type != StockMovementType.OpeningBalance)
            .Should().ContainSingle(movement =>
                movement.QuantityDelta == 3 &&
                movement.StockBeforeMovement == 4 &&
                movement.StockAfterMovement == 7);
        unitOfWork.TransactionCallCount.Should().Be(1);
        unitOfWork.SaveCallCount.Should().Be(1);
        repository.Verify(item => item.GetByIdsForUpdateAsync(
            It.Is<IEnumerable<Guid>>(ids =>
                ids.Distinct().OrderBy(id => id)
                    .SequenceEqual(new[] { firstVariant.Id, secondVariant.Id }.OrderBy(id => id))),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada batch içindeki bir varyant bulunamadığında hiçbir hareketin uygulanmadığını veya kaydedilmediğini doğruluyorum.
    [Fact]
    public async Task Handler_Should_Not_Apply_Or_Save_When_Any_Variant_Is_Missing()
    {
        var existingVariant = new ProductVariant(1, "Standard", "SKU-BULK-MISSING", 90m, 8);
        var missingVariantId = Guid.NewGuid();
        var repository = new Mock<IProductVariantRepository>();
        repository.Setup(item => item.GetByIdsForUpdateAsync(
                It.IsAny<IEnumerable<Guid>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingVariant]);
        var unitOfWork = new RecordingUnitOfWork();
        var handler = new BulkCreateStockMovementsCommandHandler(
            repository.Object,
            unitOfWork);
        var command = new BulkCreateStockMovementsCommand(
        [
            new BulkStockMovementItem(
                existingVariant.Id,
                4,
                StockMovementType.Purchase,
                "Mal kabul"),
            new BulkStockMovementItem(
                missingVariantId,
                -1,
                StockMovementType.Damage,
                "Bulunamayan varyant")
        ]);

        Func<Task> act = () => handler.Handle(command, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
        existingVariant.Stock.Should().Be(8);
        existingVariant.StockMovements.Should().ContainSingle(movement =>
            movement.Type == StockMovementType.OpeningBalance);
        unitOfWork.TransactionCallCount.Should().Be(1);
        unitOfWork.SaveCallCount.Should().Be(0);
    }

    private sealed class RecordingUnitOfWork : IUnitOfWork
    {
        public int SaveCallCount { get; private set; }
        public int TransactionCallCount { get; private set; }

        // Burada test sırasında yapılan kalıcı kayıt çağrılarını sayıyorum.
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.FromResult(1);
        }

        // Burada transaction delegesini çalıştırıp toplu işlemin kaç transaction kullandığını kaydediyorum.
        public Task<T> ExecuteInSerializableTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            CancellationToken cancellationToken = default)
        {
            TransactionCallCount++;
            return operation(cancellationToken);
        }
    }
}
