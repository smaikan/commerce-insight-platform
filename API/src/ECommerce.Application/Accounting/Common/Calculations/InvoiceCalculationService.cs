using ECommerce.Domain.Accounting.Common.Enums;
using ECommerce.Domain.Common;

namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada alış ve satış faturalarının bütün indirim, KDV ve başlık toplamlarını tek yerde hesaplıyorum.
public sealed class InvoiceCalculationService : IInvoiceCalculationService
{
    private readonly IAccountingRoundingPolicy _roundingPolicy;

    // Burada hesap motorunu ortak yuvarlama politikasıyla hazırlıyorum.
    public InvoiceCalculationService(IAccountingRoundingPolicy roundingPolicy)
    {
        _roundingPolicy = roundingPolicy ?? throw new ArgumentNullException(nameof(roundingPolicy));
    }

    // Burada ham fatura girdilerini doğrulayıp bütün güvenilir sonuçları yeniden üretiyorum.
    public InvoiceCalculationResult Calculate(InvoiceCalculationInput input)
    {
        try
        {
            return CalculateCore(input);
        }
        catch (OverflowException exception)
        {
            throw new DomainException("Invoice calculation exceeds the supported decimal range.", exception);
        }
    }

    // Burada taşma hatalarını dış sınırda yönetebilmek için hesap adımlarını tek çekirdekte çalıştırıyorum.
    private InvoiceCalculationResult CalculateCore(InvoiceCalculationInput input)
    {
        var orderedLines = ValidateAndOrderInput(input);
        var lineStates = orderedLines
            .Select(PrepareLine)
            .ToArray();

        ApplyInvoiceDiscount(lineStates, input.InvoiceDiscount);

        var lineResults = lineStates
            .Select(CreateLineResult)
            .ToArray();
        var readOnlyLines = Array.AsReadOnly(lineResults);
        var totals = CreateTotals(readOnlyLines);

        EnsureHeaderInvariants(readOnlyLines, totals);
        return new InvoiceCalculationResult(readOnlyLines, totals);
    }

    // Burada fatura satırlarını zorunluluk, tekillik ve kararlı satır sırasına göre doğruluyorum.
    private InvoiceLineCalculationInput[] ValidateAndOrderInput(InvoiceCalculationInput input)
    {
        if (input is null)
        {
            throw new DomainException("Invoice calculation input is required.");
        }

        if (input.Lines is null || input.Lines.Count == 0)
        {
            throw new DomainException("Invoice calculation requires at least one line.");
        }

        var orderedLines = input.Lines
            .OrderBy(line => line?.LineNumber ?? int.MinValue)
            .ToArray();

        if (orderedLines.Any(line => line is null))
        {
            throw new DomainException("Invoice calculation lines cannot contain null values.");
        }

        for (var index = 0; index < orderedLines.Length; index++)
        {
            var line = orderedLines[index];
            if (line.LineNumber <= 0)
            {
                throw new DomainException("Invoice line number must be greater than zero.");
            }

            if (index > 0 && orderedLines[index - 1].LineNumber == line.LineNumber)
            {
                throw new DomainException("Invoice line numbers must be unique.");
            }
        }

        if (input.InvoiceDiscount is not null)
        {
            ValidateDiscountDefinition(input.InvoiceDiscount, DiscountScope.Invoice);
        }

        return orderedLines;
    }

    // Burada tek satırın miktar, fiyat, vergi ve satır indirimi ara sonuçlarını hazırlıyorum.
    private LineState PrepareLine(InvoiceLineCalculationInput line)
    {
        EnsureDefinedEnum(line.PriceEntryMode, nameof(line.PriceEntryMode));

        var quantity = NormalizePositiveQuantity(line.Quantity, nameof(line.Quantity));
        var unitsPerUnit = NormalizePositiveQuantity(line.UnitsPerUnit, nameof(line.UnitsPerUnit));
        var enteredUnitPrice = NormalizeNonNegativeUnitPrice(
            line.EnteredUnitPrice,
            nameof(line.EnteredUnitPrice));
        var vatRate = NormalizePercentage(line.VatRate, nameof(line.VatRate));
        var stockQuantity = _roundingPolicy.RoundQuantity(checked(quantity * unitsPerUnit));
        if (stockQuantity <= 0m)
        {
            throw new DomainException("Stock quantity must be greater than zero after rounding.");
        }

        if (line.LineDiscount is not null)
        {
            ValidateDiscountDefinition(line.LineDiscount, DiscountScope.Line);
        }

        var vatFactor = checked(1m + (vatRate / 100m));
        var (unitPrices, grossAmounts) = CalculateUnitAndGrossAmounts(
            quantity,
            enteredUnitPrice,
            line.PriceEntryMode,
            vatFactor);
        var lineDiscountApplication = CalculateLineDiscount(
            line.LineDiscount,
            quantity,
            stockQuantity,
            grossAmounts,
            vatFactor);

        return new LineState(
            line.LineNumber,
            line.IsInvoiceDiscountEligible,
            stockQuantity,
            vatFactor,
            unitPrices,
            grossAmounts,
            lineDiscountApplication.Discount,
            lineDiscountApplication.Remaining);
    }

    // Burada fiyat giriş moduna göre birim ve brüt tutarların iki KDV karşılığını hesaplıyorum.
    private (AmountPair UnitPrices, AmountPair GrossAmounts) CalculateUnitAndGrossAmounts(
        decimal quantity,
        decimal enteredUnitPrice,
        PriceEntryMode priceEntryMode,
        decimal vatFactor)
    {
        if (priceEntryMode == PriceEntryMode.ExcludingVat)
        {
            var unitPriceExcludingVat = enteredUnitPrice;
            var unitPriceIncludingVat = _roundingPolicy.RoundUnitPrice(
                checked(unitPriceExcludingVat * vatFactor));
            var grossExcludingVat = _roundingPolicy.RoundMoney(
                checked(quantity * unitPriceExcludingVat));
            var grossIncludingVat = _roundingPolicy.RoundMoney(
                checked(grossExcludingVat * vatFactor));
            return (
                new AmountPair(unitPriceExcludingVat, unitPriceIncludingVat),
                new AmountPair(grossExcludingVat, grossIncludingVat));
        }

        var enteredPriceIncludingVat = enteredUnitPrice;
        var calculatedPriceExcludingVat = _roundingPolicy.RoundUnitPrice(
            enteredPriceIncludingVat / vatFactor);
        var calculatedGrossIncludingVat = _roundingPolicy.RoundMoney(
            checked(quantity * enteredPriceIncludingVat));
        var calculatedGrossExcludingVat = _roundingPolicy.RoundMoney(
            calculatedGrossIncludingVat / vatFactor);
        return (
            new AmountPair(calculatedPriceExcludingVat, enteredPriceIncludingVat),
            new AmountPair(calculatedGrossExcludingVat, calculatedGrossIncludingVat));
    }

    // Burada opsiyonel satır indiriminin seçilen KDV bazındaki tutarını ve kalan tutarı hesaplıyorum.
    private DiscountApplication CalculateLineDiscount(
        DiscountCalculationInput? discount,
        decimal quantity,
        decimal stockQuantity,
        AmountPair grossAmounts,
        decimal vatFactor)
    {
        if (discount is null)
        {
            return new DiscountApplication(AmountPair.Zero, grossAmounts);
        }

        var selectedBase = GetSelectedAmount(grossAmounts, discount.TaxBasis);
        var selectedDiscount = discount.Type switch
        {
            DiscountType.Percentage => CalculatePercentageAmount(selectedBase, discount.Value),
            DiscountType.FixedPerUnit => CalculateFixedPerUnitAmount(
                discount,
                quantity,
                stockQuantity,
                selectedBase),
            DiscountType.FixedLineTotal => discount.Value,
            _ => throw new DomainException("Line discount type is not supported.")
        };

        if (selectedDiscount > selectedBase)
        {
            throw new DomainException("Line discount cannot exceed its applicable base.");
        }

        return ApplySelectedDiscount(
            grossAmounts,
            selectedDiscount,
            discount.TaxBasis,
            vatFactor);
    }

    // Burada birim başına indirimi belge veya stok miktarına göre iki ondalıklı tutara çeviriyorum.
    private decimal CalculateFixedPerUnitAmount(
        DiscountCalculationInput discount,
        decimal quantity,
        decimal stockQuantity,
        decimal selectedBase)
    {
        var multiplier = discount.UnitBasis switch
        {
            DiscountUnitBasis.PurchaseUnit => quantity,
            DiscountUnitBasis.SaleUnit => quantity,
            DiscountUnitBasis.StockUnit => stockQuantity,
            _ => throw new DomainException("Fixed-per-unit discount requires a valid unit basis.")
        };

        var rawDiscount = checked(discount.Value * multiplier);
        if (rawDiscount > selectedBase)
        {
            throw new DomainException("Fixed-per-unit discount cannot exceed its applicable base.");
        }

        return _roundingPolicy.RoundMoney(rawDiscount);
    }

    // Burada yüzde indirimin seçilen tabandaki iki ondalıklı parasal karşılığını hesaplıyorum.
    private decimal CalculatePercentageAmount(decimal selectedBase, decimal percentage)
    {
        if (percentage == 100m)
        {
            return selectedBase;
        }

        return _roundingPolicy.RoundMoney(checked(selectedBase / 100m * percentage));
    }

    // Burada fatura indirimini satır indirimi sonrası uygun tutarlara kararlı biçimde dağıtıyorum.
    private void ApplyInvoiceDiscount(
        IReadOnlyList<LineState> lineStates,
        DiscountCalculationInput? invoiceDiscount)
    {
        if (invoiceDiscount is null)
        {
            return;
        }

        if (invoiceDiscount.Value == 0m)
        {
            return;
        }

        var eligibleLines = lineStates
            .Where(line =>
                line.IsInvoiceDiscountEligible &&
                GetSelectedAmount(line.AmountsAfterLineDiscount, invoiceDiscount.TaxBasis) > 0m)
            .ToArray();
        if (eligibleLines.Length == 0)
        {
            throw new DomainException("Invoice discount requires a positive eligible line base.");
        }

        var eligibleBaseTotal = SumChecked(
            eligibleLines,
            line => GetSelectedAmount(line.AmountsAfterLineDiscount, invoiceDiscount.TaxBasis));
        var targetDiscount = CalculateInvoiceDiscountTarget(
            invoiceDiscount,
            eligibleBaseTotal);
        if (targetDiscount > eligibleBaseTotal)
        {
            throw new DomainException("Invoice discount cannot exceed the eligible invoice base.");
        }

        DistributeInvoiceDiscount(
            eligibleLines,
            invoiceDiscount,
            targetDiscount,
            eligibleBaseTotal);
    }

    // Burada yüzde veya sabit fatura indiriminin seçilen KDV bazındaki hedef tutarını hesaplıyorum.
    private decimal CalculateInvoiceDiscountTarget(
        DiscountCalculationInput invoiceDiscount,
        decimal eligibleBaseTotal)
    {
        return invoiceDiscount.Type switch
        {
            DiscountType.Percentage => CalculatePercentageAmount(
                eligibleBaseTotal,
                invoiceDiscount.Value),
            DiscountType.FixedInvoiceTotal => invoiceDiscount.Value,
            _ => throw new DomainException("Invoice discount type is not supported.")
        };
    }

    // Burada hedef fatura indirimini oransal dağıtıp kalan kuruş farkını son uygun satıra veriyorum.
    private void DistributeInvoiceDiscount(
        IReadOnlyList<LineState> eligibleLines,
        DiscountCalculationInput invoiceDiscount,
        decimal targetDiscount,
        decimal eligibleBaseTotal)
    {
        var lineBases = eligibleLines
            .Select(line => GetSelectedAmount(
                line.AmountsAfterLineDiscount,
                invoiceDiscount.TaxBasis))
            .ToArray();
        var shares = new decimal[eligibleLines.Count];
        for (var index = 0; index < shares.Length; index++)
        {
            var rawShare = invoiceDiscount.Type == DiscountType.Percentage
                ? checked(lineBases[index] / 100m * invoiceDiscount.Value)
                : checked(targetDiscount / eligibleBaseTotal * lineBases[index]);
            shares[index] = Math.Min(
                _roundingPolicy.RoundMoney(rawShare),
                lineBases[index]);
        }

        ReconcileInvoiceDiscountShares(shares, lineBases, targetDiscount);

        for (var index = 0; index < eligibleLines.Count; index++)
        {
            var line = eligibleLines[index];
            var application = ApplySelectedDiscount(
                line.AmountsAfterLineDiscount,
                shares[index],
                invoiceDiscount.TaxBasis,
                line.VatFactor);
            line.InvoiceDiscount = application.Discount;
            line.FinalAmounts = application.Remaining;
        }
    }

    // Burada oransal yuvarlama farkını son uygun satırdan geriye doğru kapasiteyi aşmadan uzlaştırıyorum.
    private static void ReconcileInvoiceDiscountShares(
        IList<decimal> shares,
        IReadOnlyList<decimal> lineBases,
        decimal targetDiscount)
    {
        var difference = checked(targetDiscount - SumChecked(shares, share => share));
        if (difference > 0m)
        {
            for (var index = shares.Count - 1; index >= 0 && difference > 0m; index--)
            {
                var availableCapacity = checked(lineBases[index] - shares[index]);
                var addition = Math.Min(difference, availableCapacity);
                shares[index] = checked(shares[index] + addition);
                difference = checked(difference - addition);
            }
        }
        else if (difference < 0m)
        {
            var excess = -difference;
            for (var index = shares.Count - 1; index >= 0 && excess > 0m; index--)
            {
                var reduction = Math.Min(excess, shares[index]);
                shares[index] = checked(shares[index] - reduction);
                excess = checked(excess - reduction);
            }

            difference = -excess;
        }

        if (difference != 0m)
        {
            throw new DomainException("Invoice discount distribution could not be completed.");
        }
    }

    // Burada seçilen KDV bazındaki indirimi karşı baza çevirip her iki kalan tutarı birlikte koruyorum.
    private DiscountApplication ApplySelectedDiscount(
        AmountPair baseAmounts,
        decimal selectedDiscount,
        DiscountTaxBasis taxBasis,
        decimal vatFactor)
    {
        var selectedBase = GetSelectedAmount(baseAmounts, taxBasis);
        if (selectedDiscount < 0m || selectedDiscount > selectedBase)
        {
            throw new DomainException("Discount must remain within its applicable base.");
        }

        if (selectedDiscount == 0m)
        {
            return new DiscountApplication(AmountPair.Zero, baseAmounts);
        }

        if (selectedDiscount == selectedBase)
        {
            return new DiscountApplication(baseAmounts, AmountPair.Zero);
        }

        DiscountApplication result;
        if (taxBasis == DiscountTaxBasis.ExcludingVat)
        {
            var remainingExcludingVat = checked(baseAmounts.ExcludingVat - selectedDiscount);
            var remainingIncludingVat = _roundingPolicy.RoundMoney(
                checked(remainingExcludingVat * vatFactor));
            var discountIncludingVat = checked(
                baseAmounts.IncludingVat - remainingIncludingVat);
            result = new DiscountApplication(
                new AmountPair(selectedDiscount, discountIncludingVat),
                new AmountPair(remainingExcludingVat, remainingIncludingVat));
        }
        else
        {
            var remainingIncludingVat = checked(baseAmounts.IncludingVat - selectedDiscount);
            var remainingExcludingVat = _roundingPolicy.RoundMoney(
                remainingIncludingVat / vatFactor);
            var discountExcludingVat = checked(
                baseAmounts.ExcludingVat - remainingExcludingVat);
            result = new DiscountApplication(
                new AmountPair(discountExcludingVat, selectedDiscount),
                new AmountPair(remainingExcludingVat, remainingIncludingVat));
        }

        EnsureNonNegativeAmounts(result.Discount, "Calculated discount");
        EnsureNonNegativeAmounts(result.Remaining, "Calculated net amount");
        return result;
    }

    // Burada tek satırın bütün indirim, net ve KDV sonuçlarını değişmez sonuç modeline dönüştürüyorum.
    private InvoiceLineCalculationResult CreateLineResult(LineState state)
    {
        var totalDiscountExcludingVat = checked(
            state.LineDiscount.ExcludingVat + state.InvoiceDiscount.ExcludingVat);
        var totalDiscountIncludingVat = checked(
            state.LineDiscount.IncludingVat + state.InvoiceDiscount.IncludingVat);
        var vatAmount = checked(
            state.FinalAmounts.IncludingVat - state.FinalAmounts.ExcludingVat);

        var result = new InvoiceLineCalculationResult(
            state.LineNumber,
            state.StockQuantity,
            state.UnitPrices.ExcludingVat,
            state.UnitPrices.IncludingVat,
            state.GrossAmounts.ExcludingVat,
            state.GrossAmounts.IncludingVat,
            state.LineDiscount.ExcludingVat,
            state.LineDiscount.IncludingVat,
            state.InvoiceDiscount.ExcludingVat,
            state.InvoiceDiscount.IncludingVat,
            totalDiscountExcludingVat,
            totalDiscountIncludingVat,
            state.FinalAmounts.ExcludingVat,
            vatAmount,
            state.FinalAmounts.IncludingVat);

        EnsureLineInvariants(result);
        return result;
    }

    // Burada fatura başlık toplamlarını yalnız yuvarlanmış satır sonuçlarını toplayarak oluşturuyorum.
    private static InvoiceTotals CreateTotals(
        IReadOnlyList<InvoiceLineCalculationResult> lines)
    {
        return new InvoiceTotals(
            SumChecked(lines, line => line.GrossAmountExcludingVat),
            SumChecked(lines, line => line.GrossAmountIncludingVat),
            SumChecked(lines, line => line.LineDiscountAmountExcludingVat),
            SumChecked(lines, line => line.LineDiscountAmountIncludingVat),
            SumChecked(lines, line => line.InvoiceDiscountShareExcludingVat),
            SumChecked(lines, line => line.InvoiceDiscountShareIncludingVat),
            SumChecked(lines, line => line.TotalDiscountAmountExcludingVat),
            SumChecked(lines, line => line.TotalDiscountAmountIncludingVat),
            SumChecked(lines, line => line.NetAmountExcludingVat),
            SumChecked(lines, line => line.VatAmount),
            SumChecked(lines, line => line.TotalAmountIncludingVat));
    }

    // Burada satır sonucundaki brüt, indirim, net ve KDV eşitliklerinin bozulmadığını doğruluyorum.
    private static void EnsureLineInvariants(InvoiceLineCalculationResult line)
    {
        if (line.NetAmountExcludingVat < 0m ||
            line.TotalAmountIncludingVat < 0m ||
            line.VatAmount < 0m)
        {
            throw new DomainException("Calculated invoice line amounts cannot be negative.");
        }

        if (line.TotalDiscountAmountExcludingVat !=
            checked(line.LineDiscountAmountExcludingVat + line.InvoiceDiscountShareExcludingVat) ||
            line.TotalDiscountAmountIncludingVat !=
            checked(line.LineDiscountAmountIncludingVat + line.InvoiceDiscountShareIncludingVat) ||
            line.NetAmountExcludingVat !=
            checked(line.GrossAmountExcludingVat - line.TotalDiscountAmountExcludingVat) ||
            line.TotalAmountIncludingVat !=
            checked(line.GrossAmountIncludingVat - line.TotalDiscountAmountIncludingVat) ||
            line.VatAmount !=
            checked(line.TotalAmountIncludingVat - line.NetAmountExcludingVat))
        {
            throw new DomainException("Calculated invoice line totals are inconsistent.");
        }
    }

    // Burada başlık toplamlarının satır toplamları ve temel fatura eşitlikleriyle birebir uyuştuğunu doğruluyorum.
    private static void EnsureHeaderInvariants(
        IReadOnlyList<InvoiceLineCalculationResult> lines,
        InvoiceTotals totals)
    {
        if (totals.SubtotalExcludingVat !=
            SumChecked(lines, line => line.GrossAmountExcludingVat) ||
            totals.SubtotalIncludingVat !=
            SumChecked(lines, line => line.GrossAmountIncludingVat) ||
            totals.LineDiscountTotalExcludingVat !=
            SumChecked(lines, line => line.LineDiscountAmountExcludingVat) ||
            totals.LineDiscountTotalIncludingVat !=
            SumChecked(lines, line => line.LineDiscountAmountIncludingVat) ||
            totals.InvoiceDiscountTotalExcludingVat !=
            SumChecked(lines, line => line.InvoiceDiscountShareExcludingVat) ||
            totals.InvoiceDiscountTotalIncludingVat !=
            SumChecked(lines, line => line.InvoiceDiscountShareIncludingVat) ||
            totals.TotalDiscountExcludingVat !=
            checked(totals.LineDiscountTotalExcludingVat +
                totals.InvoiceDiscountTotalExcludingVat) ||
            totals.TotalDiscountIncludingVat !=
            checked(totals.LineDiscountTotalIncludingVat +
                totals.InvoiceDiscountTotalIncludingVat) ||
            totals.NetAmountExcludingVat !=
            checked(totals.SubtotalExcludingVat - totals.TotalDiscountExcludingVat) ||
            totals.VatTotal !=
            SumChecked(lines, line => line.VatAmount) ||
            totals.GrandTotalIncludingVat !=
            checked(totals.NetAmountExcludingVat + totals.VatTotal) ||
            totals.GrandTotalIncludingVat !=
            SumChecked(lines, line => line.TotalAmountIncludingVat))
        {
            throw new DomainException("Calculated invoice header totals are inconsistent.");
        }
    }

    // Burada indirim tanımının scope, tür, KDV bazı, birim bazı ve değer kurallarını doğruluyorum.
    private void ValidateDiscountDefinition(
        DiscountCalculationInput discount,
        DiscountScope expectedScope)
    {
        EnsureDefinedEnum(discount.Scope, nameof(discount.Scope));
        EnsureDefinedEnum(discount.Type, nameof(discount.Type));
        EnsureDefinedEnum(discount.TaxBasis, nameof(discount.TaxBasis));
        if (discount.UnitBasis.HasValue)
        {
            EnsureDefinedEnum(discount.UnitBasis.Value, nameof(discount.UnitBasis));
        }

        if (discount.Scope != expectedScope)
        {
            throw new DomainException("Discount scope does not match its calculation level.");
        }

        if (discount.Value < 0m)
        {
            throw new DomainException("Discount value cannot be negative.");
        }

        if (expectedScope == DiscountScope.Line)
        {
            ValidateLineDiscountType(discount);
        }
        else
        {
            ValidateInvoiceDiscountType(discount);
        }

        ValidateDiscountValuePrecision(discount);
    }

    // Burada satır indiriminin yalnız desteklenen tür ve birim bazı birleşimlerini kullandığını doğruluyorum.
    private static void ValidateLineDiscountType(DiscountCalculationInput discount)
    {
        if (discount.Type == DiscountType.FixedInvoiceTotal)
        {
            throw new DomainException("Fixed invoice total discount cannot be used on a line.");
        }

        if (discount.Type == DiscountType.FixedPerUnit && !discount.UnitBasis.HasValue)
        {
            throw new DomainException("Fixed-per-unit discount requires a unit basis.");
        }

        if (discount.Type != DiscountType.FixedPerUnit && discount.UnitBasis.HasValue)
        {
            throw new DomainException("Discount unit basis is only valid for fixed-per-unit discounts.");
        }
    }

    // Burada fatura indiriminin yalnız yüzde veya sabit fatura toplamı türünde olduğunu doğruluyorum.
    private static void ValidateInvoiceDiscountType(DiscountCalculationInput discount)
    {
        if (discount.Type is not DiscountType.Percentage and not DiscountType.FixedInvoiceTotal)
        {
            throw new DomainException("Invoice discount type is not supported.");
        }

        if (discount.UnitBasis.HasValue)
        {
            throw new DomainException("Invoice discount cannot define a unit basis.");
        }
    }

    // Burada indirim değerinin türüne uygun hassasiyet ve yüzde sınırında olduğunu doğruluyorum.
    private void ValidateDiscountValuePrecision(DiscountCalculationInput discount)
    {
        switch (discount.Type)
        {
            case DiscountType.Percentage:
                NormalizePercentage(discount.Value, nameof(discount.Value));
                break;
            case DiscountType.FixedPerUnit:
                NormalizeNonNegativeUnitPrice(discount.Value, nameof(discount.Value));
                break;
            case DiscountType.FixedLineTotal:
            case DiscountType.FixedInvoiceTotal:
                NormalizeNonNegativeMoney(discount.Value, nameof(discount.Value));
                break;
            default:
                throw new DomainException("Discount type is not supported.");
        }
    }

    // Burada pozitif miktarı ortak hassasiyete uygun biçimde doğruluyorum.
    private decimal NormalizePositiveQuantity(decimal value, string fieldName)
    {
        if (value <= 0m)
        {
            throw new DomainException($"{fieldName} must be greater than zero.");
        }

        var rounded = _roundingPolicy.RoundQuantity(value);
        if (rounded != value)
        {
            throw new DomainException($"{fieldName} exceeds the supported quantity precision.");
        }

        return rounded;
    }

    // Burada negatif olmayan birim fiyatı ortak hassasiyete uygun biçimde doğruluyorum.
    private decimal NormalizeNonNegativeUnitPrice(decimal value, string fieldName)
    {
        if (value < 0m)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        var rounded = _roundingPolicy.RoundUnitPrice(value);
        if (rounded != value)
        {
            throw new DomainException($"{fieldName} exceeds the supported unit-price precision.");
        }

        return rounded;
    }

    // Burada negatif olmayan yüzdeyi dört ondalıklı hassasiyete uygun biçimde doğruluyorum.
    private decimal NormalizeNonNegativePercentage(decimal value, string fieldName)
    {
        if (value < 0m)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        var rounded = _roundingPolicy.RoundPercentage(value);
        if (rounded != value)
        {
            throw new DomainException($"{fieldName} exceeds the supported percentage precision.");
        }

        return rounded;
    }

    // Burada yüzde indirimin sıfır ile yüz aralığında ve ortak hassasiyette olduğunu doğruluyorum.
    private decimal NormalizePercentage(decimal value, string fieldName)
    {
        if (value < 0m || value > 100m)
        {
            throw new DomainException($"{fieldName} must be between zero and one hundred.");
        }

        return NormalizeNonNegativePercentage(value, fieldName);
    }

    // Burada negatif olmayan sabit tutarı iki ondalıklı para hassasiyetinde doğruluyorum.
    private decimal NormalizeNonNegativeMoney(decimal value, string fieldName)
    {
        if (value < 0m)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }

        var rounded = _roundingPolicy.RoundMoney(value);
        if (rounded != value)
        {
            throw new DomainException($"{fieldName} exceeds the supported money precision.");
        }

        return rounded;
    }

    // Burada enum girdisinin tanımlı bir değer olduğunu doğruluyorum.
    private static void EnsureDefinedEnum<TEnum>(TEnum value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(typeof(TEnum), value))
        {
            throw new DomainException($"{fieldName} is invalid.");
        }
    }

    // Burada seçilen KDV bazına karşılık gelen tutarı döndürüyorum.
    private static decimal GetSelectedAmount(
        AmountPair amounts,
        DiscountTaxBasis taxBasis)
    {
        return taxBasis switch
        {
            DiscountTaxBasis.ExcludingVat => amounts.ExcludingVat,
            DiscountTaxBasis.IncludingVat => amounts.IncludingVat,
            _ => throw new DomainException("Discount tax basis is invalid.")
        };
    }

    // Burada hesaplanan iki KDV karşılığının negatif olmadığını doğruluyorum.
    private static void EnsureNonNegativeAmounts(AmountPair amounts, string fieldName)
    {
        if (amounts.ExcludingVat < 0m || amounts.IncludingVat < 0m)
        {
            throw new DomainException($"{fieldName} cannot be negative.");
        }
    }

    // Burada parasal değerleri decimal taşmasına karşı denetimli biçimde topluyorum.
    private static decimal SumChecked<T>(
        IEnumerable<T> values,
        Func<T, decimal> selector)
    {
        decimal total = 0m;
        foreach (var value in values)
        {
            total = checked(total + selector(value));
        }

        return total;
    }

    // Burada aynı tutarın KDV hariç ve KDV dahil karşılıklarını birlikte taşıyorum.
    private readonly record struct AmountPair(
        decimal ExcludingVat,
        decimal IncludingVat)
    {
        public static AmountPair Zero => new(0m, 0m);
    }

    // Burada bir indirim uygulamasının indirim ve kalan tutar çiftlerini birlikte taşıyorum.
    private readonly record struct DiscountApplication(
        AmountPair Discount,
        AmountPair Remaining);

    // Burada dağıtım sırasında tek satırın güvenilir ara hesap durumunu tutuyorum.
    private sealed class LineState
    {
        // Burada satırın sabit hesaplarını ve dağıtım öncesi kalan tutarını hazırlıyorum.
        public LineState(
            int lineNumber,
            bool isInvoiceDiscountEligible,
            decimal stockQuantity,
            decimal vatFactor,
            AmountPair unitPrices,
            AmountPair grossAmounts,
            AmountPair lineDiscount,
            AmountPair amountsAfterLineDiscount)
        {
            LineNumber = lineNumber;
            IsInvoiceDiscountEligible = isInvoiceDiscountEligible;
            StockQuantity = stockQuantity;
            VatFactor = vatFactor;
            UnitPrices = unitPrices;
            GrossAmounts = grossAmounts;
            LineDiscount = lineDiscount;
            AmountsAfterLineDiscount = amountsAfterLineDiscount;
            InvoiceDiscount = AmountPair.Zero;
            FinalAmounts = amountsAfterLineDiscount;
        }

        public int LineNumber { get; }
        public bool IsInvoiceDiscountEligible { get; }
        public decimal StockQuantity { get; }
        public decimal VatFactor { get; }
        public AmountPair UnitPrices { get; }
        public AmountPair GrossAmounts { get; }
        public AmountPair LineDiscount { get; }
        public AmountPair AmountsAfterLineDiscount { get; }
        public AmountPair InvoiceDiscount { get; set; }
        public AmountPair FinalAmounts { get; set; }
    }
}
