# POST /api/stock-movements/bulk

- İşlev alanı: **07 Yönetim, stok ve kampanya**
- İşlev: Birden çok manuel stok hareketini varyant SKU'larıyla tek atomik transaction içinde oluşturur.
- Operation ID: `POST-/api/stock-movements/bulk`
- Yetki: `AdminOnly`
- Content-Type: `application/json`
- Batch sınırı: 1–500 satır

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

| Alan | Tip | Zorunlu | Nullable | Kural |
| --- | --- | --- | --- | --- |
| `movements` | array | Evet | Hayır | 1–500 satır |
| `movements[].productVariantSku` | string | Evet | Hayır | Trim sonrası 1–100 karakter; aktif varyant SKU'su |
| `movements[].quantityDelta` | integer (int32) | Evet | Hayır | Signed; sıfır ve `int.MinValue` geçersiz |
| `movements[].type` | numeric `StockMovementType` | Evet | Hayır | İzinli manuel tür ve miktar yönü uyumlu olmalı |
| `movements[].reason` | string | Hayır | Evet | En fazla 500 karakter |

```json
{
  "movements": [
    {
      "productVariantSku": "TSHIRT-BLACK-M",
      "quantityDelta": 5,
      "type": 10,
      "reason": "Mal kabul"
    },
    {
      "productVariantSku": "TSHIRT-BLACK-L",
      "quantityDelta": -1,
      "type": 41,
      "reason": "Hasarlı ürün"
    }
  ]
}
```

## Başarılı response — 201 Created

`movements` dizisi request sırasını korur. Response hareket kayıtlarında kalıcı `productVariantId` bulunması normaldir; oluşturma isteği varyantı `productVariantSku` ile seçer.

```json
{
  "movementCount": 2,
  "movements": [
    {
      "id": "22dc9c47-48dc-49df-af4d-94382595a305",
      "productVariantId": "d55bcdfb-d8f8-4d9f-b56f-3b1401783765",
      "direction": 1,
      "type": 10,
      "quantityDelta": 5,
      "stockBeforeMovement": 10,
      "stockAfterMovement": 15,
      "reason": "Mal kabul",
      "orderId": null,
      "returnRequestId": null,
      "createdAt": "2026-08-23T05:30:00Z"
    },
    {
      "id": "155bd5ce-ce38-443f-bbca-990535c7683a",
      "productVariantId": "7ae0cd3d-6963-48be-b3a1-692597467a26",
      "direction": 2,
      "type": 41,
      "quantityDelta": -1,
      "stockBeforeMovement": 4,
      "stockAfterMovement": 3,
      "reason": "Hasarlı ürün",
      "orderId": null,
      "returnRequestId": null,
      "createdAt": "2026-08-23T05:30:00Z"
    }
  ]
}
```

## Atomiklik ve eşleştirme

- SKU değerleri baştaki ve sondaki boşluklardan temizlenir.
- Aynı SKU birden çok satırda kullanılabilir; hareketler request sırasıyla aynı varyanta uygulanır.
- Bütün SKU'lar ve kurallar kalıcı kayıt öncesinde doğrulanır.
- Bir SKU bulunamazsa, herhangi bir satır stoku eksiye düşürürse veya save başarısız olursa transaction rollback olur; kısmi hareket oluşmaz.

## Hatalar

| HTTP | ProblemDetails `code` | Koşul |
| --- | --- | --- |
| 400 | `validation_error` | Batch boyutu veya satır alanları geçersiz |
| 400 | `business_rule_violation` | Satırlardan biri domain stok kuralını ihlal ediyor |
| 401 | `authentication_required` / `invalid_access_token` | Geçerli oturum yok |
| 403 | `forbidden` | Kullanıcı Admin değil |
| 404 | `resource_not_found` | En az bir aktif varyant SKU'su bulunamadı |
| 409 | `concurrency_conflict` | Varyantlardan biri eşzamanlı değiştirildi |
