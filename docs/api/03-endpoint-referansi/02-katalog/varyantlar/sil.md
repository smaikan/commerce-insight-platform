# DELETE /api/product-variants/{id}

- Görev alanı: **Katalog → Varyantlar**.
- İşlev: Varyantı stok ve işlem geçmişini koruyarak mantıksal olarak siler.
- Operation ID: `DELETE-/api/product-variants/{id}`
- Yetki: **Admin**.
- Content-Type: request body yoktur.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string (uuid) |

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## İş kuralları

- Varyantın stok hareketi bulunması silmeye engel değildir.
- İşlem fiziksel satır silmez. Varyantı pasif ve silinmiş olarak işaretler; normal varyant, ürün detay ve liste sorgularından gizler.
- `StockMovement` ve diğer işlem geçmişleri korunur.
- Bir ürünün son kalan varyantı silinemez.
- Silinen varyantın SKU değeri yeni bir aktif varyantta yeniden kullanılabilir.

## Başarılı response (204)

Response body yoktur.

## Hatalar

| HTTP | `code` | Koşul |
| --- | --- | --- |
| `401` | `unauthorized` | Bearer token yok veya geçersiz. |
| `403` | `forbidden` | Kullanıcı `AdminOnly` yetkisine sahip değil. |
| `404` | `not_found` | Varyant veya bağlı ürün bulunamadı; mantıksal silinmiş varyantlar da bulunamadı kabul edilir. |
| `409` | `conflict` | Silinecek varyant ürünün son kalan varyantıdır. |

Hata gövdesi ortak `ProblemDetails` sözleşmesini kullanır.
