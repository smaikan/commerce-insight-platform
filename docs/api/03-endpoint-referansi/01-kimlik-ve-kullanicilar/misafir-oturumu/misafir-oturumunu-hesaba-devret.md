# POST /api/guest-session/claim

- Görev alanı: **Kimlik ve kullanıcılar → Misafir oturumu**.
- İşlev: Login sonrasında aynı guest session'a ait sepet ve favorileri atomik biçimde claim eder.
- Operation ID: `POST-/api/guest-session/claim`
- Yetki: **User**.
- Request body: yoktur.
- Cookie: canonical `ecommerce_guest_cart` zorunludur.

## Başarılı response (200)

```json
{
  "cart": {
    "id": "00000000-0000-0000-0000-000000000001",
    "concurrencyToken": "00000000-0000-0000-0000-000000000001",
    "items": [],
    "totalQuantity": 0,
    "subTotal": 0,
    "hasUnavailableItems": false,
    "hasPriceChanges": false,
    "createdAt": "2026-08-14T12:00:00Z",
    "updatedAt": null
  },
  "favoriteCount": 0
}
```

`cart`, claim sonrası otoriter üye sepetidir. `favoriteCount`, claim sonrası otoriter üye favori sayısıdır.

`cart.items[].mainImage`, diğer `CartDto` cevaplarıyla aynı nullable ana görsel sözleşmesini kullanır. Ayrıntı: [CartItemDto ana görsel sözleşmesi](../../03-satis-ve-siparis/sepet/kalem-ana-gorsel-sozlesmesi.md).

## Öncelik kuralları

- Üye sepeti yok veya boşsa guest içerik benimsenir. Üye sepeti doluysa üye sepeti korunur ve guest sepet birleştirilmeden kaldırılır.
- Üye favori sayısı sıfırsa guest favoriler üyeye devredilir. Üyenin herhangi bir favorisi varsa üye listesi korunur ve guest favoriler birleştirilmeden kaldırılır.
- Cart ve favorites tek serializable transaction içinde işlenir. Bir hata iki alanı da geri alır.
- Cookie yalnız başarılı response sonrasında silinir; hata halinde retry için korunur.
- Başarılı claim favori veya sepete ekleme metriğini ikinci kez yazmaz.

## Hatalar

- `400 guest_session_required`: geçerli ortak guest cookie yok.
- `401`: JWT yok, geçersiz veya süresi dolmuş.
- `409`: gerçek concurrency veya güncel cart sahiplik/iş kuralı çakışması.

Başarı/hata kodları: `200`, `400`, `401`, `409`.
