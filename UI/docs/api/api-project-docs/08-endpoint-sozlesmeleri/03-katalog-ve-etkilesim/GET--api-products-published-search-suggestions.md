# GET /api/products/published/search-suggestions

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Navbar arama modalı için düşük payload'lı public ürün önerileri.
- Yetki: `AllowAnonymous`; OpenAPI `security: []`.
- Cache: Output cache kullanılmaz.
- Rate limit: IP başına 120 istek/dakika, kuyruk yoktur.

## Parametreler

| Ad | Zorunlu | Kural |
| --- | --- | --- |
| `Query` | Evet | Trim/whitespace normalizasyonu sonrası 2–100 karakter. |
| `Limit` | Hayır | Varsayılan `10`; minimum `1`, maksimum `10`. |

Frontend iki karakterden önce istek göndermemeli ve yazım sırasında debounce/cancellation kullanmalıdır. API cancellation tokenı SQL komutuna kadar taşır. Geçersiz değerler `400 validation_error`, hız sınırı aşımı `429 rate_limit_exceeded` ProblemDetails döndürür.

## 200 response

```json
{
  "items": [
    {
      "id": "P123",
      "title": "Şönil Taşlı Kolye",
      "url": "sonil-tasli-kolye",
      "brandName": "Marka",
      "price": 2499.90,
      "compareAtPrice": 2799.90,
      "imageUrl": "https://cdn.example.com/product.jpg",
      "imageAlt": "Şönil taşlı kolye",
      "isAvailable": true
    }
  ],
  "hasMore": true
}
```

`items` ve `hasMore` zorunludur. Öğe içinde `id`, `title`, `url`, `isAvailable` zorunlu; `brandName`, `price`, `compareAtPrice`, `imageUrl`, `imageAlt` nullable'dır. `totalCount` yoktur. API `Limit+1` satır okuyup yalnız ilk `Limit` öğeyi döndürür.

`url` backend canonical ürün URL alanıdır; frontend başlıktan slug üretmez. `imageUrl`, yayınlanmış kartla aynı etkili ana görseldir ve görsel yoksa null'dır. Fiyat en düşük fiyatlı aktif varyanttan; `isAvailable` en az bir aktif ve stok pozitif varyanttan hesaplanır.

## Arama semantiği

Başlık, marka, ürün türü, koleksiyon, etiket ve MainSku aranır; SKU response'a eklenmez. Türkçe harf/aksan ve büyük-küçük harf normalize edilir. Çok kelimeli sorguda tokenlar AND'dir. Sıra: exact başlık, başlık prefix, başlık contains, marka, tür, koleksiyon, etiket, popülerlik, display order ve ürün kimliği.

Yalnız aktif/yayımlanmış ve StoreSettings görünürlük tercihlerine uyan ürünler döner. Endpoint tek SQL komutu kullanır, COUNT ve ürün başına ek sorgu üretmez.
