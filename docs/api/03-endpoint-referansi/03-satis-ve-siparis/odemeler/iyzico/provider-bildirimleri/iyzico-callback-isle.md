# POST /api/payments/iyzico/callback

- Görev alanı: **Satış ve sipariş → Ödemeler → iyzico → Provider bildirimleri**.

- Yetki: **Provider**.
- Çağıran: iyzico CheckoutForm tarayıcı dönüşü.
- Content-Type: `application/x-www-form-urlencoded`.
- Form: zorunlu `token`.

API token ile iyzico retrieve yapar; imza/tutar/kimlik doğrulamasından sonra `303 See Other` ile configuration'daki ReturnUrl'e yönlendirir. Taksitli ödemede iyzico'nun taksit farkı dahil `paidPrice` değeri sipariş toplamından yüksek olabilir; API bu gerçek tahsilatı ve taksit sayısını ayrı saklar, basket fiyatının ve mağaza sipariş toplamının düşürülmesine izin vermez:

```text
/checkout/payment-result?paymentId=<uuid>&orderId=<uuid>&status=Paid
```

Query yalnız ekran ipucudur; frontend sahiplik korumalı Order GET cevabını yeniden okumalıdır. Hatalar: `400`, `404`, `409`. Provider tokenı response/loga çıkmaz.
