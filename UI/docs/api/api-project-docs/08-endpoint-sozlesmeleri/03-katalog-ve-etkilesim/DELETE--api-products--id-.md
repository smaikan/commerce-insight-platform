# DELETE /api/products/{id}

- İşlev alanı: **03 Katalog ve ürün etkileşimi**
- İşlev: Ürünü operasyonel geçmişini koruyarak katalogdan kaldırır (soft delete).
- Operation ID: `DELETE-/api/products/{id}`
- Yetki: `AdminOnly`.
- Content-Type: request body yoktur.
- Hata: 400 geçersiz public ID, 401 authentication, 403 policy, 404 ürün bulunamadı. Ortak gövde `ProblemDetails`tir.

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `id` | path | Evet | string; `P00001` biçiminde public product ID |

## Silme kuralı

Silme işlemi ürünü fiziksel olarak kaldırmaz. Ürün `Archived` ve pasif duruma geçirilir, öne çıkarma kaldırılır ve UTC silinme zamanı kaydedilir. Sipariş, iade, stok hareketi, analitik ve muhasebe geçmişi korunur; bu ilişkiler silmeyi engellemez.

Soft-delete edilen ürün admin/storefront listelerinde, ürün detayında, URL/SEO sonuçlarında ve public varyant/görsel okumalarında dönmez. Aynı ürüne tekrar DELETE gönderilmesi idempotenttir ve yine `204` döner. Silinen ürünün ana SKU ve URL değeri yeni bir üründe yeniden kullanılabilir; geçmiş varyant kaydı korunduğu için varyant SKU değeri ayrılmış kalır.

## Başarılı response (204)

Response body yoktur.
