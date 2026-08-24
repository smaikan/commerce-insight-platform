# POST /api/product-variants/stock-movements

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Aktif ürün varyantını SKU ile eşleştirip tek bir manuel stok hareketi oluşturur.
- Operation ID: `POST-/api/product-variants/stock-movements`
- Yetki: `AdminOnly`
- Content-Type: `application/json`

## Parametreler

Path, query veya header parametresi yoktur. SKU, route güvenliğine bağlı kalmaması için request body içinde gönderilir.

## Request body

| Alan | Tip | Zorunlu | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `productVariantSku` | string | Evet | Hayır | Trim sonrası 1–100 karakter; aktif ve silinmemiş varyant SKU'su |
| `quantityDelta` | integer (int32) | Evet | Hayır | Signed; sıfır ve `int.MinValue` geçersiz |
| `type` | numeric `StockMovementType` | Evet | Hayır | Yalnız izinli manuel hareket türü ve yönü |
| `reason` | string | Hayır | Evet | En fazla 500 karakter |

```json
{
  "productVariantSku": "TSHIRT-BLACK-M",
  "quantityDelta": 5,
  "type": 10,
  "reason": "Mal kabul"
}
```

## Başarılı response — 200 OK

Response güncel `ProductVariantDto` kaydıdır. `id`, response modelinde kalır; istemci bir sonraki stok hareketini yine SKU ile oluşturur.

```json
{
  "id": "d55bcdfb-d8f8-4d9f-b56f-3b1401783765",
  "productId": "P00001",
  "name": "Beden",
  "value": "M",
  "sku": "TSHIRT-BLACK-M",
  "barcode": null,
  "material": "Pamuk",
  "price": 499.9,
  "netPrice": 454.45,
  "compareAtPrice": null,
  "stock": 15,
  "addToCartCount": 0,
  "purchaseCount": 0,
  "isActive": true,
  "concurrencyToken": "55a5766b-2bb2-4b17-8638-13cdbcfcd95a"
}
```

## Hatalar

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| 400 | `validation_error` | SKU, miktar, tür/yön veya açıklama doğrulaması başarısız |
| 400 | `business_rule_violation` | Hareket stok bakiyesini geçersiz duruma düşürüyor |
| 401 | `authentication_required` / `invalid_access_token` | Geçerli oturum yok |
| 403 | `forbidden` | Kullanıcı Admin değil |
| 404 | `resource_not_found` | Aktif varyant SKU'su bulunamadı |
| 409 | `concurrency_conflict` | Aynı varyant eşzamanlı değiştirildi |

Bu endpoint bulk işlem değildir. Birden çok manuel hareket için atomik `POST /api/stock-movements/bulk` kullanılır.
