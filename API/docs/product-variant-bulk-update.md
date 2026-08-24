# Atomik ürün varyantı batch güncellemesi

`PUT /api/product-variants/by-product/{productId}/bulk`, aynı ürüne ait mevcut varyantların SKU, seçenek, fiyat, stok ve aktivasyon alanlarını tek serializable transaction içinde günceller. Amaç global unique SKU indexi altında iki veya daha fazla SKU'nun güvenli biçimde takas edilmesidir.

## Persistence stratejisi

1. Bütün varyantlar kimlik sırasıyla ve tracked olarak ürün/vergi/seçenek ilişkileriyle okunur.
2. Ürün sahipliği, zorunlu `expectedConcurrencyToken` değerleri ve batch dışı hedef SKU sahipliği mutation başlamadan doğrulanır.
3. Merkezi option seçimleri hazırlanır.
4. Her varyant global olarak boş olduğu doğrulanan `__BULK__{32 hex}` biçimli geçici SKU'ya taşınır ve ilk `SaveChanges` çalışır.
5. Nihai detaylar uygulanır; stok farkları `StockCountAdjustment` hareketi olarak eklenir ve ikinci `SaveChanges` çalışır.
6. Transaction commit edilir. Her exception transactionın tamamını rollback eder.

Geçici SKU en fazla 40 karakterdir, `ProductVariant.Sku` uzunluk sınırına uyar ve response/log/event sözleşmesine taşınmaz. Batch dışı SKU çakışması `409 product_variant_sku_conflict`, stale token ise `409 concurrency_conflict` üretir.

## Retry davranışı

Endpoint ayrı `Idempotency-Key` istemez. Zorunlu concurrency tokenları nedeniyle başarılı intentin eski tokenlarla tekrarı mutation başlamadan `concurrency_conflict` olur; stok hareketi ikinci kez oluşmaz. Bu davranış kayıp başarılı response'u replay etmez, yalnız yan etkilerin tekrarlanmasını engeller.

Şema değişikliği ve migration yoktur. Global `ProductVariants.Sku` unique indexi ve mevcut `ConcurrencyToken` concurrency metadata'sı aynen korunur.
