using ECommerce.Application.Common.Identifiers;
using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.CurrentAccounts;
using FluentValidation;

namespace ECommerce.Application.Accounting.PurchaseInvoices;

public sealed class CurrentAccountInputValidator : AbstractValidator<CurrentAccountInput>
{
    // Burada tek cari hesap ana verisinin kimlik, iletişim, vergi ve adres sınırlarını doğruluyorum.
    public CurrentAccountInputValidator()
    {
        RuleFor(input => input.Code).NotEmpty().MaximumLength(CurrentAccount.MaximumCodeLength);
        RuleFor(input => input.Type).IsInEnum();
        RuleFor(input => input.Name).NotEmpty().MaximumLength(CurrentAccount.MaximumNameLength);
        RuleFor(input => input.TradeName).MaximumLength(CurrentAccount.MaximumNameLength);
        RuleFor(input => input.NationalIdentityNumber).MaximumLength(CurrentAccount.MaximumIdentityNumberLength);
        RuleFor(input => input.TaxNumber).MaximumLength(CurrentAccount.MaximumIdentityNumberLength);
        RuleFor(input => input.TaxOffice).MaximumLength(CurrentAccount.MaximumTaxOfficeLength);
        RuleFor(input => input.PhoneNumber).MaximumLength(CurrentAccount.MaximumPhoneLength);
        RuleFor(input => input.Email).EmailAddress().MaximumLength(CurrentAccount.MaximumEmailLength)
            .When(input => !string.IsNullOrWhiteSpace(input.Email));
        RuleFor(input => input.Country).MaximumLength(CurrentAccount.MaximumAddressPartLength);
        RuleFor(input => input.City).MaximumLength(CurrentAccount.MaximumAddressPartLength);
        RuleFor(input => input.District).MaximumLength(CurrentAccount.MaximumAddressPartLength);
        RuleFor(input => input.Neighborhood).MaximumLength(CurrentAccount.MaximumAddressPartLength);
        RuleFor(input => input.AddressLine).MaximumLength(CurrentAccount.MaximumAddressLineLength);
        RuleFor(input => input.PostalCode).MaximumLength(CurrentAccount.MaximumPostalCodeLength);
        RuleFor(input => input.UserId)
            .Must(value => PublicIdCodec.TryDecodeUserId(value, out _))
            .When(input => !string.IsNullOrWhiteSpace(input.UserId))
            .WithMessage("UserId must be a canonical public user ID.");
    }
}

public sealed class CreateCurrentAccountCommandValidator : AbstractValidator<CreateCurrentAccountCommand>
{
    // Burada cari hesap oluşturma isteğinin ana veri sözleşmesini doğruluyorum.
    public CreateCurrentAccountCommandValidator()
    {
        RuleFor(command => command.Account).NotNull().SetValidator(new CurrentAccountInputValidator());
    }
}

public sealed class UpdateCurrentAccountCommandValidator : AbstractValidator<UpdateCurrentAccountCommand>
{
    // Burada cari hesap güncelleme isteğinin kimlik ve ana veri sözleşmesini doğruluyorum.
    public UpdateCurrentAccountCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Account).NotNull().SetValidator(new CurrentAccountInputValidator());
    }
}

public sealed class GetCurrentAccountsQueryValidator : AbstractValidator<GetCurrentAccountsQuery>
{
    // Burada cari hesap listeleme sorgusunun güvenli sayfa sınırlarını doğruluyorum.
    public GetCurrentAccountsQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class PurchaseInvoiceHeaderInputValidator : AbstractValidator<PurchaseInvoiceHeaderInput>
{
    // Burada alış faturası başlığını yalnız TRY ve birim kurla birlikte temel belge kurallarına göre doğruluyorum.
    public PurchaseInvoiceHeaderInputValidator()
    {
        RuleFor(header => header.CurrentAccountId).NotEmpty();
        RuleFor(header => header.InvoiceNumber)
            .NotEmpty()
            .MaximumLength(
                ECommerce.Domain.Accounting.PurchaseInvoices.PurchaseInvoice
                    .MaximumInvoiceNumberLength);
        RuleFor(header => header.InvoiceDate).NotEmpty();
        RuleFor(header => header.CurrencyCode)
            .Must(value => string.Equals(
                value?.Trim(),
                "TRY",
                StringComparison.OrdinalIgnoreCase))
            .WithMessage("Currency code must be TRY.");
        RuleFor(header => header.ExchangeRate)
            .Equal(1m)
            .WithMessage("Exchange rate must be 1 for TRY.");
        RuleFor(header => header.Description)
            .MaximumLength(
                ECommerce.Domain.Accounting.PurchaseInvoices.PurchaseInvoice
                    .MaximumDescriptionLength);
    }
}

public sealed class CreatePurchaseInvoiceCommandValidator : AbstractValidator<CreatePurchaseInvoiceCommand>
{
    // Burada alış faturası oluşturma isteğinin başlık ve satır sözleşmelerini birlikte doğruluyorum.
    public CreatePurchaseInvoiceCommandValidator()
    {
        RuleFor(command => command.Header)
            .NotNull()
            .SetValidator(new PurchaseInvoiceHeaderInputValidator());
        RuleFor(command => command.Lines).NotNull().NotEmpty();
        RuleForEach(command => command.Lines).SetValidator(new PurchaseInvoiceLineInputValidator());
    }
}

public sealed class UpdatePurchaseInvoiceCommandValidator : AbstractValidator<UpdatePurchaseInvoiceCommand>
{
    // Burada toplu taslak güncellemesinin kimlik, başlık ve satır sözleşmelerini doğruluyorum.
    public UpdatePurchaseInvoiceCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Header)
            .NotNull()
            .SetValidator(new PurchaseInvoiceHeaderInputValidator());
        RuleFor(command => command.Lines).NotNull().NotEmpty();
        RuleForEach(command => command.Lines).SetValidator(new PurchaseInvoiceLineInputValidator());
    }
}

public sealed class PurchaseInvoiceLineInputValidator : AbstractValidator<PurchaseInvoiceLineInput>
{
    // Burada yeni alış satırının katalog kimliği ve ticari alanlarını doğruluyorum.
    public PurchaseInvoiceLineInputValidator()
    {
        RuleFor(line => line.LineNumber).GreaterThan(0);
        RuleFor(line => line.ProductVariantId).NotEmpty();
        RuleFor(line => line.PurchaseQuantity).GreaterThan(0m);
        RuleFor(line => line.UnitsPerPurchaseUnit).GreaterThan(0m);
        RuleFor(line => line.UnitOfMeasure).NotEmpty().MaximumLength(50);
        RuleFor(line => line.PriceEntryMode).IsInEnum();
        RuleFor(line => line.EnteredUnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(line => line.VatRate).InclusiveBetween(0m, 100m);
        RuleFor(line => line.LineDiscountValue).GreaterThanOrEqualTo(0m)
            .When(line => line.LineDiscountValue.HasValue);
        RuleFor(line => line.LineDiscountValue).InclusiveBetween(0m, 100m)
            .When(line => line.LineDiscountType == DiscountType.Percentage);
    }
}

public sealed class PurchaseInvoiceLineCommercialUpdateInputValidator
    : AbstractValidator<PurchaseInvoiceLineCommercialUpdateInput>
{
    // Burada mevcut snapshot'ı koruyan satır güncellemesinin yalnız ticari alanlarını doğruluyorum.
    public PurchaseInvoiceLineCommercialUpdateInputValidator()
    {
        RuleFor(line => line.PurchaseQuantity).GreaterThan(0m);
        RuleFor(line => line.UnitsPerPurchaseUnit).GreaterThan(0m);
        RuleFor(line => line.UnitOfMeasure).NotEmpty().MaximumLength(50);
        RuleFor(line => line.PriceEntryMode).IsInEnum();
        RuleFor(line => line.EnteredUnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(line => line.VatRate).InclusiveBetween(0m, 100m);
        RuleFor(line => line.LineDiscountValue).GreaterThanOrEqualTo(0m)
            .When(line => line.LineDiscountValue.HasValue);
        RuleFor(line => line.LineDiscountValue).InclusiveBetween(0m, 100m)
            .When(line => line.LineDiscountType == DiscountType.Percentage);
    }
}

public sealed class AddPurchaseInvoiceLineCommandValidator : AbstractValidator<AddPurchaseInvoiceLineCommand>
{
    // Burada taslak faturaya yeni satır ekleme isteğinin kimlik ve satır alanlarını doğruluyorum.
    public AddPurchaseInvoiceLineCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
        RuleFor(command => command.Line).NotNull().SetValidator(new PurchaseInvoiceLineInputValidator());
    }
}

public sealed class UpdatePurchaseInvoiceLineCommandValidator : AbstractValidator<UpdatePurchaseInvoiceLineCommand>
{
    // Burada mevcut satırın yalnız ticari alanlarını değiştiren isteği doğruluyorum.
    public UpdatePurchaseInvoiceLineCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
        RuleFor(command => command.LineId).NotEmpty();
        RuleFor(command => command.Line)
            .NotNull()
            .SetValidator(new PurchaseInvoiceLineCommercialUpdateInputValidator());
    }
}

public sealed class RemovePurchaseInvoiceLineCommandValidator : AbstractValidator<RemovePurchaseInvoiceLineCommand>
{
    // Burada satır kaldırma isteğinin fatura ve satır kimliklerini doğruluyorum.
    public RemovePurchaseInvoiceLineCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
        RuleFor(command => command.LineId).NotEmpty();
    }
}

public sealed class SetPurchaseInvoiceAllocationsCommandValidator : AbstractValidator<SetPurchaseInvoiceAllocationsCommand>
{
    // Burada stok hareketi tahsislerinin kimlik, miktar ve tekillik kurallarını doğruluyorum.
    public SetPurchaseInvoiceAllocationsCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
        RuleFor(command => command.LineId).NotEmpty();
        RuleFor(command => command.Allocations).NotNull().NotEmpty();
        RuleForEach(command => command.Allocations).ChildRules(allocation =>
        {
            allocation.RuleFor(item => item.StockMovementId).NotEmpty();
            allocation.RuleFor(item => item.Quantity).GreaterThan(0);
        });
        RuleFor(command => command.Allocations)
            .Must(items => items.Select(item => item.StockMovementId).Distinct().Count() == items.Count)
            .When(command => command.Allocations is not null)
            .WithMessage("Stock movement allocations must be unique.");
    }
}

public sealed class PostPurchaseInvoiceCommandValidator : AbstractValidator<PostPurchaseInvoiceCommand>
{
    // Burada post edilecek alış faturasının kimliğini doğruluyorum.
    public PostPurchaseInvoiceCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class GetPurchaseInvoicesQueryValidator : AbstractValidator<GetPurchaseInvoicesQuery>
{
    // Burada alış faturası liste sorgusunun güvenli sayfa sınırlarını doğruluyorum.
    public GetPurchaseInvoicesQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
