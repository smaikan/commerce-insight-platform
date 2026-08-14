# Yayımlanmış ürün arama altyapısı

## HTTP sözleşmesi

- `GET /api/products/published/search-suggestions?Query=şönil&Limit=10` anonim navbar önerisidir.
- `GET /api/products/published?Search=şönil&PageNumber=1&PageSize=24` mevcut sayfalı katalog sözleşmesini koruyan tam aramadır.
- `Query` trim ve whitespace normalizasyonundan sonra zorunlu, 2–100 karakterdir. `Limit` varsayılan 10, minimum 1, maksimum 10'dur.
- `Search` null/boşsa published listenin mevcut davranışı değişmez; doluysa normalize edilmiş uzunluğu 2–100 olmalıdır.
- Çok kelimeli sorguda her token birleşik arama metninde bulunmalıdır. Ürün adı, marka, tür, koleksiyon, etiket ve `MainSku` aranır. SKU yalnız eşleşme alanıdır; suggestion DTO'sunda yayımlanmaz.

Suggestion cevabı `PublishedProductSearchSuggestionsDto` tipidir. `items` zorunlu dizidir; `hasMore` zorunlu boolean'dır. Öğe alanlarından `id`, `title`, `url` ve `isAvailable` zorunludur. `brandName`, `price`, `compareAtPrice`, `imageUrl` ve `imageAlt` nullable'dır. Görsel yoksa URL/alt metni null olur; URL ürünün backend canonical katalog alanıdır.

## Arama ve relevance semantiği

Uygulama metni Türkçe küçük harfe çevirir, aksanları katlar, `ı/i/İ/I` değerlerini `i` olarak eşler ve ardışık whitespace'i tek boşluğa indirir. Örneğin ` ŞÖNİL   IŞIK ` değeri `sonil isik` olur.

Explicit `SortBy` yoksa tam arama ve suggestion şu SQL sırasını kullanır: tam ürün başlığı, başlık başlangıcı, başlık içeriği, marka, tür, koleksiyon, etiket, `PopularityScore`, `DisplayOrder`, `Product.Id`. Tam aramada explicit `SortBy` bu relevance sırasını ezer. `TypeId`, `BrandId`, `CollectionId` ve `TagId` aramayla AND çalışır. Count ve items aynı görünürlük/arama filtresini kullanır.

## SQL Server read model

Deployment ortamındaki SQL Server Full-Text Search bileşeni garanti edilmediği ve geliştirme SQL Server'ında `FULLTEXTSERVICEPROPERTY('IsFullTextInstalled') = 0` olduğu için FTS zorunluluğu eklenmedi. Migration ürün başına tek `ProductSearchDocuments` satırı ve indeksli iki/üç karakterli `ProductSearchGrams` aday tablosu oluşturur.

Sorgu önce normalize sorgunun ilk indeksli gramıyla aday kümesini daraltır, sonra bütün tokenları `ProductSearchContainsAllTokens` SQL fonksiyonuyla normalize dokümanda AND mantığında doğrular. Böylece contains semantiği korunurken marka/tür/koleksiyon/etiket ilişkileri her istekte taranmaz ve join satır çoğalması oluşmaz. Fiyat, stok ve görsel read modele kopyalanmaz; mevcut ortak published kart projeksiyonundan canlı okunur.

`RefreshProductSearchDocument` prosedürü ve Products, Brands, ProductTypes, Collections, ProductCollections, Tags ve ProductTags triggerları aynı veritabanı transaction'ında doküman/gramları yeniler. Migration mevcut ürünleri backfill eder. Büyük kataloglarda gram backfill'i bakım penceresinde uygulanmalı; migration öncesi yeterli transaction-log ve disk kapasitesi doğrulanmalıdır. Backup/restore ek bir işlem istemez çünkü fonksiyon, prosedür, trigger ve tablolar veritabanıyla birlikte taşınır.

## Görünürlük, sorgu sayısı ve cache

İki endpoint `WherePublished` ile `showOutOfStockProducts`/`showProductsWithoutPrice` kurallarını SQL'de uygular. Pasif varyant fiyat/stok hesabına girmez. Suggestion tek SQL komutudur, `COUNT` çalıştırmaz ve `Limit+1` okuyarak `hasMore` üretir. Tam arama count+items olmak üzere iki SQL komutudur; StoreSettings scalar alt sorgularla bu iki komuta gömülür.

Suggestion endpointi yüksek kardinalite nedeniyle output cache kullanmaz; dolayısıyla sınırsız query-key büyümesi ve instance-local stale veri yoktur. Ürün fiyatı, stok veya ana görsel değişikliği bir sonraki istekte doğrudan görünür. Aranabilir alan değişiklikleri transaction içindeki triggerlarla anında görünür olur. `public-search` rate-limit politikası IP başına sabit bir dakikada 120 istek, sıfır kuyruk uygular; aşım standart `429` ProblemDetails üretir.

## Ölçüm

`benchmarks/sql/product-search-candidate-comparison.sql` ve `product-search-gram-candidate.sql` 100.000 ürün üzerinde tekrar üretilebilir SQL ölçümüdür. İlişkisel aday koleksiyon/tag joinlerinde yaklaşık 1,6 milyon ilişki logical read'i üretirken hibrit gram sorgusu 2.000.000 temsili gram satırında cold 19 ms/684 logical read, warm 1 ms/361 logical read ölçülmüştür. Ayrıntılar `benchmarks/product-search-performance-report.md` belgesindedir. Bu LocalDB sonucu sorgu planı seçimini destekler; production-benzeri ortamın API p95/p99 kabul sonucu değildir. Uçtan uca p95/p99 için `benchmarks/scripts/measure-published-product-search.ps1` kullanılır.
