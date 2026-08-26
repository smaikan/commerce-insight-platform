# POST /api/cart/coupon-preview

- Görev alanı: **Satış ve sipariş → Sepet → Sepet kuponu**.

Sepetteki kupon kodunu, sepeti değiştirmeden doğrular ve uygulanabilir indirim özetini döndürür.

## Yetki

**Public / guest cart.** JWT varsa kullanıcı sepeti, yoksa güvenli guest cart cookie bağlamı kullanılır. Cart rate limit politikası geçerlidir.

## Request body

| Alan | Tip | Zorunlu | Açıklama |
| --- | --- | --- | --- |
| `couponCode` | string | Evet | Doğrulanacak kupon kodu |

```json
{
  "couponCode": "YAZ20"
}
```

## Başarılı response — 200 OK

```json
{
  "code": "YAZ20",
  "discountTotal": 259.98,
  "discountType": 1
}
```

`discountType` sayısal `CouponDiscountType` değeridir. Response yalnız önizlemedir; kupon sipariş oluşturulana kadar kesin kullanılmış sayılmaz.

## Hatalar

| HTTP | Durum |
| --- | --- |
| `400` | Kod biçimi veya request geçersiz |
| `404` | Kupon bulunamadı |
| `409` | Kupon sepet, müşteri, tarih veya kullanım koşullarına uygun değil |
| `429` | Cart rate limit aşıldı |

