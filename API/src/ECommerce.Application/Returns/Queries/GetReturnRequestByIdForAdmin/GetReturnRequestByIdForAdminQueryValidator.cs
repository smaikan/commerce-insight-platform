using FluentValidation;

namespace ECommerce.Application.Returns.Queries.GetReturnRequestByIdForAdmin;

public sealed class GetReturnRequestByIdForAdminQueryValidator : AbstractValidator<GetReturnRequestByIdForAdminQuery>
{
    // Burada yönetim iade detay isteğinin geçerli kimlik taşıdığını doğruluyorum.
    public GetReturnRequestByIdForAdminQueryValidator()
    {
        RuleFor(query => query.ReturnRequestId).NotEmpty();
    }
}
