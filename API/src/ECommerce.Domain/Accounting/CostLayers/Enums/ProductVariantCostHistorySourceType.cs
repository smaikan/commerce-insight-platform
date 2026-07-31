namespace ECommerce.Domain.Accounting.CostLayers;

// Burada varyant maliyet geçmişini oluşturan gerçek belge veya açılış maliyeti kaynağını tanımlıyorum.
public enum ProductVariantCostHistorySourceType
{
    PurchaseInvoice = 1,
    OpeningBalance = 2
}
