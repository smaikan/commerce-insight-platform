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

`ReturnType`: 0 Refund, 1 Exchange. Yalnız Delivered/ReturnRequested/ReturnApproved/Refunded siparişlerde açılır. `Refunded`, genel sipariş yaşam döngüsünde terminaldir; ancak kısmi iade desteği nedeniyle kalan uygun adetler için return workflow yeni talep açabilir. Aynı item aynı istekte tekrarlanamaz; toplam iade adedi kalan eligible adedi aşamaz. Exchange replacement aynı Product’a ait farklı, aktif, stoklu ve aynı net fiyatlı varyant olmalıdır.

Yeni admin yaşam döngüsü `Requested (0) → Received (3) → Approved (1) | Rejected (2)` biçimindedir. `receive` fiziksel teslimi UTC zamanıyla kaydeder; Order `ReturnRequested (8)` kalır ve stok değişmez. Teslim sonrası `Refund` onayı Order'ı ve GET sipariş cevaplarını `Refunded (7)` yapıp `SaleReturn` stok girişini aynı transaction'da oluşturur. Teslim sonrası `Exchange` onayı Order'ı `ReturnApproved (9)` yapıp iade stok girişini ve replacement stok çıkışını atomik uygular. Ret stok yazmaz ve Order durumunu diğer aktif taleplerden türetir. Bu durum değişimi ödeme sağlayıcısında otomatik para iadesi başlatmaz ve `Payment` durumunu değiştirmez.

`Completed (4)` ve `POST /api/returns/{id}/complete` yalnız deployment öncesindeki `Approved → Received` kayıtların bounded uyumluluğu için korunur; yeni kayıtlar complete adımına girmez. Eski `Received/Completed` kayıtlarda `ApprovedAt` bulunduğundan onay sonucu korunur ve sipariş durumu geriye düşmez. Geçersiz akış `409 return_status_transition_invalid`, gerçek eşzamanlı yazma yarışı `409 concurrency_conflict` döndürür.

Guest claim, ilgili return kayıtlarının UserId alanını aynı transaction’da günceller. Claim öncesi guest review/rating yapamaz; iade talebi oluşturabilmesi review hakkı vermez.
