using FluentValidation;

namespace ECommerce.Application.Collections.Commands.DeleteCollection;

public sealed class DeleteCollectionCommandValidator : AbstractValidator<DeleteCollectionCommand>
{
    // Burada silinecek koleksiyon kimliğinin boş olmamasını doğruluyorum.
    public DeleteCollectionCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
