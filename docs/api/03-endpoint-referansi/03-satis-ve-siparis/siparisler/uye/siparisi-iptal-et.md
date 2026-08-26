# POST /api/orders/{id}/cancel

- Görev alanı: **Satış ve sipariş → Siparişler → Üye işlemleri**.

Üyenin sahibi olduğu `Pending`, `Confirmed`, `Paid` veya `Preparing` sipariş için güvenli iptal başlatır. Request body yoktur.

## Yetki ve istek

- Security: Bearer/JWT User.
- `id`: zorunlu Order GUID.
- Sahiplik taşınmayan/yok sipariş aynı `404 not_found` sonucunu verir.

```http
POST /api/orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/cancel
Authorization: Bearer <access-token>
```

## Durum ve provider davranışı

- `Pending/Confirmed`: mevcut CF-Retrieve, kesin failure ve abandoned-token mutabakatı korunur.
- `Paid/Preparing`: önce iyzico reporting sonucu doğrulanır. Türkiye iş tarihinde aynı günse `/payment/cancel`; aksi halde CF-Retrieve'da kalıcılaştırılmış gerçek item `paymentTransactionId/paidPrice` değerleriyle `/payment/refund` kullanılır.
- Refund V2 kullanılmaz. Provider toplamından tahminî kalem tutarı üretilmez.
- `Shipped` ve sonraki durumlar provider çağrısından önce reddedilir.
- Provider sonucu belirsizse Order, Payment, stok ve kupon değiştirilmez; `202` operasyonu worker tarafından uzlaştırılır.
- Zaten `Cancelled` sipariş idempotent olarak `200` döner.

## 200 OK

İptal tamamlandı veya daha önce tamamlanmışsa güncel `OrderDto` döner. `status=6` olur.

```json
{
  "id": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "orderNumber": "ORD-20260824-A1B2",
  "status": 6,
  "grandTotal": 110,
  "cancelledAt": "2026-08-24T07:20:00Z",
  "items": [],
  "payments": []
}
```

`OrderDto`nun diğer alanları ortak sipariş sözleşmesindeki şekilde bulunur; örnek yalnız iptal açısından belirleyici alanları gösterir.

## 202 Accepted

Provider sonucu mutabakat bekliyorsa:

```json
{
  "operationId": "3470e031-3fc8-42af-9755-f0fcae2b06cb",
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "status": 2,
  "reversalType": 0,
  "createdAt": "2026-08-24T07:19:00Z",
  "updatedAt": "2026-08-24T07:19:03Z",
  "nextAttemptAt": "2026-08-24T07:20:03Z",
  "pollingUrl": "/api/orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/cancellation"
}
```

`status`: `0 Requested`, `1 Processing`, `2 ReconciliationPending`, `3 Completed`, `4 Failed`, `5 ManualReview`. `reversalType`: `0 Cancel`, `1 Refund`. Tarihler UTC'dir; `nextAttemptAt` nullable'dır.

## Hatalar

| HTTP | `code` | Koşul |
| --- | --- | --- |
| `400` | `validation_error` | Path biçimi geçersiz. |
| `401` | `authentication_required` / `invalid_access_token` | JWT yok/geçersiz. |
| `404` | `resource_not_found` | Sipariş yok veya kullanıcıya ait değil. |
| `409` | `order_cancellation_not_allowed` | Sipariş Shipped veya daha ileri yaşam döngüsünde. |
| `409` | `payment_reversal_data_missing` | Güvenli cancel/refund için eski ödemede provider/item verisi eksik. |
| `409` | `payment_reversal_rejected` | Provider ters işlemi kesin reddetti. |
| `409` | `payment_reversal_manual_review` | Finansal bütünlük otomatik doğrulanamadı. |
| `409` | `conflict` | Ödeme/sipariş durumu güvenli iptal önkoşullarını taşımıyor. |

Frontend `200` sonucunu tamamlandı sayar. `202` sonucunda butonu sonsuza kadar loading bırakmaz; `pollingUrl` adresini kontrollü aralıklarla okur. `409` başarı sayılmaz ve sipariş yeniden okunur. Kör provider retry yapılmaz.
