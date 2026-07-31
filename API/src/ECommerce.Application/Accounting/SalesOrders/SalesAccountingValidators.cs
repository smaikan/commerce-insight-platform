using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Accounting.SalesInvoices;
using ECommerce.Domain.Accounting.SalesOrders;
using FluentValidation;

namespace ECommerce.Application.Accounting.SalesOrders;

public sealed class AccountingSalesOrderHeaderInputValidator : AbstractValidator<AccountingSalesOrderHeaderInput>
{
    // Burada Accounting satış başlığının zorunlu ve açıkça girilen alanlarını doğruluyorum.
    public AccountingSalesOrderHeaderInputValidator()
    {
        RuleFor(header => header.CurrentAccountId).NotEmpty();
        RuleFor(header => header.OrderNumber)
            .NotEmpty()
            .MaximumLength(AccountingSalesOrder.MaximumOrderNumberLength);
        RuleFor(header => header.OrderDate).NotEmpty();
        RuleFor(header => header.CurrencyCode)
            .NotEmpty()
            .Length(3)
            .Matches("^[A-Za-z]{3}$")
            .Must(value => string.Equals(value, "TRY", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Accounting sales currently supports only TRY.");
        RuleFor(header => header.ExchangeRate)
            .Equal(1m)
            .WithMessage("TRY accounting sales must use exchange rate 1.");
        RuleFor(header => header.ShippingTotal)
            .GreaterThanOrEqualTo(0m)
            .PrecisionScale(18, 2, false);
        RuleFor(header => header.ShippingPayer).IsInEnum();
        RuleFor(header => header)
            .Must(HasConsistentShippingPayer)
            .WithMessage(
                "Shipping payer must be None when shipping is zero and Seller or Customer when positive.");
        RuleFor(header => header.Description)
            .MaximumLength(AccountingSalesOrder.MaximumDescriptionLength);
        RuleFor(header => header.InvoiceDiscountType)
            .Must(value =>
                !value.HasValue ||
                value.Value is DiscountType.Percentage or DiscountType.FixedInvoiceTotal)
            .WithMessage("Invoice discount type must be Percentage or FixedInvoiceTotal.");
        RuleFor(header => header.InvoiceDiscountTaxBasis)
            .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
            .WithMessage("Invoice discount tax basis is invalid.");
        RuleFor(header => header.InvoiceDiscountValue)
            .GreaterThanOrEqualTo(0m)
            .When(header => header.InvoiceDiscountValue.HasValue);
        RuleFor(header => header.InvoiceDiscountValue)
            .InclusiveBetween(0m, 100m)
            .When(header => header.InvoiceDiscountType == DiscountType.Percentage);
        RuleFor(header => header)
            .Must(HasCompleteInvoiceDiscount)
            .WithMessage("Invoice discount type, value and tax basis must be supplied together.");
    }

    // Burada opsiyonel fatura indiriminin parçalı gönderilmesini engelliyorum.
    private static bool HasCompleteInvoiceDiscount(AccountingSalesOrderHeaderInput header)
    {
        var suppliedCount = new object?[]
        {
            header.InvoiceDiscountType,
            header.InvoiceDiscountValue,
            header.InvoiceDiscountTaxBasis
        }.Count(value => value is not null);
        return suppliedCount is 0 or 3;
    }

    // Burada kargo tutarı ile ödeyen tarafın birbirini zorunlu ve açık biçimde tamamladığını doğruluyorum.
    private static bool HasConsistentShippingPayer(AccountingSalesOrderHeaderInput header)
    {
        return header.ShippingTotal == 0m
            ? header.ShippingPayer == ShippingPayer.None
            : header.ShippingPayer is ShippingPayer.Seller or ShippingPayer.Customer;
    }
}

public sealed class AccountingSalesOrderLineInputValidator : AbstractValidator<AccountingSalesOrderLineInput>
{
    // Burada Accounting satış satırının ürün, miktar, fiyat, vergi ve indirim sözleşmesini doğruluyorum.
    public AccountingSalesOrderLineInputValidator()
    {
        RuleFor(line => line.LineNumber).GreaterThan(0);
        RuleFor(line => line.ProductVariantId).NotEmpty();
        RuleFor(line => line.Quantity).GreaterThan(0m);
        RuleFor(line => line.UnitOfMeasure)
            .NotEmpty()
            .MaximumLength(AccountingSalesOrderItem.MaximumUnitOfMeasureLength);
        RuleFor(line => line.UnitsPerSaleUnit).GreaterThan(0m);
        RuleFor(line => line.EnteredUnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(line => line.PriceEntryMode).IsInEnum();
        RuleFor(line => line.VatRate).InclusiveBetween(0m, 100m);
        RuleFor(line => line.LineDiscountType)
            .Must(value =>
                !value.HasValue ||
                value.Value is DiscountType.Percentage
                    or DiscountType.FixedPerUnit
                    or DiscountType.FixedLineTotal)
            .WithMessage(
                "Line discount type must be Percentage, FixedPerUnit or FixedLineTotal.");
        RuleFor(line => line.LineDiscountTaxBasis)
            .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
            .WithMessage("Line discount tax basis is invalid.");
        RuleFor(line => line.LineDiscountUnitBasis)
            .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
            .WithMessage("Line discount unit basis is invalid.");
        RuleFor(line => line.LineDiscountValue)
            .GreaterThanOrEqualTo(0m)
            .When(line => line.LineDiscountValue.HasValue);
        RuleFor(line => line.LineDiscountValue)
            .InclusiveBetween(0m, 100m)
            .When(line => line.LineDiscountType == DiscountType.Percentage);
        RuleFor(line => line)
            .Must(HasCompleteLineDiscount)
            .WithMessage(
                "Line discount type, value and tax basis must be supplied together; " +
                "unit basis is required only for FixedPerUnit.");
        RuleFor(line => line)
            .Must(HasWholeStockQuantity)
            .WithMessage("Quantity multiplied by units per sale unit must be a positive whole stock quantity.");
    }

    // Burada ortak indirim alanlarını ve UnitBasis'in yalnız FixedPerUnit kuralını birlikte doğruluyorum.
    private static bool HasCompleteLineDiscount(AccountingSalesOrderLineInput line)
    {
        if (line.LineDiscountType is null &&
            line.LineDiscountValue is null &&
            line.LineDiscountTaxBasis is null &&
            line.LineDiscountUnitBasis is null)
        {
            return true;
        }

        if (!line.LineDiscountType.HasValue ||
            !line.LineDiscountValue.HasValue ||
            !line.LineDiscountTaxBasis.HasValue)
        {
            return false;
        }

        return line.LineDiscountType == DiscountType.FixedPerUnit
            ? line.LineDiscountUnitBasis.HasValue
            : !line.LineDiscountUnitBasis.HasValue;
    }

    // Burada muhasebe satış birimini fiziksel stok için güvenli tam sayıya dönüştürülebilir tutuyorum.
    private static bool HasWholeStockQuantity(AccountingSalesOrderLineInput line)
    {
        var stockQuantity = line.Quantity * line.UnitsPerSaleUnit;
        return stockQuantity > 0m &&
               stockQuantity <= int.MaxValue &&
               stockQuantity == decimal.Truncate(stockQuantity);
    }
}

public sealed class SalesInvoiceHeaderInputValidator : AbstractValidator<SalesInvoiceHeaderInput>
{
    // Burada iç satış faturası başlığında istemcinin vermesi gereken alanları doğruluyorum.
    public SalesInvoiceHeaderInputValidator()
    {
        RuleFor(header => header.InvoiceNumber)
            .NotEmpty()
            .MaximumLength(SalesInvoice.MaximumInvoiceNumberLength);
        RuleFor(header => header.InvoiceDate).NotEmpty();
        RuleFor(header => header.Description)
            .MaximumLength(SalesInvoice.MaximumDescriptionLength);
    }
}

public sealed class SalesInvoiceLineUpdateInputValidator :
    AbstractValidator<SalesInvoiceLineUpdateInput>
{
    // Burada fatura satırı güncellemesinin yalnız geçerli ticari miktar, fiyat, KDV ve indirim alanları taşımasını sağlıyorum.
    public SalesInvoiceLineUpdateInputValidator()
    {
        RuleFor(line => line.Quantity).GreaterThan(0m);
        RuleFor(line => line.UnitOfMeasure)
            .NotEmpty()
            .MaximumLength(AccountingSalesOrderItem.MaximumUnitOfMeasureLength);
        RuleFor(line => line.UnitsPerSaleUnit).GreaterThan(0m);
        RuleFor(line => line.PriceEntryMode).IsInEnum();
        RuleFor(line => line.VatRate).InclusiveBetween(0m, 100m);
        RuleFor(line => line.EnteredUnitPrice).GreaterThanOrEqualTo(0m);
        RuleFor(line => line.LineDiscountType)
            .Must(value =>
                !value.HasValue ||
                value.Value is DiscountType.Percentage
                    or DiscountType.FixedPerUnit
                    or DiscountType.FixedLineTotal)
            .WithMessage(
                "Line discount type must be Percentage, FixedPerUnit or FixedLineTotal.");
        RuleFor(line => line.LineDiscountTaxBasis)
            .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
            .WithMessage("Line discount tax basis is invalid.");
        RuleFor(line => line.LineDiscountUnitBasis)
            .Must(value => !value.HasValue || Enum.IsDefined(value.Value))
            .WithMessage("Line discount unit basis is invalid.");
        RuleFor(line => line.LineDiscountValue)
            .GreaterThanOrEqualTo(0m)
            .When(line => line.LineDiscountValue.HasValue);
        RuleFor(line => line.LineDiscountValue)
            .InclusiveBetween(0m, 100m)
            .When(line => line.LineDiscountType == DiscountType.Percentage);
        RuleFor(line => line)
            .Must(HasCompleteLineDiscount)
            .WithMessage(
                "Line discount type, value and tax basis must be supplied together; " +
                "unit basis is required only for FixedPerUnit.");
        RuleFor(line => line)
            .Must(HasWholeStockQuantity)
            .WithMessage(
                "Quantity multiplied by units per sale unit must be a positive whole stock quantity.");
    }

    // Burada güncellenen ticari indirimin ortak alanlarını ve FixedPerUnit birim bazını birlikte doğruluyorum.
    private static bool HasCompleteLineDiscount(SalesInvoiceLineUpdateInput line)
    {
        if (line.LineDiscountType is null &&
            line.LineDiscountValue is null &&
            line.LineDiscountTaxBasis is null &&
            line.LineDiscountUnitBasis is null)
        {
            return true;
        }

        if (!line.LineDiscountType.HasValue ||
            !line.LineDiscountValue.HasValue ||
            !line.LineDiscountTaxBasis.HasValue)
        {
            return false;
        }

        return line.LineDiscountType == DiscountType.FixedPerUnit
            ? line.LineDiscountUnitBasis.HasValue
            : !line.LineDiscountUnitBasis.HasValue;
    }

    // Burada fatura satırı güncellemesinin fiziksel stok miktarını pozitif tam sayıda tuttuğunu doğruluyorum.
    private static bool HasWholeStockQuantity(SalesInvoiceLineUpdateInput line)
    {
        var stockQuantity = line.Quantity * line.UnitsPerSaleUnit;
        return stockQuantity > 0m &&
               stockQuantity <= int.MaxValue &&
               stockQuantity == decimal.Truncate(stockQuantity);
    }
}

public sealed class CreateAccountingSalesOrderCommandValidator
    : AbstractValidator<CreateAccountingSalesOrderCommand>
{
    // Burada taslak satış oluşturma isteğinin idempotency, başlık, satır ve opsiyonel fatura tutarlılığını doğruluyorum.
    public CreateAccountingSalesOrderCommandValidator()
    {
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(AccountingSalesOrder.MaximumIdempotencyKeyLength)
            .Matches("^[A-Za-z0-9_-]+$");
        RuleFor(command => command.Header)
            .NotNull()
            .SetValidator(new AccountingSalesOrderHeaderInputValidator());
        RuleFor(command => command.Lines).NotNull().NotEmpty();
        RuleForEach(command => command.Lines).SetValidator(new AccountingSalesOrderLineInputValidator());
        RuleFor(command => command.Lines)
            .Must(HasUniqueLineNumbers)
            .When(command => command.Lines is not null)
            .WithMessage("Sales order line numbers must be unique.");
        RuleFor(command => command.Invoice!)
            .NotNull()
            .SetValidator(new SalesInvoiceHeaderInputValidator())
            .When(command => command.CreateInvoice);
        RuleFor(command => command.Invoice)
            .Null()
            .When(command => !command.CreateInvoice)
            .WithMessage("Invoice header is not allowed when CreateInvoice is false.");
    }

    // Burada sipariş satır numaralarının tekil olduğunu doğruluyorum.
    private static bool HasUniqueLineNumbers(IReadOnlyList<AccountingSalesOrderLineInput> lines)
    {
        return lines.Select(line => line.LineNumber).Distinct().Count() == lines.Count;
    }
}

public sealed class UpdateAccountingSalesOrderCommandValidator
    : AbstractValidator<UpdateAccountingSalesOrderCommand>
{
    // Burada taslak satışın toplu güncelleme sözleşmesini doğruluyorum.
    public UpdateAccountingSalesOrderCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Header)
            .NotNull()
            .SetValidator(new AccountingSalesOrderHeaderInputValidator());
        RuleFor(command => command.Lines).NotNull().NotEmpty();
        RuleForEach(command => command.Lines).SetValidator(new AccountingSalesOrderLineInputValidator());
        RuleFor(command => command.Lines)
            .Must(lines => lines.Select(line => line.LineNumber).Distinct().Count() == lines.Count)
            .When(command => command.Lines is not null)
            .WithMessage("Sales order line numbers must be unique.");
    }
}

public sealed class AddAccountingSalesOrderItemCommandValidator
    : AbstractValidator<AddAccountingSalesOrderItemCommand>
{
    // Burada taslağa satır ekleme isteğinin kimlik ve satır alanlarını doğruluyorum.
    public AddAccountingSalesOrderItemCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.Line)
            .NotNull()
            .SetValidator(new AccountingSalesOrderLineInputValidator());
    }
}

public sealed class UpdateAccountingSalesOrderItemCommandValidator
    : AbstractValidator<UpdateAccountingSalesOrderItemCommand>
{
    // Burada taslak satır güncelleme isteğinin iki kimliğini ve yalnız ticari alanlarını doğruluyorum.
    public UpdateAccountingSalesOrderItemCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
        RuleFor(command => command.Line)
            .NotNull()
            .SetValidator(new SalesInvoiceLineUpdateInputValidator());
    }
}

public sealed class RemoveAccountingSalesOrderItemCommandValidator
    : AbstractValidator<RemoveAccountingSalesOrderItemCommand>
{
    // Burada taslak satır silme isteğinin sipariş ve satır kimliklerini doğruluyorum.
    public RemoveAccountingSalesOrderItemCommandValidator()
    {
        RuleFor(command => command.OrderId).NotEmpty();
        RuleFor(command => command.ItemId).NotEmpty();
    }
}

public sealed class PostAccountingSalesOrderCommandValidator
    : AbstractValidator<PostAccountingSalesOrderCommand>
{
    // Burada post edilecek Accounting satış siparişinin kimliğini doğruluyorum.
    public PostAccountingSalesOrderCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class GetAccountingSalesOrdersQueryValidator
    : AbstractValidator<GetAccountingSalesOrdersQuery>
{
    // Burada Accounting satış siparişi listesinin güvenli sayfa sınırlarını doğruluyorum.
    public GetAccountingSalesOrdersQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class CreateSalesInvoiceFromOrderCommandValidator
    : AbstractValidator<CreateSalesInvoiceFromOrderCommand>
{
    // Burada mevcut siparişten fatura üretme isteğinin kimlik ve başlığını doğruluyorum.
    public CreateSalesInvoiceFromOrderCommandValidator()
    {
        RuleFor(command => command.AccountingSalesOrderId).NotEmpty();
        RuleFor(command => command.Header)
            .NotNull()
            .SetValidator(new SalesInvoiceHeaderInputValidator());
    }
}

public sealed class CreateDirectSalesInvoiceCommandValidator
    : AbstractValidator<CreateDirectSalesInvoiceCommand>
{
    // Burada doğrudan fatura girişinin idempotency, iki başlık ve satış satırı sözleşmesini doğruluyorum.
    public CreateDirectSalesInvoiceCommandValidator()
    {
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .MaximumLength(AccountingSalesOrder.MaximumIdempotencyKeyLength)
            .Matches("^[A-Za-z0-9_-]+$");
        RuleFor(command => command.OrderHeader)
            .NotNull()
            .SetValidator(new AccountingSalesOrderHeaderInputValidator());
        RuleFor(command => command.InvoiceHeader)
            .NotNull()
            .SetValidator(new SalesInvoiceHeaderInputValidator());
        RuleFor(command => command.Lines).NotNull().NotEmpty();
        RuleForEach(command => command.Lines).SetValidator(new AccountingSalesOrderLineInputValidator());
        RuleFor(command => command.Lines)
            .Must(lines => lines.Select(line => line.LineNumber).Distinct().Count() == lines.Count)
            .When(command => command.Lines is not null)
            .WithMessage("Sales order line numbers must be unique.");
    }
}

public sealed class UpdateSalesInvoiceCommandValidator
    : AbstractValidator<UpdateSalesInvoiceCommand>
{
    // Burada taslak faturanın başlık ve varsa tam satır listesi güncellemesini doğruluyorum.
    public UpdateSalesInvoiceCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Header)
            .NotNull()
            .SetValidator(new SalesInvoiceHeaderInputValidator());
        RuleFor(command => command.Lines)
            .Must(lines => lines is null || lines.Count > 0)
            .WithMessage("Sales invoice must contain at least one line when lines are supplied.");
        RuleForEach(command => command.Lines!)
            .SetValidator(new AccountingSalesOrderLineInputValidator())
            .When(command => command.Lines is not null);
        RuleFor(command => command.Lines)
            .Must(lines => lines is null || lines.Select(line => line.LineNumber).Distinct().Count() == lines.Count)
            .WithMessage("Sales invoice line numbers must be unique.");
    }
}

public sealed class AddSalesInvoiceLineCommandValidator :
    AbstractValidator<AddSalesInvoiceLineCommand>
{
    // Burada taslak faturaya yeni ürün satırı ekleme isteğinin kimlik ve tam satır sözleşmesini doğruluyorum.
    public AddSalesInvoiceLineCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
        RuleFor(command => command.Line)
            .NotNull()
            .SetValidator(new AccountingSalesOrderLineInputValidator());
    }
}

public sealed class UpdateSalesInvoiceLineCommandValidator :
    AbstractValidator<UpdateSalesInvoiceLineCommand>
{
    // Burada fatura satırı ticari güncellemesinin iki kimlik ve ürünsüz payload sınırını doğruluyorum.
    public UpdateSalesInvoiceLineCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
        RuleFor(command => command.LineId).NotEmpty();
        RuleFor(command => command.Line)
            .NotNull()
            .SetValidator(new SalesInvoiceLineUpdateInputValidator());
    }
}

public sealed class RemoveSalesInvoiceLineCommandValidator :
    AbstractValidator<RemoveSalesInvoiceLineCommand>
{
    // Burada taslak fatura satırı silme isteğinin fatura ve satır kimliklerini doğruluyorum.
    public RemoveSalesInvoiceLineCommandValidator()
    {
        RuleFor(command => command.InvoiceId).NotEmpty();
        RuleFor(command => command.LineId).NotEmpty();
    }
}

public sealed class PostSalesInvoiceCommandValidator : AbstractValidator<PostSalesInvoiceCommand>
{
    // Burada bağlı satış siparişi üzerinden post edilecek faturanın kimliğini doğruluyorum.
    public PostSalesInvoiceCommandValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
    }
}

public sealed class GetSalesInvoicesQueryValidator : AbstractValidator<GetSalesInvoicesQuery>
{
    // Burada iç satış faturası listesinin güvenli sayfa sınırlarını doğruluyorum.
    public GetSalesInvoicesQueryValidator()
    {
        RuleFor(query => query.PageNumber).InclusiveBetween(1, 10_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
