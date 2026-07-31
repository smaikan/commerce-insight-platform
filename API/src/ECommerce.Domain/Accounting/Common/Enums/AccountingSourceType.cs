namespace ECommerce.Domain.Accounting.Common.Enums;

// Burada muhasebe kayıtlarının kesinleşmiş kaynak belge türlerini kalıcı sayısal değerlerle tanımlıyorum.
public enum AccountingSourceType
{
    PurchaseInvoice = 1,
    SalesInvoice = 2,
    AccountingSalesOrder = 3,
    Payment = 4,
    FinancialTransaction = 5
}
