# Admin API İyileştirme Önerileri

## ADM-RET-001 — İade kalemi bazında karar sözleşmesi

- Öncelik: Yüksek
- Durum: Açık sözleşme eksikliği
- Etkilenen arayüz: `/orders/[orderId]` iade ve değişim yönetimi
- Değişmeyen kimlik: `ADM-RET-001`
- Problem: `ReturnRequestDto.items` her ürün ve talep adetini taşıyor; ancak mevcut `POST /api/returns/{id}/approve` ve `POST /api/returns/{id}/reject` işlemleri bütün `ReturnRequest` kaydına tek karar uyguluyor. Back office çalışanı aynı talepteki ürünleri ayrı ayrı onaylayamıyor veya reddedemiyor.
- Önerilen sözleşme: Her `ReturnItem` için karar durumu, karar notu ve karar zamanı sağlayan atomik bir talep-karar endpoint'i yayımlansın. Karma kararların `ReturnRequestStatus`, `OrderStatus`, refund toplamı, stok ve değişim etkisini nasıl üreteceği açıkça belgelensin.
- Kabul ölçütleri:
  1. İstek her `returnItemId` için `Approved` veya `Rejected` kararı taşıyabilmeli.
  2. Aynı karar intent'i idempotent olmalı ve eşzamanlı güncellemede `409` üretmeli.
  3. Kısmi onayda refund/değişim ve stok işlemleri yalnız onaylanan adetleri kullanmalı.
  4. Talep ve sipariş üst durumlarının karma satır kararlarından nasıl türetildiği belgelenmeli.
  5. OpenAPI, endpoint Markdown, DTO ve validator aynı sözleşmeyi taşımalı.

Mevcut Admin arayüzü bu sözleşme yayımlanana kadar ürün ve adetleri ayrı satırlarda gösterir; onay veya ret kararını API'nin desteklediği şekilde talebin tamamına uygular.

## ADM-PROD-001 — Varyant SKU çakışması için typed ProblemDetails sözleşmesi

- Öncelik: Orta
- Durum: Açık sözleşme eksikliği
- Etkilenen arayüz: `/products/[productId]` varyant oluşturma ve güncelleme
- Değişmeyen kimlik: `ADM-PROD-001`
- Problem: `POST /api/product-variants/by-product/{productId}` ve `PUT /api/product-variants/{id}` runtime'da global SKU benzersizliği ihlalinde `409` ve genel `code=conflict` döndürüyor. OpenAPI ile endpoint Markdown belgeleri SKU benzersizliğinin ürünler arasında global olduğunu ve bu hatayı diğer iş kuralı çakışmalarından ayıran typed bir kodu yayımlamıyor.
- Önerilen sözleşme: Varyant SKU çakışması için örneğin `product_variant_sku_conflict` gibi stabil bir ProblemDetails `code` değeri; normalize edilen çakışan alan adı ve global benzersizlik kapsamı OpenAPI ile iki endpoint Markdown dosyasında yayımlansın.
- Kabul ölçütleri:
  1. Create ve update endpoint'leri aynı typed kodu döndürmeli.
  2. Update kontrolü güncellenen varyantın kendi kimliğini hariç tutmalı.
  3. SKU karşılaştırmasının trim ve büyük/küçük harf normalizasyonu açıkça belgelenmeli.
  4. OpenAPI 409 response, endpoint Markdown, handler ve integration testleri aynı davranışı taşımalı.
  5. Frontend hatayı doğrudan ilgili SKU alanına bağlayabilmeli; concurrency yalnız `concurrency_conflict` koduyla ayrılmalı.

## ADM-PROD-002 — Mevcut varyanta ilk seçenek bağlantısı eklenirken sahte concurrency çatışması

- Öncelik: Yüksek
- Durum: Doğrulanmış API kalıcılık hatası
- Etkilenen arayüz: `/products/[productId]` üzerinde varyantsız ürünü varyantlı ürüne dönüştürme ve mevcut varyantın seçenek kombinasyonunu değiştirme
- Değişmeyen kimlik: `ADM-PROD-002`
- Problem: `PUT /api/product-variants/{id}` yeni `ProductVariantOptionValue` bağlantılarını mevcut, takipli varyanta eklediğinde EF Core bu client-generated Guid kimlikli child entity'yi `Added` yerine `Modified` olarak işaretleyebiliyor. Var olmayan satıra UPDATE uygulanınca sıfır satır etkileniyor ve API yanlış biçimde `409 code=concurrency_conflict` döndürüyor. Runtime trace `0HNNVRNMCHNOT:00000003` çakışan entity'yi `ECommerce.Domain.Entities.ProductVariantOptionValue`, state'i `Modified` olarak doğruladı.
- Önerilen uygulama: `ProductVariantOptionValueConfiguration` içinde `Id` alanını açıkça client-generated (`ValueGeneratedNever`) yapılandır; koleksiyon değiştirme akışında yeni bağlantıların `Added`, kaldırılanların `Deleted` olduğuna dair SQL Server integration testi ekle.
- Kabul ölçütleri:
  1. Varyantsız ürünün varsayılan varyantı `Uzunluk / 40 CM` gibi ilk gerçek seçeneğe başarıyla güncellenmeli.
  2. Mevcut tekli ve birleşik seçenek kombinasyonları değiştirilebilmeli.
  3. Yeni bağlantılar INSERT, kaldırılan bağlantılar DELETE üretmeli; var olmayan child satırına UPDATE gönderilmemeli.
  4. Gerçek eşzamanlı değişiklikler yine `409 concurrency_conflict` olarak korunmalı.
  5. Test SQL Server provider üzerinde çalışmalı; yalnız SQLite testi bu entity-state farkını güvenilir biçimde yakalamıyor.

## ADM-PROD-003 — Ürün görselleri için atomik toplu sıralama

- Öncelik: Orta
- Durum: Açık API iyileştirmesi; Admin mevcut tekil PUT sözleşmesiyle güvenli yeniden deneme uygular
- Etkilenen arayüz: `/products/[productId]` medya sıralama ve ana görsel yönetimi
- Değişmeyen kimlik: `ADM-PROD-003`
- Problem: API yalnız `PUT /api/product-images/{id}` ile tek görselin `displayOrder` ve `isMain` alanlarını güncelliyor. Birden fazla görsel sürüklenerek yeniden sıralandığında Admin değişen kayıtları ayrı isteklerle kaydetmek zorunda kalıyor; ara istekte hata oluşursa sıra geçici olarak kısmen uygulanabiliyor ve endpoint concurrency tokenı taşımıyor.
- Önerilen sözleşme: Ürüne ait görsel kimliklerini, benzersiz hedef `displayOrder` değerlerini, tek ana görsel kimliğini ve beklenen concurrency bağlamını alan atomik bir bulk reorder endpoint'i yayımlansın.
- Kabul ölçütleri:
  1. Bütün hedef sıralama ve ana görsel seçimi tek transaction içinde uygulanmalı veya hiç uygulanmamalı.
  2. Başka ürüne ait, eksik, tekrarlanan görsel kimlikleri ve tekrarlanan sıra değerleri stabil typed ProblemDetails kodlarıyla reddedilmeli.
  3. Eşzamanlı medya değişikliği `409 concurrency_conflict` üretmeli; authoritative güncel sıra cevapta veya belgelenmiş yeniden okuma akışında erişilebilir olmalı.
  4. OpenAPI ve Markdown sözleşmesi request/response, numeric sınırlar, maksimum 10 görsel ve idempotent tekrar davranışını açıklamalı.

## ADM-RET-002 — Sipariş kapsamlı iade ürün projeksiyonu

- Öncelik: Orta
- Durum: Açık performans sözleşmesi eksikliği
- Etkilenen arayüz: `/orders/[orderId]` ve sipariş listesi hızlı bakış
- Değişmeyen kimlik: `ADM-RET-002`
- Problem: `GET /api/returns?OrderId=...` yalnız `ReturnRequestSummaryDto` döndürüyor ve ürün/adet taşımıyor. Admin, sipariş içindeki talep edilen ürünleri göstermek için her iade özeti adına ayrıca `GET /api/returns/admin/{id}` çağrısı yapmak zorunda kalıyor.
- Önerilen sözleşme: Sipariş kimliğiyle tek çağrıda iade taleplerinin ürün/adet karar projeksiyonunu döndüren yönetici okuma modeli yayımlansın veya mevcut OrderId filtreli listeye açıkça belgelenmiş kompakt `items` alanı eklensin.
- Kabul ölçütleri:
  1. Bir siparişin iade numarası, tipi, durumu ve ürün/adetleri tek backend sorgu/HTTP cevabında alınabilmeli.
  2. Projeksiyon yalnız AdminOnly olmalı ve ortak cache'e alınmamalı.
  3. Sayfalama ve nullable alanlar OpenAPI ile Markdown'da birebir tanımlanmalı.
  4. Admin hızlı bakışında iade talebi başına detay N+1 çağrısı gerekmemeli.

## ADM-RET-003 — Fiziksel teslim alma karar öncesinde olacak iade yaşam döngüsü

- Öncelik: Kritik / Admin akışı engeli
- Durum: Çözüldü (2026-08-23; yeni lifecycle, legacy uyumluluk, typed 409 ve endpoint sözleşmeleri yayımlandı)
- Etkilenen arayüz: `/orders/[orderId]` iade ve değişim yönetimi
- Değişmeyen kimlik: `ADM-RET-003`
- Problem: Onaylanan operasyon akışı `Requested (0) → Received (3) → Approved (1) / Rejected (2)` olmalıdır; yönetici talep geldiğinde önce ürünleri fiziksel olarak teslim almalı, ancak bundan sonra kabul veya ret kararı verebilmelidir. Mevcut domain bunun tersini zorunlu kılıyor: `Receive` yalnız `Approved (1)` kaydında, `Approve` ve `Reject` yalnız `Requested (0)` kaydında çalışıyor. Ayrıca mevcut `Receive` refund stoğunu hemen geri ekliyor ve `ReturnOrderStatusSynchronizer`, `Received` kaydını karar verilmiş sayarak siparişi `Refunded (7)` veya `ReturnApproved (9)` durumuna taşıyor. Admin'in butonları yalnız görsel olarak yeniden sıralaması ilk `receive` çağrısını başarısız kılar; istemcinin gizlice `approve` ardından `receive` çağırması ise karar verilmeden sipariş durumunu ve stok etkisini değiştirir, iki ayrı mutasyonu atomik olmayan sahte bir işlem halinde sunar.
- Önerilen sözleşme: Mevcut endpoint adları korunacaksa `POST /api/returns/{id}/receive` yalnız `Requested (0) → Received (3)` geçişini; `POST /api/returns/{id}/approve` ve `/reject` yalnız `Received (3) → Approved (1) / Rejected (2)` geçişini uygulamalıdır. `Received` karar bekleyen fiziksel teslim durumudur ve bu aşamada Order `ReturnRequested (8)` kalmalıdır. Refund stok girişi ve Order `Refunded (7)` geçişi yalnız teslim alınmış talep onaylandığında atomik uygulanmalı; ret halinde iade stoğu artırılmamalı ve aktif başka talep yoksa sipariş durumu belgelenmiş önceki duruma dönmelidir. Exchange stok ve Order `ReturnApproved (9)` etkisinin hangi karar anında oluşacağı aynı şekilde açıkça yayımlanmalıdır. Ürün kararına göre `Approved` ve `Rejected` terminal Admin durumlarıysa mevcut `/complete` endpoint'inin yeni taleplerde kullanılıp kullanılmadığı, eski `Completed (4)` kayıtlarının okunma davranışı ve geriye uyumluluk açıkça belgelenmelidir.
- Kabul ölçütleri:
  1. Yeni talepte Admin'e yalnız “Ürünleri teslim aldım” aksiyonu gösterilebilmeli ve çağrı `Requested (0) → Received (3)` sonucunu döndürmeli.
  2. `Received (3)` talepte yalnız “İadeyi onayla” ve “Talebi reddet” kararları geçerli olmalı; karar notu bu aşamada alınmalı.
  3. Refund için `approve`, ReturnRequest'i `Approved (1)` ve Order'ı aynı transaction içinde `Refunded (7)` yapmalı; ödeme sağlayıcısı veya `Payment` kaydı etkilenmemeli.
  4. `receive` aşamasında satılabilir stok artırılmamalı ve Order `ReturnRequested (8)` kalmalı; refund stok girişi yalnız sonraki onayla ve yalnız bir kez oluşmalı.
  5. `reject`, teslim alınmış talebi `Rejected (2)` yapmalı, refund/değişim stok etkisi oluşturmamalı ve sipariş üst durumunu aynı siparişteki diğer aktif talepleri dikkate alarak yeniden üretmeli.
  6. Exchange onayının iade stok girişi, replacement stok çıkışı ve Order `ReturnApproved (9)` etkisi tek atomik davranış olarak tanımlanmalı; yetersiz/değişmiş replacement stok hatası kararı kısmen kaydetmemeli.
  7. `/complete` operasyonunun yeni matriste kaldırılmış, terminal veya hâlâ gerekli olup olmadığı; `Completed (4)`, `approvedAt`, `rejectedAt`, `receivedAt` ve `completedAt` alanlarının yeni semantiği OpenAPI ile dört endpoint Markdown belgesinde yayımlanmalı.
  8. Geçersiz/eski durum geçişleri stabil typed ProblemDetails kodu vermeli; aynı talebe eşzamanlı karar ve tekrar gönderim davranışı belgelenip test edilmeli.
  9. Domain, handler, sipariş durum senkronizasyonu, stok hareketi, notification/outbox ve SQL Server integration testleri yeni matrisi birlikte doğrulamalı.

Admin arayüzü yayımlanan sözleşmeye göre yeni kayıtlarda `Requested → Received → Approved/Rejected` matrisini; timestamp ile doğrulanan eski kayıtlarda ise sınırlı receive/complete uyumluluğunu uygular.

## ADM-CNT-001 — İletişim mesajı durum geçiş matrisi

- Öncelik: Yüksek
- Durum: Çözüldü (2026-08-21; geçiş matrisi OpenAPI ve endpoint Markdown'da yayımlandı)
- Etkilenen arayüz: `/contact-messages/[messageId]` durum yönetimi
- Değişmeyen kimlik: `ADM-CNT-001`
- Problem: `PATCH /api/contact-messages/{id}/status` Markdown sözleşmesi yalnız “domain allowlist durum geçişi” ifadesini içeriyor. Her `ContactMessageStatus` değerinden hangi hedeflere geçilebildiği OpenAPI veya yayımlanmış Markdown içinde belirtilmiyor. Admin arayüzü bu nedenle yalnız geçerli durum aksiyonlarını sözleşmeye dayanarak gösteremez.
- Önerilen sözleşme: Numeric kaynak ve hedef değerlerini içeren açık geçiş matrisi endpoint Markdown dosyasına eklenmeli; aynı duruma geçiş, terminal/yeniden açma davranışı ve geçersiz geçişte dönen ProblemDetails `code` değeri belgelenmeli.
- Kabul ölçütleri:
  1. `New`, `InProgress`, `WaitingForCustomer`, `Resolved`, `Closed` ve `Spam` için izin verilen bütün hedefler numeric değerleriyle yayımlanmalı.
  2. Aynı duruma geçişin kabul edilip edilmediği açık olmalı.
  3. Yeniden açma ve Spam'den çıkarma davranışı ayrıca belirtilmeli.
  4. Geçersiz geçişin HTTP durumu ve ProblemDetails `code` değeri belgelenmeli.
  5. OpenAPI açıklaması, endpoint Markdown, domain allowlist ve testler aynı matrisi taşımalı.

## ADM-CNT-002 — İletişim activity semantiği ve sıralama sözleşmesi

- Öncelik: Yüksek
- Durum: Çözüldü (2026-08-21; activity alan semantiği ve stabil sıralama OpenAPI ve endpoint Markdown'da yayımlandı)
- Etkilenen arayüz: `/contact-messages/[messageId]` audit/activity, dahili not ve müşteri yanıtı görünümü
- Değişmeyen kimlik: `ADM-CNT-002`
- Problem: OpenAPI `ContactMessageActivityType` için yalnız `0..4` numeric değerlerini yayımlıyor; Markdown dosyaları bu değerlerin adlarını, `content`, `previousValue`, `newValue` ve `replyId` alanlarının tipe göre anlamını veya `activities`/`replies` dizilerinin sıralama garantisini tanımlamıyor. Bu bilgiler yalnız API kaynak kodunda bulunuyor ve frontend için yayımlanmış sözleşme sayılamıyor.
- Önerilen sözleşme: Activity enum eşlemesi, tipe göre alan kullanım tablosu, actor nullable davranışı, status/assignment değer biçimi, reply bağlantısı ve kronolojik sıralama/tie-breaker garantisi detail endpoint Markdown sözleşmesine eklenmeli.
- Kabul ölçütleri:
  1. `ContactMessageActivityType` numeric değerleri adlarıyla yayımlanmalı.
  2. Her activity tipi için kullanılabilen ve null kalan DTO alanları açıklanmalı.
  3. `previousValue`/`newValue` değerlerinin status adı mı numeric değer mi, assignment için public user ID mi olduğu tanımlanmalı.
  4. `ReplyQueued.replyId` ile `ContactMessageReplyDto.id` ilişkisinin cardinality ve nullable davranışı belgelenmeli.
  5. `activities` ve `replies` sıralaması ile eşit zaman damgasındaki stabil tie-breaker yayımlanmalı.
  6. OpenAPI açıklamaları, endpoint Markdown, DTO mapping ve testler aynı semantiği taşımalı.

## STO-ORD-001 — Shipped öncesi müşteri iptali ve ödeme iadesi orkestrasyonu

- Öncelik: Kritik
- Durum: Çözüldü (2026-08-24; iyzico cancel/item refund sagası, polling ve reconciliation worker uygulandı)
- Etkilenen arayüz: Storefront `/checkout/confirmation/[orderId]`, `/account/orders/[orderId]` ve guest sipariş confirmation/self-service akışları
- Değişmeyen kimlik: `STO-ORD-001`
- Problem: Ürün kararı gereği müşteri `Pending (0)`, `Confirmed (1)`, `Paid (2)` ve `Preparing (3)` durumlarındaki siparişi `Shipped (4)` olmadan önce iptal edebilmelidir. Mevcut `POST /api/orders/{id}/cancel` ve `POST /api/guest-orders/{id}/cancel` uygulaması yalnız `Pending/Confirmed` kabul ediyor; `Paid/Preparing` için ödeme sağlayıcısında tahsilatı geri alan cancel/refund yeteneği bulunmadığından `409` üretiyor. Eski yerel davranışın yalnız sipariş durumunu `Cancelled` yapacak şekilde geri açılması güvenli değildir; sağlayıcıdaki tahsilat açık kalabilir.
- Önerilen sözleşme: Üye ve guest iptal endpoint'leri aynı yaşam döngüsü kuralını kullanmalı; `Paid/Preparing` iptalinde sağlayıcıdaki authoritative ödeme sonucuna göre cancel/refund işlemini idempotent ve dayanıklı bir orkestrasyonla tamamlamalıdır. Sağlayıcı çağrısı veritabanı transaction'ı dışında yapılmalı; kesin başarıdan sonra ödeme, sipariş, stok, kupon ve notification/outbox etkileri tek serializable transaction içinde bir kez uygulanmalıdır. Sonucun belirsiz kaldığı timeout/ağ kesintisinde sipariş iptal edilmiş gösterilmemeli; mutabakat bekleyen durum stabil bir ProblemDetails kodu veya belgelenmiş `202 Accepted` operasyon modeliyle istemciye bildirilmelidir.
- Kabul ölçütleri:
  1. Üye ve guest endpoint'leri `Pending`, `Confirmed`, `Paid` ve `Preparing` siparişlerde aynı sonucu üretmeli; `Shipped` ve sonraki durumları stabil typed ProblemDetails koduyla reddetmeli.
  2. `Pending/Confirmed` akışında açık veya belirsiz ödeme önce güvenli biçimde uzlaştırılmalı; kesin tahsilat yoksa stok ve kupon yalnız bir kez bırakılarak sipariş `Cancelled` olmalı.
  3. `Paid/Preparing` akışında sağlayıcı cancel/refund başarısı kesinleşmeden sipariş `Cancelled` yapılmamalı, stok artırılmamalı ve kupon bırakılmamalı.
  4. Kesin sağlayıcı başarısından sonra ödeme durumu provider sonucuyla uyumlu `Cancelled` veya `Refunded`, sipariş durumu `Cancelled` olmalı; stok, kupon ve bildirim/outbox yan etkileri atomik ve idempotent uygulanmalı.
  5. Sağlayıcı timeout'u veya belirsiz sonuçta kalıcı bir reconciliation kaydı oluşturulmalı; tekrar çağrı aynı iptal intent'ini sürdürmeli ve çift refund/stock/coupon etkisi üretmemeli.
  6. Endpoint başarı, bekleyen mutabakat, sağlayıcı reddi, eşzamanlı değişiklik ve geçersiz yaşam döngüsü cevapları OpenAPI ile iki endpoint Markdown sözleşmesinde aynı HTTP durumları ve typed kodlarla yayımlanmalı.
  7. Member ve guest için `Paid`, `Preparing`, `Shipped`, provider success/reject/timeout, tekrar istek ve callback yarışlarını kapsayan unit ile SQL Server integration testleri eklenmeli.

Storefront `200` sonucunu tamamlanmış iptal, `202` sonucunu polling gerektiren devam eden operasyon ve typed `409` sonuçlarını terminal hata olarak işlemelidir. `Paid/Preparing` artık refundsuz `Cancelled` yapılmaz; `Shipped` durumunda müşteri iptali kapalı kalır.

## ADM-ACC-001 — Cari hesap listeleme filtre ve arama sözleşmesi

- Öncelik: Orta
- Durum: Açık kullanılabilirlik eksikliği
- Etkilenen arayüz: `/accounting/current-accounts`
- Değişmeyen kimlik: `ADM-ACC-001`
- Problem: `GET /api/accounting/current-accounts` yalnız `pageNumber` ve `pageSize` kabul ediyor. Admin cari kodu/unvanı arayamıyor; tür veya aktiflik filtresi uygulayamıyor. Frontend yalnız o anki sayfayı filtreleyerek yanıltıcı sonuç üretmemelidir.
- Önerilen sözleşme: Server-side `search`, `type` ve `isActive` filtreleri ile açık, stabil sıralama seçenekleri yayımlansın.
- Kabul ölçütleri:
  1. Arama en az cari kodu, ad ve ticari unvan üzerinde tanımlı, normalize edilmiş davranışa sahip olmalı.
  2. Tür ve aktiflik filtreleri birlikte kullanılabilmeli; filtre değişiminde sayfalama deterministik kalmalı.
  3. Varsayılan ve izin verilen sıralamalar tie-breaker alanıyla birlikte belgelenmeli.
  4. Query sınırları OpenAPI, Markdown, validator ve persistence testlerinde aynı olmalı.

## ADM-ACC-002 — Cari ekstreye özgü hareket DTO'su

- Öncelik: Yüksek
- Durum: Açık semantik sözleşme eksikliği
- Etkilenen arayüz: `/accounting/current-accounts/[currentAccountId]`
- Değişmeyen kimlik: `ADM-ACC-002`
- Problem: Cari ekstre genel `AccountingReportRowDto` ile dönüyor; hareket türü, kaynak türü, belge referansı, açıklama, açılış/kapanış ve yürüyen bakiye alanları yayımlanmıyor. `tertiaryAmount` mevcut uygulamada açık tutarı temsil ediyor fakat bu anlam DTO sözleşmesinden çıkarılamıyor.
- Önerilen sözleşme: Borç, alacak ve açık tutarı adlandırılmış alanlarla veren, hareket/kaynak/refans semantiği açık cari-ekstre DTO'su yayımlansın. Yürüyen bakiye sunulacaksa açılış bakiyesi ve sıralama garantisiyle backend tarafından hesaplanmalı.
- Kabul ölçütleri:
  1. Her kolonun mali anlamı ve para birimi davranışı OpenAPI ile Markdown'da açık olmalı.
  2. Hareket türü, kaynak türü, kaynak kimliği/numarası ve açıklama nullable kurallarıyla yayımlanmalı.
  3. Tarih sıralaması ve eşit tarihteki stabil tie-breaker belgelenmeli.
  4. Bilinmeyen cari kimliğinin `404` mü yoksa boş `200` mü döndüreceği kesinleştirilmeli.
  5. Sayfalama, tarih filtresi ve varsa yürüyen bakiye aynı veri kümesi üzerinde integration testlerle doğrulanmalı.

## ADM-ACC-003 — Cari hesap güncelleme eşzamanlılık koruması

- Öncelik: Yüksek
- Durum: Açık veri kaybı riski
- Etkilenen arayüz: Cari hesap düzenleme
- Değişmeyen kimlik: `ADM-ACC-003`
- Problem: CurrentAccount response ve update isteğinde concurrency token bulunmuyor. İki yönetici aynı kaydı düzenlediğinde son yazan önceki değişikliği sessizce ezebilir.
- Önerilen sözleşme: Response'a opaque concurrency token eklenip PUT isteğinde zorunlu gönderilsin; stale token stabil typed `409 concurrency_conflict` üretsin.
- Kabul ölçütleri:
  1. GET detail güncel concurrency tokenı döndürmeli ve PUT bu tokenı zorunlu almalı.
  2. Başarılı PUT yeni tokenı dönmeli.
  3. Stale token veriyi değiştirmeden typed `409` ve güncel kaydı yeniden okumaya yeterli güvenli hata bilgisi üretmeli.
  4. OpenAPI, Markdown ve eşzamanlı integration testi aynı davranışı doğrulamalı.

## ADM-ACC-004 — CurrentAccount create HTTP/OpenAPI sözleşme eşitliği

- Öncelik: Orta
- Durum: Açık dokümantasyon drift'i
- Etkilenen arayüz: Cari hesap oluşturma ve generated client
- Değişmeyen kimlik: `ADM-ACC-004`
- Problem: Runtime create işlemi `201 Created` ve `Location` header döndürüyor; yayımlanan OpenAPI/Markdown yalnız `200` gösterebiliyor ve request body zorunluluğu farklı ifade ediliyor.
- Önerilen sözleşme: Runtime davranışı korunacaksa OpenAPI ve Markdown `201`, response DTO, `Location`, zorunlu body ve bütün ProblemDetails cevaplarını birebir yayımlamalı.
- Kabul ölçütleri:
  1. Başarı status'u, response gövdesi ve `Location` header üç sözleşme kaynağında aynı olmalı.
  2. Request body zorunluluğu ve nullable alanlar runtime validator ile eşleşmeli.
  3. `400`, `401`, `403`, `404` ve `409` ProblemDetails şemaları ve stabil kodları belgelenmeli.
  4. OpenAPI drift kontrolü CurrentAccount operasyonlarını kapsamalı.

## ADM-ACC-005 — Alış faturası draft edit round-trip sözleşmesi

- Öncelik: Kritik
- Durum: Doğrulanmış veri kaybı riski; Admin mevcut taslağı düzenlemeye açmıyor
- Etkilenen arayüz: `/accounting/purchase-invoices/[purchaseInvoiceId]/edit`
- Değişmeyen kimlik: `ADM-ACC-005`
- Problem: `PurchaseInvoiceHeaderInput` başlık indirim türü/değeri/vergi bazını; `PurchaseInvoiceLineInput` satır indirim yapılandırması ile `isInvoiceDiscountEligible` değerini alıyor. Buna karşılık `PurchaseInvoiceDto` ve `PurchaseInvoiceLineDto` bu kaynak alanları geri döndürmüyor. GET ile açılan taslağın PUT payload'ına dönüştürülmesi görünmeyen indirim/uygunluk değerlerini silebilir; toplamın sıfır olması, sıfır değerli bir indirim tanımı bulunmadığını kanıtlamaz. Response/update request ayrıca concurrency token taşımadığı için iki admin last-write-wins riski altındadır.
- Önerilen sözleşme: Detail DTO bütün editable kaynak alanlarını ve opaque concurrency tokenı döndürsün; PUT aynı tokenı zorunlu alsın ve successful response yeni tokenı taşısın.
- Kabul ölçütleri:
  1. GET → değişiklik yok → PUT round-trip bütün header/line commercial alanlarını birebir korumalı.
  2. Satır kimliği ve snapshot alanları immutable kalırken editable indirim alanları açıkça yayımlanmalı.
  3. Stale token hiçbir alanı değiştirmeden `409 concurrency_conflict` üretmeli.
  4. OpenAPI, Markdown, mapper, validator ve SQL Server integration testleri aynı sözleşmeyi doğrulamalı.

## ADM-ACC-006 — Fatura giderinin post sırasında FIFO maliyetinden silinmesi

- Öncelik: Kritik
- Durum: Doğrulanmış muhasebe doğruluğu hatası; Admin giderli faturada post işlemini engelliyor
- Etkilenen arayüz: Alış faturası gider dağıtımı ve muhasebeleştirme
- Değişmeyen kimlik: `ADM-ACC-006`
- Problem: Fatura gideri satırların allocated expense ve final cost değerlerini güncelliyor. `PostPurchaseInvoice` ise posting öncesi `CalculateInvoice` çağırarak bu değerleri sıfırlıyor ve FIFO katmanlarını gider hariç final maliyetle üretiyor. Aynı reset, giderli taslağın header/satır güncellemesinde de mevcut gider allocation kayıtları yeniden uygulanmadan gerçekleşiyor.
- Önerilen uygulama: Belge commercial toplamları yeniden hesaplandıktan sonra mevcut `PurchaseInvoiceExpenseAllocation` kayıtları deterministik biçimde yeniden uygulanmalı; post bu authoritative final cost üzerinden layer/history üretmeli ve bütün işlem tek transaction içinde kalmalı.
- Kabul ölçütleri:
  1. Gider ekleme öncesi/sonrası final cost farkı post edilen InventoryCostLayer birim maliyetine aynen yansımalı.
  2. Draft header/satır edit'i mevcut giderleri kaybetmeden final maliyeti yeniden hesaplamalı veya açık bir business rule ile engellenmeli.
  3. Birden fazla gider ve üç allocation yöntemi için rounding toplamları korunmalı.
  4. Posting retry aynı layer/history etkisini yalnız bir kez oluşturmalı.
  5. Domain, handler ve SQL Server integration testleri supplier debt'in giderden etkilenmediğini, FIFO maliyetinin etkilendiğini doğrulamalı.

## ADM-ACC-007 — Alış faturasındaki mevcut stok tahsisini bağlama duyarlı düzenleme

- Öncelik: Yüksek
- Durum: Açık sözleşme eksikliği
- Etkilenen arayüz: Alış faturası satır tahsisi
- Değişmeyen kimlik: `ADM-ACC-007`
- Problem: Available movement sorgusu tamamen tahsisli hareketi, mevcut satırın kendi tahsisi olsa bile listeden çıkarıyor. Detail allocation DTO'su yalnız movement ID ve miktar taşıdığı için UI hareket tarihi/fiziksel kapasite/kullanılabilir kapasiteyi yeniden kuramıyor. Ayrıca `PUT allocations` boş diziyi reddettiği için tahsisi temizleme operasyonu yok.
- Önerilen sözleşme: Available endpoint `purchaseInvoiceLineId` bağlamını alarak satırın kendi tahsisini kapasiteye geri eklesin veya allocation response hareket snapshot/kapsam alanlarıyla zenginleşsin; açık tahsis temizleme davranışı yayımlansın.
- Kabul ölçütleri:
  1. Mevcut tam tahsis, edit ekranında hareket kapasitesiyle birlikte güvenle yeniden gösterilebilmeli.
  2. Self-allocation başka faturaların allocation'larından ayrı hesaplanmalı.
  3. Clear/remove davranışı atomik ve açık endpoint/body sözleşmesine sahip olmalı.
  4. Eşzamanlı başka tahsis `409` üretmeli ve authoritative current capacity yeniden okunabilmeli.

## ADM-ACC-008 — İptal edilmiş alış faturası tahsislerinin Purchase hareketini kilitlemesi

- Öncelik: Kritik
- Durum: Doğrulanmış stok maliyetleme kullanılabilirlik hatası
- Etkilenen arayüz: Posted alış faturası iptali ve sonraki stok tahsisi
- Değişmeyen kimlik: `ADM-ACC-008`
- Problem: İptal işlemi maliyet katmanını geçersizleştirip supplier debt ters kaydı oluşturuyor fakat `PurchaseInvoiceStockAllocation` kayıtlarını etkinlikten düşürmüyor. Available movement hesabı fatura statusünü ayırmadan bütün allocation'ları topladığı için iptal edilmiş faturanın tahsis ettiği Purchase hareketi yeniden kullanılamıyor.
- Önerilen uygulama: Allocation'ın etkinliği kaynak fatura lifecycle'ına göre hesaplanmalı veya iptalde tarihsel bağı koruyan açık reversal/invalidation kaydı oluşturulmalı; cancelled allocation kullanılabilir kapasiteyi tüketmemeli.
- Kabul ölçütleri:
  1. İptal edilen tam tahsisli faturanın hareketi aynı miktarla yeniden tahsise açılmalı.
  2. Tarihsel fatura-allocation ilişkisi silinmeden audit edilebilmeli.
  3. İkinci iptal idempotent kalmalı ve kapasiteyi iki kez artırmamalı.
  4. Aktif Posted/Draft allocation'lar kapasiteyi tüketmeye devam etmeli.

## ADM-ACC-009 — Gider geçmişi projeksiyonu ve create idempotency

- Öncelik: Yüksek
- Durum: Açık sözleşme eksikliği
- Etkilenen arayüz: Fatura giderleri, genel gider defteri ve kategori seçimi
- Değişmeyen kimlik: `ADM-ACC-009`
- Problem: `PurchaseInvoiceExpenseDto` kategori adı/kodu, KDV oranı, açıklama ve audit alanlarını döndürmüyor; `ExpenseDto` da yalnız categoryId taşıyor. Sayfalı kategori listesinin ilk 100 kaydı bütün tarihsel kayıtları güvenilir biçimde adlandıramaz. Fatura gideri, genel gider ve kategori create endpointleri idempotency anahtarı almadığından timeout sonrası kullanıcı retry'ı düzeltilemeyen duplicate append-only kayıt üretebilir.
- Önerilen sözleşme: Gider DTO'larına kategori snapshot/projection, KDV oranı, açıklama ve audit alanları eklensin; create intentleri zorunlu `Idempotency-Key` ile desteklenip aynı key/same body replay davranışı yayımlansın.
- Kabul ölçütleri:
  1. Gider geçmişi ek lookup olmadan kategori kodu/adı, açıklama, KDV ve oluşturulma bilgisini gösterebilmeli.
  2. Aynı key ve aynı body aynı sonucu; aynı key farklı body stabil conflict sonucunu üretmeli.
  3. OpenAPI header'ı zorunlu, body ve response şemalarını eksiksiz göstermeli.
  4. KDV validator'ları dokümandaki `0..100` sınırını ve fatura gideri açıklamasında 500 karakteri uygulamalı.

## ADM-ACC-010 — Satış ve fatura iptal lifecycle sözleşmesinin runtime ile eşitlenmesi

- Öncelik: Kritik
- Durum: Doğrulanmış dokümantasyon/runtime çelişkisi; Admin bağımsız fatura iptalini açmıyor
- Etkilenen arayüz: `/accounting/sales-orders/[salesOrderId]`, `/accounting/sales-invoices/[salesInvoiceId]`
- Değişmeyen kimlik: `ADM-ACC-010`
- Problem: Accounting dokümanları Draft ve Posted `SalesInvoice` için bağımsız iptal akışı tarif ediyor. Runtime ise `AccountingSalesOrder` iptalini yalnız `Posted` durumda kabul ediyor; fatura iptalini de bağlı satış zaten `Cancelled` değilse `409` ile reddediyor. Normal satış iptali bağlı faturayı aynı transaction içinde zaten iptal ediyor. Dokümandaki akışın UI'ya uygulanması, kullanıcıya sürekli başarısız veya yanlış kapsamlı bir aksiyon sunar.
- Önerilen sözleşme: Tek lifecycle matrisi yayımlansın: Draft belgede düzenleme/post, yalnız Posted `AccountingSalesOrder` üzerinde cancel; `SalesInvoice` cancel ise ya endpoint'ten kaldırılmalı ya da yalnız recovery/replay davranışı olarak açıkça işaretlenmelidir. Posted satıştan `from-order` ile oluşturulan faturanın doğrudan Posted olduğu da belgelenmelidir.
- Kabul ölçütleri:
  1. Order ve invoice için durum × aksiyon matrisi OpenAPI açıklaması, Markdown ve domain guard'larıyla aynı olmalı.
  2. Draft order/invoice cancel davranışı ve hata status/kodu kesinleştirilmeli.
  3. Bağımsız invoice cancel normal iş akışında desteklenmiyorsa endpoint kullanıcı aksiyonu olarak yayımlanmamalı.
  4. Satış iptali stok reversal, FIFO restore, müşteri alacağı reversal ve bağlı fatura iptalini tek idempotent işlemde doğrulamalı.
  5. Geçerli müşteri tahsilat tahsisi engeli stabil typed `409` koduyla yayımlanmalı.

## ADM-ACC-011 — Satış tam-liste düzenleme concurrency ve reversal projeksiyonu

- Öncelik: Yüksek
- Durum: Açık veri kaybı ve denetim izi eksikliği
- Etkilenen arayüz: Muhasebe satışı/satış faturası Draft düzenleme ve Cancelled detay
- Değişmeyen kimlik: `ADM-ACC-011`
- Problem: Sales order ve invoice detail DTO'ları editable commercial alanları round-trip için taşısa da update request/response'larında concurrency token yoktur; paralel yöneticiler tam satır listesini son-yazan-kazan biçiminde sessizce ezebilir. Bulk veya tekil satır mutation'ı entity kimliklerini yeniden üretebildiği için eski item/line ID'leri de mutation sonrasında geçersizleşebilir. Cancelled detaylar orijinal movement/consumption kayıtlarını taşırken reversal hareketi, restore edilen katman ve finansal reversal bağlarını ayrıca projekte etmez.
- Önerilen sözleşme: Order/invoice response'larına opaque concurrency token eklenip bütün update mutation'larında zorunlu doğrulansın; her başarı yeni token ve authoritative tam belge döndürsün. Cancellation detail DTO'su orijinal ve reversal kayıtlarını açık yön/rol alanlarıyla ilişkilendirsin.
- Kabul ölçütleri:
  1. Stale tam-liste PUT hiçbir başlık veya satırı değiştirmeden typed `409 concurrency_conflict` üretmeli.
  2. Mutation response'u yeni token ve güncel item/line ID'leriyle tam belgeyi dönmeli.
  3. OpenAPI, Markdown ve SQL Server yarış testleri aynı davranışı doğrulamalı.
  4. Cancelled order detail; stok reversal, FIFO restore ve current-account reversal kimliklerini audit edilebilir biçimde yayımlamalı.
  5. Raw `cancelledBy` kimliği yerine çözümlenebilir kullanıcı snapshot'ı veya açık admin referans sözleşmesi sağlanmalı.

## ADM-ACC-012 — Satış create/OpenAPI/idempotency sözleşme drift'i

- Öncelik: Yüksek
- Durum: Doğrulanmış üretilmiş sözleşme sapması; Admin runtime kuralını elle uyguluyor
- Etkilenen arayüz: Muhasebe satışı ve doğrudan satış faturası oluşturma, generated client
- Değişmeyen kimlik: `ADM-ACC-012`
- Problem: Runtime `POST /sales-orders`, `POST /sales-invoices` ve `POST /sales-invoices/from-order/{id}` işlemlerinde `201 Created + Location` döndürüyor; yayımlanan OpenAPI/endpoint Markdown çoğunlukla yalnız `200` gösteriyor. İlk iki create'te `Idempotency-Key` runtime'da zorunlu, en fazla 100 karakter ve `[A-Za-z0-9_-]+` iken generated sözleşmede opsiyonel. Bazı Markdown örnekleri array olan `lines/items` alanlarını object olarak gösteriyor; sales-order cancel başarı response'u da generated şemada gerçek `CancellationResultDto` yerine ProblemDetails olarak üretilebiliyor.
- Önerilen sözleşme: Controller metadata, OpenAPI ve endpoint Markdown runtime ile birebir eşitlensin; zorunlu idempotency header, `201`, `Location`, request body zorunluluğu, nullable enumlar ve gerçek success DTO'ları yayımlansın.
- Kabul ölçütleri:
  1. Aynı key + aynı normalize payload aynı kaydı; aynı key + farklı payload stabil typed conflict'i döndürmeli.
  2. Header zorunluluğu, regex/uzunluk ve global uniqueness kapsamı iki create operasyonunda belgelenmeli.
  3. `from-order` operasyonunun header almaması ve same-header replay/different-header conflict davranışı açıkça yayımlanmalı.
  4. Üç create operasyonunun `201`, response gövdesi ve `Location` header'ı OpenAPI drift testine eklenmeli.
  5. Request array örnekleri ve order cancel success şeması generated client ile runtime integration testinde doğrulanmalı.

## ADM-ACC-013 — Payment idempotency intent ve parasal precision bütünlüğü

- Öncelik: Kritik
- Durum: Doğrulanmış sessiz yanlış-kayıt ve yuvarlama riski; Admin aynı intent anahtarını alan değişiminde yeniliyor ve kuruş hassasiyetini zorunlu tutuyor
- Etkilenen arayüz: Müşteri tahsilatı ve tedarikçi ödemesi oluşturma
- Değişmeyen kimlik: `ADM-ACC-013`
- Problem: Aynı `Idempotency-Key` farklı payload ile kullanıldığında handler payload karşılaştırmadan eski Payment'ı döndürüyor. Validator tutarlarda iki ondalık zorlamazken persistence `decimal(18,2)` saklıyor; uygulamada eşit görünen çok ondalıklı allocation toplamları DB yuvarlamasında ayrışabilir.
- Önerilen sözleşme: Aynı key + normalize aynı payload aynı Payment'ı, aynı key + farklı payload stabil typed `409 idempotency_conflict` döndürmeli. Payment ve allocation tutarları validator, OpenAPI ve DB'de aynı `decimal(18,2)` sınırına sahip olmalı.
- Kabul ölçütleri:
  1. Cari, type, tutar, tarih, finans hesabı veya allocation değişen replay hiçbir zaman eski Payment'ı başarı gibi döndürmemeli.
  2. 18,2 sınırı ve midpoint rounding davranışı integration testlerle doğrulanmalı.
  3. Header runtime, OpenAPI ve Markdown'da zorunlu olmalı; create başarısı `201 + Location` olarak eşitlenmeli.

## ADM-ACC-014 — İptal edilen Payment allocation denetim izi projeksiyonu

- Öncelik: Kritik
- Durum: Doğrulanmış tarihsel veri projeksiyonu kaybı; Admin iptal edilen `unallocatedAmount` değerini avans olarak etiketlemiyor
- Etkilenen arayüz: Ödeme detayı ve iptal sonrası denetim izi
- Değişmeyen kimlik: `ADM-ACC-014`
- Problem: DTO `isReversed/reversedAt` taşımasına rağmen mapper yalnız geçerli allocation'ları döndürüyor. İptal sonrası `allocations=[]`, allocated `0`, unallocated payment amount olur; özgün dağıtım hedefleri ve terslenme zamanı görünmez.
- Önerilen sözleşme: Payment detail bütün allocation geçmişini, aktif/terslenmiş durumunu ve reversal bağlarını döndürmeli; özet değerleri durum semantiğiyle birlikte adlandırılmalı.
- Kabul ölçütleri:
  1. Cancel sonrası özgün allocation ID, hedef cari hareket, tutar, reversedAt ve reversal kaydı okunabilmeli.
  2. Cancelled payment'ın unallocated değeri kullanılabilir avans gibi yorumlanmamalı.
  3. İkinci cancel aynı denetim izini değiştirmeden idempotent sonuç vermeli.

## ADM-ACC-015 — Manuel finans hareketi tip matrisi ve tek-bacaklı transfer açığı

- Öncelik: Kritik
- Durum: Doğrulanmış dokümantasyon/runtime çelişkisi; Admin güvenli kesişim allowlist'ini kullanıyor
- Etkilenen arayüz: Manuel finans hareketi
- Değişmeyen kimlik: `ADM-ACC-015`
- Problem: Doküman POS `30` tipini manuel kabul edip transfer `20/21` tiplerini atomik endpoint'e ayırıyor; runtime POS'u reddedip `20/21` tiplerini genel endpoint'te kabul ediyor. Bu, tek bacaklı banka transferi ile finansal bütünlüğün bozulmasına izin veriyor.
- Önerilen sözleşme: Genel manuel endpoint `10,11,40,41,50` allowlist'iyle sınırlandırılmalı; POS kararı eşitlenmeli; `20/21` yalnız atomik banka transferi handler'ında üretilebilmeli.
- Kabul ölçütleri:
  1. Genel POST üzerinde 20/21 stabil typed `400` üretmeli.
  2. Doküman, validator, enum matrisi ve integration testleri aynı allowlist'i doğrulamalı.
  3. Transfer iki farklı aktif TRY banka hesabında aynı source ID ile tam iki bacak üretmeli.

## ADM-ACC-016 — Atomik banka transferi reversal bütünlüğü

- Öncelik: Kritik
- Durum: Doğrulanmış toplam-para bütünlüğü riski; Admin transfer ve reversal satırlarında tekil tersleme sunmuyor
- Etkilenen arayüz: Banka ekstresi ve finans hareketi reversal
- Değişmeyen kimlik: `ADM-ACC-016`
- Problem: Tekil reversal handler transferin yalnız bir bacağını ve reversal satırını yeniden tersleyebiliyor. Tek bacağın terslenmesi toplam finans bakiyesini yapay biçimde artırabilir veya azaltabilir; statement DTO'su karşı hesap/paired transaction bağını da taşımıyor.
- Önerilen sözleşme: Transfer çifti tek operasyonla atomik terslenmeli; tekil reverse `20/21/60/61` tiplerini reddetmeli. Statement DTO karşı hesap ve paired transaction kimliğini yayımlamalı.
- Kabul ölçütleri:
  1. Transfer reversal iki bacağı tek transaction'da ve yalnız bir kez oluşturmalı.
  2. Tek-bacak ve reversal-of-reversal denemeleri hiçbir bakiye değiştirmeden typed conflict üretmeli.
  3. Ekstre iki bacağı ortak transfer kimliği ve karşı hesapla ilişkilendirebilmeli.

## ADM-ACC-017 — Finans hesabı detail, sayfalı ekstre ve retry uzlaştırma sözleşmesi

- Öncelik: Yüksek
- Durum: Açık ölçeklenebilirlik ve belirsiz-sonuç kurtarma eksikliği
- Etkilenen arayüz: Kasa/banka defteri, ekstre ve manuel/transfer create
- Değişmeyen kimlik: `ADM-ACC-017`
- Problem: Hesap detail endpoint'i yok; bilinmeyen statement ID boş `200` döndürüyor. Listeler/ekstreler sayfasız. Manual/transfer aynı UUID replay'ında özgün DTO yerine `409` verir; timeout sonrası sonuç yalnız statement içindeki source ID taranarak uzlaştırılabilir. Cash/bank create runtime `201` iken OpenAPI `200` gösterebilir ve idempotency desteklemez.
- Önerilen sözleşme: Native detail/404, tarih filtreli sayfalı statement ve sourceId lookup sağlansın; mutation idempotency same-key/same-body replay döndürsün. Runtime status/header/body zorunlulukları OpenAPI ile eşitlensin.
- Kabul ölçütleri:
  1. Bilinmeyen hesap detail/statement stabil `404`, var olan boş hesap `200 []` üretmeli.
  2. Ekstre sıralaması `transactionDate, createdAt, id` ve sayfalama boyunca deterministik olmalı.
  3. Timeout sonrası source ID ile tek manual hareket veya tam transfer çifti güvenle bulunabilmeli.
  4. Cash/bank create `201 + Location` ve duplicate code/IBAN hata semantiği yayımlanmalı.

## ADM-ACC-018 — Raporlara özgü şema, filtre ve satır kimliği sözleşmesi

- Öncelik: Yüksek
- Durum: Açık raporlama sözleşmesi eksikliği; Admin rapora özel allowlist ve kolon kataloğu kullanıyor
- Etkilenen arayüz: `/accounting/reports/*`
- Değişmeyen kimlik: `ADM-ACC-018`
- Problem: 28 rapor aynı `AccountingReportRowDto` şemasını kullanıyor; `amount`, `secondaryAmount`, `tertiaryAmount`, `quantity` ve `rate` alanlarının anlamı endpoint'e göre değişiyor. OpenAPI bütün raporlara aynı `Search`, `Id`, `HasSalesInvoice`, `From` ve `To` filtrelerini yayımlasa da birçok rapor ilgili alanı üretmediği için bazı filtreler anlamsız veya sürekli boş sonuç veriyor. Ürün kârlılığı ve KDV grupları her satırda `Guid.Empty` döndürdüğünden `id` tekil satır kimliği değildir.
- Önerilen sözleşme: Her rapor için anlamlı alan adları taşıyan ayrı response DTO'su ve yalnız desteklenen query parametreleri yayımlansın; gruplanmış satırlar ürün kimliği veya KDV oranı + dönem gibi deterministik bir satır anahtarı taşısın. Para alanlarının KDV dahil/hariç ve kâr/maliyet semantiği OpenAPI description alanlarında açıkça belirtilsin.
- Kabul ölçütleri:
  1. Her endpoint yalnız gerçekten uyguladığı filtreleri OpenAPI'de yayımlamalı ve geçersiz filtreyi sessizce yok saymamalı.
  2. Satış, alış, FIFO, cari, nakit ve KDV response'ları finansal alanları kendi adlarıyla taşımalı.
  3. Her rapor satırı aynı sonuç kümesi içinde stabil ve tekil bir anahtara sahip olmalı.
  4. Tarih filtresinin hangi iş tarihine uygulandığı ve sınırların inclusive/exclusive davranışı endpoint bazında belgelenmeli.
  5. Contract testleri OpenAPI, reader projection ve SQL Server sonucundaki alan/filtre anlamlarını karşılaştırmalı.

## ADM-ACC-019 — Rapor sorgularında veritabanı sayfalaması ve hesaplanmış özet sözleşmesi

- Öncelik: Kritik
- Durum: Doğrulanmış ölçeklenebilirlik riski; Admin yalnız API `totalCount` değerini kayıt sayısı olarak gösteriyor
- Etkilenen arayüz: 28 muhasebe raporu ve muhasebe genel bakış kuyrukları
- Değişmeyen kimlik: `ADM-ACC-019`
- Problem: `AccountingReportReader` raporların temel kayıtlarını `ToListAsync` ile belleğe aldıktan sonra ortak arama, filtre, `Count`, `Skip` ve `Take` işlemlerini uyguluyor. Veri büyüdükçe tek bir sayfa isteği bütün aday satırları API belleğine taşıyabilir. Sözleşme ayrıca dönem geneli finansal toplamları taşımadığından sayfadaki satırları toplamak yanlış sonuç üretir.
- Önerilen uygulama: Filtre, deterministik sıralama, count ve pagination mümkün olan bütün raporlarda SQL sorgusuna itilmelidir. Dönem geneli tutar gerekiyorsa para birimi ve KDV semantiği açık, backend tarafından hesaplanan ayrı `summary` alanı veya ayrı özet endpoint'i sağlanmalıdır.
- Kabul ölçütleri:
  1. PageSize 20 isteği filtre öncesi bütün aday kayıtları uygulama belleğine almamalı.
  2. Sıralama; tarih, belge/varlık kimliği ve grup anahtarıyla sayfalar arasında deterministik olmalı.
  3. `totalCount` filtrelenmiş satır sayısını SQL tarafında üretmeli ve page boundary testleriyle doğrulanmalı.
  4. Finansal summary sunulursa bütün filtrelenmiş sonuç üzerinden, para birimi ayrımını koruyarak hesaplanmalı; page subtotal ile karıştırılmamalı.
  5. Büyük veri seti için SQL Server integration ve performans bütçesi testi eklenmeli.

## STO-PAY-001 — Terk edilen iyzico CheckoutForm oturumunu güvenli biçimde sonlandırma

- Öncelik: Yüksek / checkout rezervasyon kurtarma engeli
- Durum: Çözüldü (2026-08-24; atomik terk etme, rezervasyon bırakma ve geç tahsilat mutabakatı uygulandı)
- Etkilenen arayüz: Storefront `/checkout` bekleyen ödeme modalı, `/checkout/confirmation/[orderId]` ve guest/member sipariş iptali
- Değişmeyen kimlik: `STO-PAY-001`
- Uygulanan sözleşme:
  1. “Ödemeye devam et” aynı `orderId + Idempotency-Key` ile mevcut ve henüz terk edilmemiş CheckoutForm oturumunu yeniden açar; ikinci sipariş veya ikinci rezervasyon oluşturmaz.
  2. “Siparişi iptal et” önce tokenı iyzico CF-Retrieve ile doğrular. İmzalı ve siparişle eşleşen sonuç kesin `Paid` değilse ödeme denemesi müşteri tarafından terk edilmiş olarak işaretlenir; Payment `Cancelled`, Order `Cancelled (6)` olur ve stok, kupon ile notification/outbox etkileri tek serializable transaction içinde yalnız bir kez uygulanır.
  3. Provider sonucu kesin `Paid` ise terk etme uygulanmaz; authoritative ödeme sonucu yerelde tamamlanır ve refundsuz sipariş iptali `409` ile reddedilir.
  4. Retrieve timeout'u, bağlantı hatası veya kimlik/tutar bütünlüğü belirsizliği halinde hiçbir yerel durum değiştirilmez ve mevcut güvenli `409` sözleşmesi korunur.
  5. iyzico açık fakat tahsil edilmemiş CheckoutForm tokenını geçersiz kılan bir API yayımlamadığı için terk edilmiş token kalıcı olarak izlenir. Callback/webhook veya bounded worker daha sonra kesin tahsilat görürse sipariş yeniden açılmaz; provider `paymentId` üzerinden iyzico cancel çağrısı yapılır ve sonuç audit alanlarına kaydedilir.
  6. Açık token kesin başarısız olduğunda veya kullanım süresi güvenlik payıyla dolduğunda izleme terminal tamamlanır. Geçici retrieve/cancel hataları kısa lease ve yeniden deneme planıyla tekrar işlenir.
  7. Kullanıcı iptali başarıyla tamamlandıktan sonra eski CheckoutForm URL'si storefront tarafından yeniden açılmaz; yeni alışveriş bağımsız bir checkout ve idempotency anahtarıyla başlar.
  8. Üye ve guest endpointleri aynı yaşam döngüsünü kullanır; unit, provider gateway ve persistence integration testleri tekrar istek, stok bırakma ve geç tahsilat geri çevirme davranışını kapsar.
