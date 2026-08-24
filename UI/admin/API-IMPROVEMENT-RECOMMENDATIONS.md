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
