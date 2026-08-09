# İade ve Değişim API’leri

Üye ve guest aynı ReturnRequest domain kurallarını kullanır. Üye endpointleri JWT owner filtresi, guest endpointleri session→Order grant zinciri kullanır. Guest `ReturnRequest.UserId=null` olabilir; claim sırasında UserId atomik atanır.

## Müşteri endpointleri

| Akış | Üye | Guest |
| --- | --- | --- |
| Oluştur | `POST /api/returns` | `POST /api/guest-orders/{orderId}/returns` |
| Liste | `GET /api/returns/mine` | `GET /api/guest-orders/{orderId}/returns` |
| Detay | `GET /api/returns/{id}` | `GET /api/guest-orders/{orderId}/returns/{returnId}` |

Guest POST session cookie, `ecommerce_guest_csrf`→`X-Guest-CSRF` ve güvenilir Origin gerektirir. GET cevapları no-store’dur. Başka order/return erişimi 404’tür.

```json
{
  "type": 0,
  "items": [{
    "orderItemId": "c6b34cd2-...",
    "quantity": 1,
    "replacementProductVariantId": null
  }],
  "customerNote": "Ürün hasarlı geldi"
}
```

`ReturnType`: 0 Refund, 1 Exchange. Yalnız Delivered/ReturnRequested/ReturnApproved siparişlerde açılır. Aynı item aynı istekte tekrarlanamaz; toplam iade adedi kalan eligible adedi aşamaz. Exchange replacement aynı Product’a ait farklı, aktif, stoklu ve aynı net fiyatlı varyant olmalıdır.

Admin yaşam döngüsü değişmez: Requested → Approved/Rejected → Received → Completed. ReturnRequested/ReturnApproved Order durumları yalnız iade akışıyla set edilir. Receive/complete mevcut SaleReturn/transfer kurallarını ve StockMovement ledger’ını kullanır; frontend doğrudan stok yazmaz.

Guest claim, ilgili return kayıtlarının UserId alanını aynı transaction’da günceller. Claim öncesi guest review/rating yapamaz; iade talebi oluşturabilmesi review hakkı vermez.
