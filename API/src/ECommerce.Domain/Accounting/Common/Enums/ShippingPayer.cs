namespace ECommerce.Domain.Accounting.Common.Enums;

// Burada satış belgesindeki kargo bedelini işletmenin mi müşterinin mi üstlendiğini tanımlıyorum.
public enum ShippingPayer
{
    None = 0,
    Seller = 1,
    Customer = 2
}
