using FluentValidation;

namespace ECommerce.Application.Tags.Commands.DeleteTag;

public sealed class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
{
    // Burada silinecek etiket kimliğinin boş olmamasını doğruluyorum.
    public DeleteTagCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
