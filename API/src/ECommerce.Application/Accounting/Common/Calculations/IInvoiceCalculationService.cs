namespace ECommerce.Application.Accounting.Common.Calculations;

// Burada alış ve satış faturalarının ortak saf hesaplama motorunu tanımlıyorum.
public interface IInvoiceCalculationService
{
    // Burada güvenilir ham girdilerden bütün satır ve başlık toplamlarını yeniden hesaplıyorum.
    InvoiceCalculationResult Calculate(InvoiceCalculationInput input);
}
