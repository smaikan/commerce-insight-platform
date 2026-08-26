# PUT /api/product-variants/by-product/{productId}/bulk

- Görev alanı: **Katalog → Varyantlar**.
- İşlev: Aynı ürüne ait mevcut varyantları SKU takası dahil atomik olarak günceller.
- Operation ID: `PUT-/api/product-variants/by-product/{productId}/bulk`
- Yetki: **Admin**.
- Content-Type: `application/json`

Bu endpoint iki veya daha fazla mevcut varyantın SKU değerini birbirleriyle değiştirmek için kullanılır. Tekil `PUT /api/product-variants/{id}` sözleşmesi korunur; SKU takası için ayrı tekil çağrılar yapılmamalıdır.

## Parametreler

| Ad | Konum | Zorunlu | Şema | Açıklama |
| --- | --- | --- | --- | --- |
| `productId` | path | Evet | string | `P` önekli public ürün kimliği; örnek `P00004`. |

## Request body

| Alan | Tip | Zorunlu | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `variants` | array | Evet | Hayır | 1–100 satır; aynı varyant kimliği veya hedef SKU yinelenemez. |
| `variants[].id` | uuid | Evet | Hayır | Güncellenecek mevcut `ProductVariant` kimliği. |
| `variants[].name` | string | Evet | Hayır | 1–150; birleşik modelde en fazla üç `/` parçası. |
| `variants[].value` | string | Evet | Hayır | 1–150; `name` ile aynı sayıda `/` parçası. |
| `variants[].sku` | string | Evet | Hayır | 1–100; batch içinde ve batch dışındaki varyantlara karşı benzersiz. |
| `variants[].price` | number | Evet | Hayır | `> 0`; vergi dahil hedef fiyat. |
| `variants[].stock` | int32 | Evet | Hayır | `>= 0`; hedef mutlak stok bakiyesi. |
| `variants[].expectedConcurrencyToken` | uuid | Evet | Hayır | Son GET/response içindeki güncel `concurrencyToken`. |
| `variants[].compareAtPrice` | number | Hayır | Evet | Verildiyse `price` değerinden küçük olamaz. |
| `variants[].barcode` | string | Hayır | Evet | En fazla 100. |
| `variants[].material` | string | Hayır | Evet | En fazla 120. |
| `variants[].isActive` | boolean | Evet | Hayır | Hedef satış aktivasyonu. |
| `variants[].stockAdjustmentReason` | string | Hayır | Evet | En fazla 500; stok farkı varsa `StockCountAdjustment` gerekçesi. |

SKU takası örneği:

```json
{
  "variants": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "Uzunluk",
      "value": "45 CM",
      "sku": "SKU-B",
      "price": 899.9,
      "stock": 5,
      "expectedConcurrencyToken": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
      "compareAtPrice": 999.9,
      "barcode": null,
      "material": "Çelik",
      "isActive": true,
      "stockAdjustmentReason": "Depo sayımı"
    },
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "name": "Uzunluk",
      "value": "50 CM",
      "sku": "SKU-A",
      "price": 899.9,
      "stock": 3,
      "expectedConcurrencyToken": "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
      "compareAtPrice": null,
      "barcode": "8690000000001",
      "material": null,
      "isActive": true,
      "stockAdjustmentReason": null
    }
  ]
}
```

## Atomiklik ve iş kuralları

- Bütün kimlikler path'teki ürüne ait olmalıdır; eksik veya başka ürüne ait tek kimlik bütün batch'i `404` ile durdurur.
- Hedef SKU kümesi batch içindeki mevcut SKU sahiplerini çakışma saymaz. `A ↔ B` takası ve `A → B, B → C, C → A` döngüsü desteklenir.
- SQL Server unique index için varyantlar transaction içinde önce kısa, benzersiz geçici SKU'lara, sonra nihai SKU'lara taşınır. Geçici değerler transaction dışında görünmez; response, ProblemDetails veya loglara yazılmaz.
- İki kayıt aşaması aynı serializable transaction içindedir. İkinci aşama dahil herhangi bir hata bütün değişiklikleri rollback eder.
- `stock`, mutlak hedef bakiyedir. Fark varsa varyant başına tam bir `StockCountAdjustment` hareketi oluşur; doğrudan stok yazılmaz.
- `netPrice` ürünün güncel vergi oranından sunucuda yeniden hesaplanır.
- `name/value` merkezi option resolver üzerinden işlenir ve seçenek bağlantıları aynı transaction'da güncellenir.
- Her başarılı mutasyon yeni bir `concurrencyToken` üretir. Cevaptaki token sonraki mutation için saklanmalıdır.

## Başarılı response (200)

Response, yalnız request içindeki varyantları request sırasıyla ve veritabanına kalıcılaşmış authoritative değerlerle döndürür.

```json
[
  {
    "id": "11111111-1111-1111-1111-111111111111",
    "productId": "P00004",
    "name": "Uzunluk",
    "value": "45 CM",
    "variantOptionNameId": "33333333-3333-3333-3333-333333333333",
    "variantOptionValueId": "44444444-4444-4444-4444-444444444444",
    "sku": "SKU-B",
    "barcode": null,
    "material": "Çelik",
    "price": 899.9,
    "netPrice": 749.92,
    "compareAtPrice": 999.9,
    "stock": 5,
    "addToCartCount": 12,
    "purchaseCount": 4,
    "isActive": true,
    "concurrencyToken": "cccccccc-cccc-cccc-cccc-cccccccccccc"
  },
  {
    "id": "22222222-2222-2222-2222-222222222222",
    "productId": "P00004",
    "name": "Uzunluk",
    "value": "50 CM",
    "variantOptionNameId": "33333333-3333-3333-3333-333333333333",
    "variantOptionValueId": "55555555-5555-5555-5555-555555555555",
    "sku": "SKU-A",
    "barcode": "8690000000001",
    "material": null,
    "price": 899.9,
    "netPrice": 749.92,
    "compareAtPrice": null,
    "stock": 3,
    "addToCartCount": 8,
    "purchaseCount": 2,
    "isActive": true,
    "concurrencyToken": "dddddddd-dddd-dddd-dddd-dddddddddddd"
  }
]
```

## Hata sözleşmeleri

| HTTP | Kesin `code` değerleri | Davranış |
| --- | --- | --- |
| `400` | `validation_error`, `business_rule_violation` | Alan, batch boyutu, duplicate ID/SKU, fiyat, stok veya option şekli geçersiz. |
| `401` | `authentication_required`, `invalid_access_token` | Bearer token yok veya geçersiz. |
| `403` | `forbidden` | Aktif Admin yetkisi yok. |
| `404` | `resource_not_found` | Bir varyant yok veya path'teki ürüne ait değil; kimlik ayrıntısı açıklanmaz. |
| `409` | `product_variant_sku_conflict` | Hedef SKU batch dışındaki varyantta kullanılıyor; `errors` ilgili satırı gösterir. |
| `409` | `concurrency_conflict` | En az bir `expectedConcurrencyToken` eski; bütün batch rollback olur. |
| `500` | `internal_error` | Beklenmeyen sunucu hatası; production detail genel metindir. |

Batch dışı SKU çakışması örneği:

```json
{
  "type": "urn:ecommerce:error:product_variant_sku_conflict",
  "title": "Product variant SKU conflict",
  "status": 409,
  "detail": "One or more variant SKU values are already in use outside this batch.",
  "instance": "/api/product-variants/by-product/P00004/bulk",
  "errors": {
    "variants[0].sku": [
      "This SKU is already used by a variant outside this batch."
    ]
  },
  "code": "product_variant_sku_conflict",
  "traceId": "0HNEXAMPLE:00000001",
  "timestamp": "2026-08-22T04:00:00Z"
}
```

Stale token örneği:

```json
{
  "type": "urn:ecommerce:error:concurrency_conflict",
  "title": "Concurrency conflict",
  "status": 409,
  "detail": "One or more product variants were changed by another operation. Refresh the data and try again.",
  "instance": "/api/product-variants/by-product/P00004/bulk",
  "code": "concurrency_conflict",
  "traceId": "0HNEXAMPLE:00000002",
  "timestamp": "2026-08-22T04:00:01Z"
}
```

## Retry ve idempotency

`Idempotency-Key` kullanılmaz. İşlem mutlak hedef değerler kullanan bir `PUT` operasyonudur ve her satırın zorunlu optimistic concurrency tokenı at-most-once yan etki koruması sağlar. Başarılı cevabı kaybedip aynı eski tokenlarla tekrar göndermek stok hareketini veya diğer yan etkileri tekrarlamaz; `409 concurrency_conflict` döner. İstemci güncel varyantları GET ile okuyup kullanıcı niyetini yeniden oluşturmalıdır; tokenı körlemesine değiştirip otomatik retry yapmamalıdır.
