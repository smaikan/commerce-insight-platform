using FluentValidation;

namespace ECommerce.Application.Returns.Commands.ReceiveReturnRequest;

public sealed class ReceiveReturnRequestCommandValidator : AbstractValidator<ReceiveReturnRequestCommand>
{
    // Burada ürün teslim alma isteğinin geçerli iade talebi kimliği taşıdığını doğruluyorum.
    public ReceiveReturnRequestCommandValidator()
    {
        RuleFor(command => command.ReturnRequestId).NotEmpty();
    }
}
