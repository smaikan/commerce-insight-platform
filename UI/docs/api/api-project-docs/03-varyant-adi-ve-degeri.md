# Varyant adı ve değeri modeli

Her satılabilir `ProductVariant` frontend tarafından tekil veya en fazla üç parçalı çapraz kombinasyon olarak gönderilir. Backend kombinasyon üretmez.

Örnek kayıt:

```json
{
  "name": "Ebat",
  "value": "100x150",
  "sku": "HALI-100X150",
  "price": 2500,
  "stock": 12
}
```

## Merkezi kayıtlar

- `VariantOptionNames` yalnızca adları saklar. `Ebat` ve `ebat`, büyük/küçük harf duyarlı iki farklı kayıttır.
- `VariantOptionValues`, `VariantOptionNameId` foreign key'iyle adın altındaki değerleri saklar. `Ebat` altındaki `100x150` ve `200x250` iki ayrı satırdır.
- Yeni bir varyant gönderildiğinde bulunmayan ad ve değer otomatik kaydedilir; aynı ad/değer çifti tekrar kullanılır.

Birleştirilmiş örnek: `name: "Ebat / Renk"`, `value: "100x150 / Kırmızı"`. Bu metinler ProductVariant üzerinde aynen kalır; merkezi tablolara ise `Ebat`, `Renk`, `100x150` ve `Kırmızı` ayrı kayıtlar olarak yazılır. Aynı varyantta en fazla üç parça kabul edilir.
