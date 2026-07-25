using ECommerce.Application.Addresses.Commands.CreateAddress;
using ECommerce.Application.Addresses.Commands.DeleteAddress;
using ECommerce.Application.Addresses.Commands.SetDefaultAddress;
using ECommerce.Application.Addresses.Commands.UpdateAddress;
using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Addresses.Queries.GetAddresses;
using ECommerce.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.API.Controllers.User;

[ApiController]
[Authorize]
[Route("api/addresses")]
public sealed class AddressesController : ControllerBase
{
    private readonly ISender _sender;

    // Burada adres HTTP isteklerini Application katmanına yönlendirecek MediatR sender'ını hazırlıyorum.
    public AddressesController(ISender sender)
    {
        _sender = sender;
    }

    // Burada oturumdaki kullanıcının isteğe bağlı tür filtresiyle kendi adreslerini listeliyorum.
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AddressDto>>> GetList(
        [FromQuery] AddressType? type,
        CancellationToken cancellationToken)
    {
        var addresses = await _sender.Send(new GetAddressesQuery(type), cancellationToken);
        return Ok(addresses);
    }

    // Burada oturumdaki kullanıcı için yeni adres oluşturma isteğini Application komutuna çeviriyorum.
    [HttpPost]
    public async Task<ActionResult<AddressDto>> Create(
        AddressRequest request,
        CancellationToken cancellationToken)
    {
        var address = await _sender.Send(
            new CreateAddressCommand(
                request.Type,
                request.Title,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.City,
                request.District,
                request.FullAddress,
                request.PostalCode,
                request.IsDefault),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, address);
    }

    // Burada rota adres kimliğiyle gelen düzenleme isteğini güvenli Application komutuna aktarıyorum.
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AddressDto>> Update(
        Guid id,
        AddressRequest request,
        CancellationToken cancellationToken)
    {
        var address = await _sender.Send(
            new UpdateAddressCommand(
                id,
                request.Type,
                request.Title,
                request.FirstName,
                request.LastName,
                request.PhoneNumber,
                request.City,
                request.District,
                request.FullAddress,
                request.PostalCode,
                request.IsDefault),
            cancellationToken);
        return Ok(address);
    }

    // Burada adres silme isteğini kullanıcı sahipliği denetiminin yapılacağı Application katmanına gönderiyorum.
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteAddressCommand(id), cancellationToken);
        return NoContent();
    }

    // Burada adresi kendi teslimat veya fatura türü için varsayılan yapma isteğini iletiyorum.
    [HttpPatch("{id:guid}/default")]
    public async Task<ActionResult<AddressDto>> SetDefault(Guid id, CancellationToken cancellationToken)
    {
        var address = await _sender.Send(new SetDefaultAddressCommand(id), cancellationToken);
        return Ok(address);
    }
}

// Burada adres oluşturma ve güncelleme isteklerinde kullanılan HTTP gövde sözleşmesini tanımlıyorum.
public sealed record AddressRequest(
    AddressType Type,
    string Title,
    string FirstName,
    string LastName,
    string PhoneNumber,
    string City,
    string District,
    string FullAddress,
    string? PostalCode = null,
    bool IsDefault = false);
