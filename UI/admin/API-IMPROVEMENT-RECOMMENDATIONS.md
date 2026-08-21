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
