using FluentValidation;

namespace ECommerce.Application.Brands.Commands.DeleteBrand;

public sealed class DeleteBrandCommandValidator : AbstractValidator<DeleteBrandCommand>
{
    // Burada silinecek marka kimliğinin boş olmamasını doğruluyorum.
    public DeleteBrandCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}
