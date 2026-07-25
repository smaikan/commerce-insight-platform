using ECommerce.Domain.Entities;
using ECommerce.Domain.Enums;
using FluentValidation;

namespace ECommerce.Application.Returns.Commands.CreateReturnRequest;

public sealed class CreateReturnRequestCommandValidator : AbstractValidator<CreateReturnRequestCommand>
{
    // Burada müşteri iade talebinin kimlik, kalem, değişim ve not sınırlarını doğruluyorum.
    public CreateReturnRequestCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Items)
            .NotNull()
            .NotEmpty()
            .Must(items => items.Count <= ReturnRequest.MaximumItemCount)
            .WithMessage($"Return request cannot contain more than {ReturnRequest.MaximumItemCount} items.");
        RuleForEach(command => command.Items).ChildRules(item =>
        {
            item.RuleFor(value => value.OrderItemId).NotEmpty();
            item.RuleFor(value => value.Quantity).GreaterThan(0);
        });
        RuleFor(command => command)
            .Must(command => command.Type != ReturnType.Exchange ||
                             command.Items is not null &&
                             command.Items.All(item => item.ReplacementProductVariantId.HasValue && item.ReplacementProductVariantId.Value != Guid.Empty))
            .WithMessage("Every exchange item requires a replacement product variant.");
        RuleFor(command => command)
            .Must(command => command.Type != ReturnType.Refund ||
                             command.Items is not null &&
                             command.Items.All(item => !item.ReplacementProductVariantId.HasValue))
            .WithMessage("Refund items cannot contain a replacement product variant.");
        RuleFor(command => command.CustomerNote)
            .MaximumLength(ReturnRequest.MaximumCustomerNoteLength)
            .When(command => !string.IsNullOrWhiteSpace(command.CustomerNote));
    }
}
