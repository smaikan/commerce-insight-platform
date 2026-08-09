# POST /api/guest-orders/{id}/cancel

Guest müşterinin yalnız Pending/Confirmed ve uzlaştırma beklemeyen siparişini iptal eder; mevcut `Cancellation` StockMovement ve kupon release akışını kullanır.

## İstek sözleşmesi

- Authorization: guest session grant.
- Cookie: `ecommerce_guest_orders`, `ecommerce_guest_csrf`.
- Header: trusted `Origin`, `X-Guest-CSRF`.
- Body: yok.

| Route | Required | Nullable | Kural |
| --- | --- | --- | --- |
| id | Evet | Hayır | Order GUID |

```http
POST /api/guest-orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/cancel
Origin: https://store.example.com
X-Guest-CSRF: <csrf>
Cookie: ecommerce_guest_orders=<session>; ecommerce_guest_csrf=<csrf>
```

Başarıda güncel `OrderDto` (`status=Cancelled`) döner; cookie değişmez. Stok doğrudan yazılmaz, pozitif cancellation hareketi oluşur ve guest kupon kullanımı OrderId üzerinden geri alınır.

## Hatalar, retry ve UI davranışı

- `400`: route biçimi geçersiz.
- `401 invalid_guest_access`: session geçersiz.
- `403 invalid_guest_access`: Origin/CSRF reddi.
- `404 not_found`: erişim grant'i olmayan sipariş dahil.
- `409 conflict`: durum iptale uygun değil veya ödeme uzlaştırması bekliyor.
- `428/429/503`: guest checkout koruması bu uçta çalışmaz.
- `500`: siparişi yeniden okuyarak sonucu doğrula; körlemesine tekrar etme.

UI işlem boyunca butonu kilitler, başarıda sipariş sorgusunu yeniler. Cookie/CSRF/PII loglanmaz.
