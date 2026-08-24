# POST /api/guest-orders/{id}/cancel

Guest müşterinin session grant'i kapsamındaki `Pending`, `Confirmed`, `Paid` veya `Preparing` sipariş için güvenli iptal başlatır. Finansal karar ve `200/202/409` semantiği üye endpointiyle aynıdır.

## Yetki ve istek

- Cookie: `ecommerce_guest_orders`, `ecommerce_guest_csrf`.
- Header: trusted `Origin`, `X-Guest-CSRF`.
- `id`: zorunlu Order GUID.
- Body: yok.

```http
POST /api/guest-orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/cancel
Origin: https://store.example.com
X-Guest-CSRF: <csrf>
Cookie: ecommerce_guest_orders=<session>; ecommerce_guest_csrf=<csrf>
```

`Pending/Confirmed` sipariş mevcut CF-Retrieve ve abandoned-token akışını kullanır. `Paid/Preparing` sipariş aynı gün `/payment/cancel`, diğer durumda gerçek item transaction değerleriyle standart `/payment/refund` sagasına girer. Provider sonucu belirsizken yerel sipariş/ödeme/stok/kupon değiştirilmez.

## Responses

- `200 OK`: güncel `OrderDto`, `status=Cancelled (6)`; zaten tamamlanan replay de aynıdır.
- `202 Accepted`: `OrderCancellationOperationDto`; alanlar ve numeric enumlar üye cancel belgesindekiyle aynıdır. Guest polling URL'si `/api/guest-orders/{orderId}/cancellation` olur.
- `400 validation_error`: route biçimi.
- `401 invalid_guest_access`: session geçersiz.
- `403 invalid_guest_access`: Origin/CSRF reddi.
- `404 resource_not_found`: grant kapsamı dışında veya bulunamayan sipariş.
- `409 order_cancellation_not_allowed`: Shipped veya sonrası.
- `409 payment_reversal_data_missing`: güvenli refund verisi eksik.
- `409 payment_reversal_rejected`: kesin provider reddi.
- `409 payment_reversal_manual_review`: manuel finansal inceleme.
- `409 conflict`: diğer güvenli yaşam döngüsü çatışmaları.

`200` ve `202` JSON biçimleri üye endpoint belgesindekiyle aynıdır. `202` frontend başarısı değildir; polling başlatır. Mutation tamamlanınca sepet yeniden oluşturulmaz, guest cookie değişmez ve response PII/provider kimliği taşımaz.
