using ECommerce.Application.Addresses.Commands.CreateAddress;
using ECommerce.Application.Addresses.Commands.DeleteAddress;
using ECommerce.Application.Addresses.Commands.SetDefaultAddress;
using ECommerce.Application.Addresses.Commands.UpdateAddress;
using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Addresses.Queries.GetAddresses;
using ECommerce.Application.Common.Exceptions;
using ECommerce.Application.Common.Interfaces;
using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentAssertions;
using Moq;

namespace ECommerce.UnitTests.Application;

public sealed class AddressApplicationTests
{
    // Burada yeni varsayılan adres oluşturulurken aynı türdeki eski varsayılan işaretin kaldırıldığını doğruluyorum.
    [Fact]
    public async Task Create_Should_Replace_Previous_Default_For_Same_Type()
    {
        var previousDefault = CreateAddress(isDefault: true);
        var addresses = new Mock<IAddressRepository>();
        Address? createdAddress = null;
        addresses.Setup(repository => repository.GetDefaultsForUserAndTypeForUpdateAsync(
                7,
                AddressType.Shipping,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Address> { previousDefault });
        addresses.Setup(repository => repository.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Callback<Address, CancellationToken>((address, _) => createdAddress = address)
            .Returns(Task.CompletedTask);
        var unitOfWork = CreateAddressTransactionalUnitOfWork();
        var handler = new CreateAddressCommandHandler(
            addresses.Object,
            new StubCurrentUser(7),
            unitOfWork.Object);

        var result = await handler.Handle(CreateCommand(isDefault: true), CancellationToken.None);

        previousDefault.IsDefault.Should().BeFalse();
        createdAddress.Should().NotBeNull();
        createdAddress!.UserId.Should().Be(7);
        createdAddress.IsDefault.Should().BeTrue();
        result.Id.Should().Be(createdAddress.Id);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada başka kullanıcıya ait adres kimliğinin güncelleme için bulunamadığını doğruluyorum.
    [Fact]
    public async Task Update_Should_Not_Expose_Another_Users_Address()
    {
        var addresses = new Mock<IAddressRepository>();
        addresses.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                It.IsAny<Guid>(),
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);
        var handler = new UpdateAddressCommandHandler(
            addresses.Object,
            new StubCurrentUser(7),
            CreateAddressTransactionalUnitOfWork().Object);

        Func<Task> act = () => handler.Handle(
            new UpdateAddressCommand(
                Guid.NewGuid(),
                AddressType.Shipping,
                "Ev",
                "Ada",
                "Yılmaz",
                "05000000000",
                "İzmir",
                "Konak",
                "Mahalle",
                "Adres"),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // Burada varsayılan seçim değişince yalnız aynı türdeki eski varsayılanların kaldırıldığını doğruluyorum.
    [Fact]
    public async Task SetDefault_Should_Clear_Other_Defaults_Of_The_Same_Type()
    {
        var selectedAddress = CreateAddress(type: AddressType.Billing);
        var previousDefault = CreateAddress(type: AddressType.Billing, isDefault: true);
        var addresses = new Mock<IAddressRepository>();
        addresses.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                selectedAddress.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(selectedAddress);
        addresses.Setup(repository => repository.GetDefaultsForUserAndTypeForUpdateAsync(
                7,
                AddressType.Billing,
                selectedAddress.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Address> { previousDefault });
        var unitOfWork = CreateAddressTransactionalUnitOfWork();
        var handler = new SetDefaultAddressCommandHandler(
            addresses.Object,
            new StubCurrentUser(7),
            unitOfWork.Object);

        var result = await handler.Handle(
            new SetDefaultAddressCommand(selectedAddress.Id),
            CancellationToken.None);

        previousDefault.IsDefault.Should().BeFalse();
        selectedAddress.IsDefault.Should().BeTrue();
        result.IsDefault.Should().BeTrue();
    }

    // Burada sipariş geçmişine bağlı adresin silinmeden önce anlaşılır bir conflict hatası verdiğini doğruluyorum.
    [Fact]
    public async Task Delete_Should_Reject_Address_Referenced_By_Order()
    {
        var address = CreateAddress();
        var addresses = new Mock<IAddressRepository>();
        addresses.Setup(repository => repository.GetByIdForUserForUpdateAsync(
                address.Id,
                7,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);
        addresses.Setup(repository => repository.IsReferencedByOrderAsync(
                address.Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var unitOfWork = new Mock<IUnitOfWork>();
        var handler = new DeleteAddressCommandHandler(
            addresses.Object,
            new StubCurrentUser(7),
            unitOfWork.Object);

        Func<Task> act = () => handler.Handle(
            new DeleteAddressCommand(address.Id),
            CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>();
        addresses.Verify(repository => repository.Remove(It.IsAny<Address>()), Times.Never);
        unitOfWork.Verify(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // Burada liste sorgusunun yalnız geçerli kullanıcının kimliğiyle repository'ye gittiğini doğruluyorum.
    [Fact]
    public async Task GetAddresses_Should_Query_Only_Current_User()
    {
        var address = CreateAddress();
        var addresses = new Mock<IAddressRepository>();
        addresses.Setup(repository => repository.GetByUserIdAsync(
                7,
                AddressType.Shipping,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Address> { address });
        var handler = new GetAddressesQueryHandler(addresses.Object, new StubCurrentUser(7));

        var result = await handler.Handle(new GetAddressesQuery(AddressType.Shipping), CancellationToken.None);

        result.Should().ContainSingle();
        result.Single().Id.Should().Be(address.Id);
        addresses.Verify(repository => repository.GetByUserIdAsync(
            7,
            AddressType.Shipping,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // Burada adres command validatorlarının geçersiz kimlik, tür ve zorunlu alanları reddettiğini doğruluyorum.
    [Fact]
    public void Validators_Should_Reject_Invalid_Requests()
    {
        new CreateAddressCommandValidator()
            .Validate(new CreateAddressCommand(
                (AddressType)99,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                new string('P', Address.MaximumPostalCodeLength + 1)))
            .IsValid.Should().BeFalse();
        new UpdateAddressCommandValidator()
            .Validate(new UpdateAddressCommand(
                Guid.Empty,
                (AddressType)99,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty,
                string.Empty))
            .IsValid.Should().BeFalse();
        new DeleteAddressCommandValidator()
            .Validate(new DeleteAddressCommand(Guid.Empty))
            .IsValid.Should().BeFalse();
        new SetDefaultAddressCommandValidator()
            .Validate(new SetDefaultAddressCommand(Guid.Empty))
            .IsValid.Should().BeFalse();
        new GetAddressesQueryValidator()
            .Validate(new GetAddressesQuery((AddressType)99))
            .IsValid.Should().BeFalse();
    }

    // Burada testler için geçerli adres oluşturma isteğini hazırlıyorum.
    private static CreateAddressCommand CreateCommand(bool isDefault = false)
    {
        return new CreateAddressCommand(
            AddressType.Shipping,
            "Ev",
            "Ada",
            "Yılmaz",
            "05000000000",
            "İzmir",
            "Konak",
            "Mahalle",
            "Alsancak Mahallesi 1. Sokak No: 1",
            "35220",
            isDefault);
    }

    // Burada testler için sahibine bağlı ve isteğe göre varsayılan olan geçerli adres oluşturuyorum.
    private static Address CreateAddress(
        AddressType type = AddressType.Shipping,
        bool isDefault = false)
    {
        return new Address(
            7,
            type,
            "Ev",
            "Ada",
            "Yılmaz",
            "05000000000",
            "İzmir",
            "Konak",
            "Mahalle",
            "Alsancak Mahallesi 1. Sokak No: 1",
            "35220",
            isDefault);
    }

    // Burada serializable transaction delegesini test içinde doğrudan çalıştıran iş birimi mockunu hazırlıyorum.
    private static Mock<IUnitOfWork> CreateAddressTransactionalUnitOfWork()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(unit => unit.ExecuteInSerializableTransactionAsync(
                It.IsAny<Func<CancellationToken, Task<AddressDto>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<AddressDto>>, CancellationToken>((operation, token) => operation(token));
        unitOfWork.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return unitOfWork;
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        // Burada test isteğinin sabit oturum kullanıcı kimliğini hazırlıyorum.
        public StubCurrentUser(long userId)
        {
            UserId = userId;
        }

        public long? UserId { get; }
    }
}


