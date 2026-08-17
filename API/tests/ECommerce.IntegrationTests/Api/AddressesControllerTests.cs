using ECommerce.API.Controllers.User;
using ECommerce.Application.Addresses.Commands.CreateAddress;
using ECommerce.Application.Addresses.Commands.DeleteAddress;
using ECommerce.Application.Addresses.Commands.SetDefaultAddress;
using ECommerce.Application.Addresses.Commands.UpdateAddress;
using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Addresses.Queries.GetAddresses;
using ECommerce.Domain.Enums;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.IntegrationTests.Api;

public sealed class AddressesControllerTests
{
    // Burada adres oluşturma endpointinin yalnız HTTP isteğini güvenli Application komutuna taşıdığını doğruluyorum.
    [Fact]
    public async Task Create_Should_Send_Create_Command_And_Return_Created()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);
        var request = CreateRequest(isDefault: true);

        var result = await controller.Create(request, CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(StatusCodes.Status201Created);
        var command = sender.LastRequest.Should().BeOfType<CreateAddressCommand>().Subject;
        command.Type.Should().Be(AddressType.Shipping);
        command.Title.Should().Be("Ev");
        command.IsDefault.Should().BeTrue();
    }

    // Burada adres güncelleme endpointinin rota kimliğini komuta eklediğini doğruluyorum.
    [Fact]
    public async Task Update_Should_Send_Route_Address_Id_In_Command()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);
        var addressId = Guid.NewGuid();

        var result = await controller.Update(addressId, CreateRequest(), CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        sender.LastRequest.Should().BeOfType<UpdateAddressCommand>()
            .Which.AddressId.Should().Be(addressId);
    }

    // Burada adres listeleme endpointinin tür filtresini sorguya aktardığını doğruluyorum.
    [Fact]
    public async Task GetList_Should_Send_Optional_Type_Filter()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);

        var result = await controller.GetList(AddressType.Billing, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        sender.LastRequest.Should().BeOfType<GetAddressesQuery>()
            .Which.Type.Should().Be(AddressType.Billing);
    }

    // Burada silme ve varsayılan seçim endpointlerinin rota kimliğini ilgili komutlara aktardığını doğruluyorum.
    [Fact]
    public async Task Delete_And_SetDefault_Should_Send_Route_Address_Id()
    {
        var sender = new RecordingSender();
        var controller = CreateController(sender);
        var addressId = Guid.NewGuid();

        var deleteResult = await controller.Delete(addressId, CancellationToken.None);
        sender.LastRequest.Should().BeOfType<DeleteAddressCommand>()
            .Which.AddressId.Should().Be(addressId);
        deleteResult.Should().BeOfType<NoContentResult>();

        var setDefaultResult = await controller.SetDefault(addressId, CancellationToken.None);
        setDefaultResult.Result.Should().BeOfType<OkObjectResult>();
        sender.LastRequest.Should().BeOfType<SetDefaultAddressCommand>()
            .Which.AddressId.Should().Be(addressId);
    }

    // Burada adres controllerının bütün yüzeyini JWT ile koruduğunu doğruluyorum.
    [Fact]
    public void Controller_Should_Require_Authentication()
    {
        var authorization = typeof(AddressesController)
            .GetCustomAttributes(inherit: true)
            .OfType<AuthorizeAttribute>()
            .Single();

        authorization.Policy.Should().BeNull();
    }

    // Burada doğrudan controller testleri için sender bağlı adres controllerını hazırlıyorum.
    private static AddressesController CreateController(RecordingSender sender)
    {
        return new AddressesController(sender);
    }

    // Burada testler için geçerli adres HTTP isteğini hazırlıyorum.
    private static AddressRequest CreateRequest(bool isDefault = false)
    {
        return new AddressRequest(
            AddressType.Shipping,
            "Ev",
            "Ada",
            "Yılmaz",
            "05000000000",
            "İzmir",
            "Konak",
            null,
            "Alsancak Mahallesi 1. Sokak No: 1",
            "35220",
            isDefault);
    }

    private sealed class RecordingSender : ISender
    {
        public object? LastRequest { get; private set; }

        // Burada cevaplı MediatR isteklerini kaydedip endpoint testleri için uygun adres cevabı veriyorum.
        public Task<TResponse> Send<TResponse>(
            IRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(CreateResponse<TResponse>());
        }

        // Burada cevapsız MediatR isteklerini kaydedip silme akışını başarılı tamamlıyorum.
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            LastRequest = request;
            return Task.CompletedTask;
        }

        // Burada dinamik MediatR isteğini kaydedip örnek adres cevabı döndürüyorum.
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult<object?>(CreateAddressDto());
        }

        // Burada generic stream istekleri için boş asenkron akış üretiyorum.
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(
            IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<TResponse>();
        }

        // Burada dinamik stream istekleri için boş asenkron akış üretiyorum.
        public IAsyncEnumerable<object?> CreateStream(
            object request,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return EmptyAsync<object?>();
        }

        // Burada istenen response türüne göre tekil veya liste adres cevabını üretiyorum.
        private static TResponse CreateResponse<TResponse>()
        {
            if (typeof(TResponse) == typeof(AddressDto))
            {
                return (TResponse)(object)CreateAddressDto();
            }

            if (typeof(TResponse) == typeof(IReadOnlyList<AddressDto>))
            {
                IReadOnlyList<AddressDto> response = [CreateAddressDto()];
                return (TResponse)(object)response;
            }

            throw new InvalidOperationException($"Unexpected response type {typeof(TResponse).Name}.");
        }

        // Burada controller cevapları için temsilî ve güvenli adres DTO'su hazırlıyorum.
                private static AddressDto CreateAddressDto()
        {
            return new AddressDto(
                Guid.NewGuid(),
                AddressType.Shipping,
                "Ev",
                "Ada",
                "Y�lmaz",
                "05000000000",
                "�zmir",
                "Konak",
                "Adres",
                "Adres",
                "35220",
                false,
                DateTime.UtcNow,
                null);
        }

        // Burada stream testleri için gerçekten eleman üretmeyen asenkron koleksiyon hazırlıyorum.
        private static async IAsyncEnumerable<T> EmptyAsync<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }
}

