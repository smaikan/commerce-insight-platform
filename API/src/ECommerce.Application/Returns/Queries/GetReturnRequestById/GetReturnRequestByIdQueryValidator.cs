using FluentValidation;

namespace ECommerce.Application.Returns.Queries.GetReturnRequestById;

public sealed class GetReturnRequestByIdQueryValidator : AbstractValidator<GetReturnRequestByIdQuery>
{
    // Burada müşteri iade detay isteğinin geçerli kimlik taşıdığını doğruluyorum.
    public GetReturnRequestByIdQueryValidator()
    {
        RuleFor(query => query.ReturnRequestId).NotEmpty();
    }
}
