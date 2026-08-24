# Admin Ön Muhasebe Modülü Geliştirme Planı

## Belge durumu

- Durum: MVP 1, MVP 3, MVP 4 ve MVP 5 tamamlandı; MVP 2 güvenli frontend dilimi tamamlandı ve kritik API sözleşmeleri bekleniyor
- Hedef uygulama: `UI/admin`
- Modül kökü: `UI/admin/src/modules/accounting`
- Route kökü: `/accounting`
- Son güncelleme: 2026-08-24
- Kapsam sahibi: Admin Ön Muhasebe

Bu belge, Admin Paneli içindeki ön muhasebe modülünün ana takip kaynağıdır. MVP ilerlemesi, sözleşme kararları, açık riskler ve doğrulama sonuçları burada güncellenecektir.

## 1. Temel kararlar

- Ön muhasebe, Admin Paneline entegre fakat ayrı bir bounded module olarak geliştirilecektir.
- Ayrı bir DNS veya web subdomain'i oluşturulmayacaktır. Bu belgedeki subdomain kavramı iş alanı ve kod sahipliği sınırını ifade eder.
- Route'larda ayrı Admin uygulaması kararı gereği `/admin` prefix'i kullanılmayacaktır.
- Tüm muhasebe ekranları `AdminOnly` yetkilendirmesi altında kalacaktır.
- `AccountingSalesOrder`, e-ticaret `Order`, `Cart` ve `User` akışından tamamen ayrı tutulacaktır.
- Muhasebe modülü `orders`, `customers`, `inventory` veya diğer feature modüllerinin private dosyalarını import etmeyecektir.
- Ürün ve varyant seçimi mevcut katalog API'sinden accounting-owned lookup/adapter üzerinden yapılacaktır; ikinci ürün modeli oluşturulmayacaktır.
- API; bakiye, KDV, indirim, toplam, ödenen/kalan, FIFO maliyeti ve kâr için tek otoritedir.
- Admin/auth/accounting verisi shared cache'e alınmayacak; varsayılan `no-store` olacaktır.
- Kaynak kayıtlar iptal veya reversal sonrasında silinmeyecek, geçmiş görünür kalacaktır.

## 2. Kod ve subdomain yapısı

```text
src/
  app/
    (admin)/
      accounting/
        page.tsx
        current-accounts/
        purchase-invoices/
        sales-orders/
        sales-invoices/
        payments/
        treasury/
        expenses/
        costing/
        reports/
  modules/
    accounting/
      core/
      current-accounts/
      purchases/
      sales/
      treasury/
      costing/
      reports/
```

Subdomain sorumlulukları:

| Subdomain | Sorumluluk |
| --- | --- |
| `core` | Muhasebe enum/presentation, ortak formatlama ve ProblemDetails davranışı; lifecycle/idempotency ilk gerçek tüketicisinde eklenecek |
| `current-accounts` | Müşteri, tedarikçi ve karma cari master verisi ile cari ekstre |
| `purchases` | Alış faturaları, stok hareketi tahsisi, alış giderleri ve genel giderler |
| `sales` | Muhasebe satış siparişleri ve opsiyonel satış faturaları |
| `treasury` | Ödeme, tahsilat, kasa, banka, ekstre, transfer ve finansal reversal |
| `costing` | Açılış maliyet katmanı, FIFO ve varyant maliyet geçmişi |
| `reports` | Rapor kataloğu, rapora özgü filtre ve kolon haritaları |

## 3. Route haritası

```text
/accounting
/accounting/current-accounts
/accounting/current-accounts/new
/accounting/current-accounts/[currentAccountId]
/accounting/current-accounts/[currentAccountId]/edit
/accounting/purchase-invoices
/accounting/purchase-invoices/new
/accounting/purchase-invoices/[purchaseInvoiceId]
/accounting/purchase-invoices/[purchaseInvoiceId]/edit
/accounting/sales-orders
/accounting/sales-orders/new
/accounting/sales-orders/[salesOrderId]
/accounting/sales-orders/[salesOrderId]/edit
/accounting/sales-invoices
/accounting/sales-invoices/new
/accounting/sales-invoices/[salesInvoiceId]
/accounting/sales-invoices/[salesInvoiceId]/edit
/accounting/payments
/accounting/payments/new
/accounting/payments/[paymentId]
/accounting/treasury
/accounting/expenses
/accounting/costing
/accounting/reports
/accounting/reports/[report]
```

Yalnız aktif MVP'nin route'ları oluşturulacaktır. Henüz geliştirilmemiş navigasyon öğeleri linksiz ve `Geliştirme aşamasında` durumunda kalacaktır.

## 4. Sözleşme kapısı

Her MVP başlamadan önce ilgili OpenAPI operasyonu, endpoint Markdown belgesi, controller, DTO ve validator karşılaştırılacaktır. Çelişki halinde frontend tahminde bulunmayacak; bulgu `API-IMPROVEMENT-RECOMMENDATIONS.md` dosyasına kaydedilecektir.

### Çözülmesi gereken kritik lifecycle kararı

Doküman ve runtime arasında iptal davranışı çelişmektedir:

- Doküman, Draft belgelerde iptali destekliyor.
- Runtime, AccountingSalesOrder ve PurchaseInvoice iptalini yalnız `Posted` durumunda kabul ediyor.
- Doküman bağımsız SalesInvoice iptalini anlatıyor.
- Runtime, SalesInvoice-only iptalini reddediyor ve bağlı AccountingSalesOrder'ın iptal edilmesini istiyor.

Önerilen karar:

- [x] Draft belgede iptal aksiyonu gösterilmez.
- [x] AccountingSalesOrder yalnız `Posted` durumunda iptal edilir.
- [x] PurchaseInvoice yalnız `Posted` durumunda iptal edilir.
- [x] SalesInvoice bağımsız iptal edilmez; bağlı AccountingSalesOrder üzerinden iptal edilir.
- [ ] API dokümanı, OpenAPI açıklaması ve action matrisi seçilen davranışla eşitlenir.

Bu karar MVP 2 ve MVP 3 lifecycle aksiyonları uygulanmadan önce kesinleştirilmelidir.

### Bilinen diğer sözleşme riskleri

- CurrentAccount listesinde `search`, type/active filter ve serbest sıralama yoktur.
- Tahsise açık cari hareketler için kilitli seçim endpoint'i yoktur; rapor satırları yarış durumuna açıktır.
- Bazı create controller'ları runtime'da `201` döndürürken OpenAPI/Markdown `200` gösterebilir. İstemci tüm başarılı `2xx` durumlarını kabul etmelidir.
- Accounting ProblemDetails response şemaları OpenAPI'de eksiktir; mevcut shared güvenli hata modeli kullanılacaktır.
- Zorunlu beş `Idempotency-Key` header'ı OpenAPI'de opsiyonel görünebilir.
- `sales-invoices/from-order/{id}` idempotency anahtarı almaz; otomatik retry yapılmayacaktır.
- Yirmi sekiz raporun tamamı için alan semantiği eksiksiz yayımlanmış değildir; yalnız doğrulanmış kolon haritasına sahip raporlar açılacaktır.
- Accounting overview aggregate endpoint'i yoktur; sahte para KPI'ları üretilmeyecektir.
- Genel gider, kasa, banka ve kategori için belgelenmemiş update/delete/lifecycle aksiyonu eklenmeyecektir.

## 5. MVP takibi

### MVP 1 — Muhasebe temeli ve cari hesaplar

Durum: `Tamamlandı`

Kapsam:

- [x] Muhasebe navigasyonunu aktif route durumuna hazırlama
- [x] Aktif muhasebe route'unda parent navigasyonu otomatik açık tutma
- [x] `/accounting` çalışma merkezi
- [x] Cari hesap liste ve server-side pagination
- [x] Cari hesap oluşturma
- [x] Cari hesap detay
- [x] Cari hesap güncelleme ve aktif/pasif davranışı
- [x] Müşteri, tedarikçi ve karma hesap ayrımı
- [x] Cari ekstre
- [x] Merkezi numeric enum ve status presentation
- [x] TRY/tarih/tabular numeral formatlama
- [x] Accounting ProblemDetails, Retry-After ve validation field mapping
- [x] CurrentAccount için double-submit intent koruması

CurrentAccount endpointleri idempotency header veya belge lifecycle'ı kullanmadığı için generic lifecycle/idempotency altyapısı sırf soyutlama amacıyla kurulmadı. Bu altyapı ilk gerçek tüketiciler olan satış, alış veya ödeme mutation diliminde eklenecektir.

Genel bakış yalnız belgeli verileri kullanacaktır:

- [x] Gecikmiş alacak/borç kuyrukları
- [x] Maliyetsiz veya kısmi maliyetli hareket kuyrukları
- [x] Gerçek hızlı işlem bağlantıları
- [x] Yalnız API `totalCount` değerinden türeyen kapsamı açık sayılar

Çıkış ölçütü:

- [x] Kullanıcı cari hesabı oluşturabilir, listeleyebilir, görüntüleyebilir ve güncelleyebilir.
- [x] Pasif cari listede ve tarihsel detayda görünür kalır; yeni belge lookup kuralı ilgili belge MVP'sinde uygulanacaktır.
- [x] Boş, yükleniyor, hata, 403, 404, 409 ve 429 durumları tanımlıdır.
- [x] Masaüstü ve mobil klavye/pointer kontrolleri geçer.

### MVP 2 — Alış faturaları ve giderler

Durum: `Sözleşme engelli` — güvenli frontend dilimi tamamlandı

Kapsam:

- [x] Alış faturası liste, oluşturma ve detay
- [ ] Mevcut Draft belgeyi kayıpsız düzenleme (`ADM-ACC-005`)
- [x] Yeni belge formunda satır ekleme, güncelleme ve silme
- [ ] Kayıtlı satırı kayıpsız güncelleme ve silme (`ADM-ACC-005`)
- [x] Mevcut pozitif Purchase StockMovement için güvenli tahsis
- [ ] Tam tahsisli mevcut hareketi tekrar düzenleme (`ADM-ACC-007`)
- [x] Tam tahsis olmadan post aksiyonunu engelleme/açıklama
- [x] Gidersiz belge post işleminden sonra supplier debt ve FIFO cost layer görünümü
- [x] Runtime ile uyumlu Posted iptal/reversal davranışı
- [x] Alış faturası gideri ekleme ve listeleme
- [x] KDV hariç tutar, miktar ve manuel gider dağıtımı
- [x] Gider kategorisi oluşturma ve listeleme
- [x] Genel gider oluşturma ve listeleme
- [x] API'nin authoritative final maliyetlerini gösterme
- [ ] Giderli alış faturasını maliyet kaybetmeden post etme (`ADM-ACC-006`)

Gösterilmeyecek aksiyonlar:

- [x] Genel gider update
- [x] Genel gider delete
- [x] Genel gider post/cancel
- [x] PurchaseInvoice kaynaklı yeni fiziksel stok hareketi

Çıkış ölçütü:

- [x] Yeni Draft belge input'u hata sonrası korunur.
- [ ] Kayıtlı Draft belge kayıpsız düzenlenebilir (`ADM-ACC-005`).
- [x] Eksik tahsisli posting başarısızlığı doğru açıklanır.
- [x] Başarılı post sonrası detay yeniden okunur.
- [x] Tüketilmiş maliyet katmanı nedeniyle gelen `409` kör retry edilmez.

MVP 2 API blokörleri:

- `ADM-ACC-005`: Detail DTO, düzenleme request'indeki indirim yapılandırmasını ve `isInvoiceDiscountEligible` değerini geri döndürmüyor; concurrency token da yok. GET→PUT veri kaybı yaratabileceği için edit route'u bilinçli olarak salt okunur blokör ekranıdır.
- `ADM-ACC-006`: Fatura gideri dağıtıldıktan sonra post hesaplaması allocated cost değerlerini sıfırlıyor ve FIFO katmanına gider payı taşınmıyor. Giderli belgede post UI tarafından engellenir.
- `ADM-ACC-007`: Available movement endpoint'i mevcut satırın kendi tam tahsisini bağlama dahilinde döndürmüyor; güvenli yeniden tahsis mümkün değil.
- `ADM-ACC-008`: İptal edilmiş fatura tahsisleri hareket kapasitesini tüketmeye devam ediyor.
- `ADM-ACC-009`: Gider DTO projection alanları eksik ve MVP 2 create endpoint'lerinde idempotency anahtarı bulunmuyor; UI double-submit'i önler fakat otomatik retry yapmaz.

### MVP 3 — Muhasebe satışları ve satış faturaları

Durum: `Tamamlandı` — doğrulanmış runtime kurallarıyla güvenli frontend dilimi

Kapsam:

- [x] AccountingSalesOrder liste, oluşturma, detay ve Draft düzenleme
- [x] Ürün varyantı satırları
- [x] Faturalı ve faturasız satış oluşturma
- [x] Post sonrası stok çıkışı, FIFO tüketimi ve müşteri alacağı görünümü
- [x] Doğrudan SalesInvoice oluşturma
- [x] Mevcut muhasebe satışından SalesInvoice oluşturma
- [x] SalesInvoice liste, detay ve Draft düzenleme
- [x] Ödenen/kalan, maliyet ve kâr alanlarını API'den gösterme
- [x] Kararlaştırılmış post/cancel lifecycle matrisi
- [x] Lifecycle mutation sonrası authoritative detay yenileme

Kesin domain sınırları:

- [x] E-ticaret Order/Cart/User tipi veya action'ı import edilmez.
- [x] SalesInvoice ikinci stok veya müşteri alacağı etkisi olarak gösterilmez.
- [x] Muhasebe satışları e-ticaret Siparişler ekranından route, metin ve navigasyon olarak ayrıdır.

Çıkış ölçütü:

- [x] Aynı create intent retry'ında aynı idempotency key korunur.
- [x] Double-click ikinci satış/fatura etkisi oluşturmaz.
- [x] Posted/Cancelled belgeler readonly olur.
- [x] Cancel/reversal geçmişi silinmeden gösterilir; API'nin ayrıca projekte etmediği reversal bağları açıkça belirtilir (`ADM-ACC-011`).

Bilinen API sözleşme açıkları:

- `ADM-ACC-010`: Dokümandaki bağımsız SalesInvoice iptali runtime tarafından normal akışta desteklenmiyor; UI iptali yalnız Posted AccountingSalesOrder üzerinden sunar.
- `ADM-ACC-011`: Tam-liste update concurrency tokenı ve cancellation reversal projeksiyonu yoktur; UI 409'u overwrite/retry etmez ve mutation sonrası tam detayı yeniden okur.
- `ADM-ACC-012`: Create başarı status/header, zorunlu idempotency header ve bazı generated response/body şemaları runtime ile drift halindedir; adapter gerçek runtime sözleşmesini uygular.

### MVP 4 — Ödeme, tahsilat, kasa ve banka

Durum: `Tamamlandı (2026-08-24)`

Kapsam:

- [x] Müşteri tahsilatı
- [x] Tedarikçi ödemesi
- [x] Tahsis edilmemiş tedarikçi avansı
- [x] Ödeme liste, detay ve cancel/reversal
- [x] Tahsilatların CurrentAccountTransaction kayıtlarına dağıtılması
- [x] Tam olarak bir cash veya bank hesabı seçimi
- [x] Kasa hesapları ve ekstre
- [x] Banka hesapları ve ekstre
- [x] Manuel finans hareketi
- [x] Finans hareketi reversal
- [x] Atomik banka transferi
- [x] API'den türetilmiş bakiye görünümü

Çıkış ölçütü:

- [x] CustomerCollection allocation olmadan gönderilemez.
- [x] SupplierPayment boş allocation ile avans olarak gönderilebilir.
- [x] Allocation açık tutarı veya ödeme toplamını aşamaz; allocation varsa toplam kuruş düzeyinde payment tutarına eşittir.
- [x] Allocation race `409` sonrası güncel bakiye/hareket yeniden okunur; otomatik retry yapılmaz.
- [x] Bakiye hiçbir zaman editable frontend alanı olmaz.
- [x] Reversal, orijinal hareketi silmez.

Kesin güvenlik kararları ve bilinen API açıkları:

- `ADM-ACC-013`: Payment idempotency anahtarı farklı payload'ı ayırt etmiyor ve API iki ondalık hassasiyeti zorlamıyor; UI intent değişiminde key'i yeniler, sonuç kimliğini doğrular ve kuruş hesabı kullanır.
- `ADM-ACC-014`: Cancel sonrası reversed allocation satırları detail DTO'dan kaybolur; UI iptal edilmiş `unallocatedAmount` değerini avans olarak sunmaz.
- `ADM-ACC-015`: Manuel finans formu yalnız runtime/doküman güvenli kesişimi `10,11,40,41,50` tiplerini açar; POS ve tek-bacaklı transfer kapalıdır.
- `ADM-ACC-016`: Transfer bacakları ve reversal satırları tekil reverse edilebildiği için UI bu aksiyonları gizler; yalnız özgün güvenli manuel hareket terslenebilir.
- `ADM-ACC-017`: Native finans hesabı detail/404 ve sayfalı ekstre yoktur; frontend önce tam hesap defterinden ID doğrular, sonra statement okur ve sahte pagination üretmez.

### MVP 5 — Maliyet, raporlar ve son kalite geçişi

Durum: `Tamamlandı (2026-08-24)`

Kapsam:

- [x] OpeningBalance maliyet katmanı GET/PATCH
- [x] Concurrency token taşıma ve stale token recovery
- [x] Varyant maliyet geçmişi
- [x] FIFO katman ve tüketim raporları
- [x] Stok değerleme
- [x] Satış, alış ve fatura raporları
- [x] Kârlılık raporları
- [x] Cari, alacak/borç ve vade raporları
- [x] Ödeme, kasa ve banka hareket raporları
- [x] Alış/satış KDV raporları
- [x] Rapor kataloğu ve rapora özel filtre/kolon haritası
- [x] Muhasebe genel bakışını gerçek operasyon verileriyle tamamlama
- [x] Modül geneli responsive, accessibility, E2E ve görsel geçiş

Çıkış ölçütü:

- [x] Universal finance table kullanılmaz.
- [x] Her açılan raporun alan anlamı ve kolonları açıkça doğrulanmıştır.
- [x] Sayfa satırları genel toplam olarak sunulmaz.
- [x] Stale cost-layer token `409` sonrası kullanıcıdan yeni onay alınır.
- [x] Geniş rapor tabloları mobilde erişilebilir kalır.

## 6. Tasarım sistemi

Mevcut Admin kabuğu korunacaktır:

- 256px koyu lacivert sidebar
- 56px kompakt topbar
- Açık gri sayfa zemini
- Beyaz ve bordered çalışma yüzeyleri
- Kontrollü mavi primary/active/focus vurgusu
- Geist font ailesi
- Mevcut semantic success/warning/danger rolleri

Muhasebe ürün listesi kalıbını kullanmayacaktır. Thumbnail, katalog organizasyonu, pazarlama badge'i veya kart galerisi yerine aşağıdaki arketipler kullanılacaktır.

### Muhasebe genel bakış

- Sahte para KPI veya trend yoktur.
- İşlem bekleyen belgeler ve doğrulanmış operasyon kuyrukları önceliklidir.
- Hızlı aksiyonlar yalnız gerçek route ve endpoint'e bağlanır.

### Cari hesap çalışma alanı

- Görselsiz, kod/unvan odaklı liste
- Cari türü, iletişim ve aktiflik bilgisi
- Detayda kimlik/iletişim ile ekstre/bakiye alanlarının ayrılması

### Belge sicili

- Belge numarası
- Cari hesap
- Belge ve vade tarihi
- Durum
- Genel toplam
- Ödenen ve kalan
- Son işlem/lifecycle zamanı

### Belge editörü

- Geniş ana kolon: başlık ve tekrarlı satırlar
- Dar rail: belge durumu, API toplamları ve lifecycle aksiyonları
- `Taslağı kaydet` ile `Muhasebeleştir` farklı önem seviyelerinde
- Post/cancel/reverse için sonucu açıkça anlatan dialog

### Defter ve ekstre

- Kaynak ve referans
- Borç/alacak veya giriş/çıkış yönü
- İşaretli ve mutlak tutar
- Bakiye etkisi
- Tarih ve reversal bağlantısı

### Rapor çalışma alanı

- Rapor kataloğu
- Seçilen rapora özgü filtreler
- Seçilen rapora özgü kolon haritası
- Server-side pagination
- Belgelenmemiş sort/export/toplam aksiyonu yoktur

### Görsel kurallar

- Para, miktar ve bakiye sağa hizalı `tabular-nums` kullanır.
- Gövde/control metni yaklaşık 14px, yardımcı metin 12–13px, sayfa başlığı 20–24px olur.
- Ledger satırı masaüstünde yaklaşık 52–60px olur.
- Ana yüzeylerde border ve bölüm ayırıcıları kullanılır.
- Shadow yalnız dialog, drawer, popover ve menu için kullanılır.
- Gradient, glow, glassmorphism veya ayrı bir muhasebe renk teması oluşturulmaz.
- Draft nötr/mavi, Posted success, Cancelled nötr gri; overdue warning, gerçek negatif risk danger kullanır.

## 7. Responsive kurallar

- 1440px ve üzeri: sidebar sabit, liste/rapor tam çalışma genişliğinde, belge editörü ana kolon + 20–22rem rail.
- 1024–1279px: düşük öncelikli meta birleştirilir; kimlik, durum, tutar ve aksiyon korunur.
- Mobil: sidebar erişilebilir drawer; belge rail'i karar sırasına göre ana içeriğin altına iner.
- Mobil tekrarlı belge satırları düzenlenebilir bloklara dönüşebilir.
- Gerçek ledger/rapor tablosu yatay kaydırılır; ilk kimlik kolonu sticky olabilir.
- Kaydırılabilir içeriğin devam ettiği scroll gölgesi veya benzeri erişilebilir bir ipucuyla belirtilir.
- Kolonlar sessizce kesilmez ve kritik aksiyonlar kaybolmaz.
- Mobil lifecycle aksiyonları sarılır veya içeriği kapatmayan action bar'a taşınır.

## 8. Etkileşim, pointer ve klavye kuralları

- Gerçek Link, Button, filter, sort, menu ve seçim kontrolleri `cursor: pointer` kullanır.
- Disabled lifecycle aksiyonları gerçek `disabled` veya `aria-disabled` ve `cursor: not-allowed` kullanır.
- Badge, salt tutar, tablo hücresi ve tıklanamayan satır `cursor: default` kullanır.
- Tüm satır tıklanacaksa semantik Link/focus yüzeyi oluşturulur; aksi halde yalnız kimlik bağlantısı tıklanabilir olur.
- Hiçbir aksiyon yalnız hover ile keşfedilmez.
- Mobil dokunma hedefleri yaklaşık 44px olur.
- Bütün kontrollerde görünür `focus-visible` korunur.
- Dialog/drawer focus'u içeride tutulur, güvenli olduğunda Escape ile kapanır ve focus tetikleyiciye döner.
- Uzun form validation summary ilk hatalı alana yönlendirir.
- Pending mutation `aria-busy` ile gösterilir ve tekrar aktivasyonu engeller.
- Success yalnız transient toast ile bırakılmaz; authoritative durum ekranda kalıcı görünür.
- `409` sonrasında güncel kayıt okunur; otomatik overwrite veya blind retry yapılmaz.

## 9. Idempotency ve concurrency

- Aynı kullanıcı intent'i, double-click, timeout veya unknown response retry'ında aynı idempotency key'i kullanır.
- Kullanıcı payload'ı değiştirdiğinde veya yeni işlem başlattığında yeni key oluşturulur.
- Non-idempotent veya idempotency sözleşmesi olmayan mutation otomatik retry edilmez.
- Post/cancel/reverse sonrasında authoritative detail yeniden okunur.
- OpeningBalance cost update en son concurrency token ile gönderilir.
- Stale token `409` durumunda form korunur, güncel veri alınır ve kullanıcıdan yeni onay istenir.
- Payment/financial transaction idempotency davranış farklılıkları feature testleriyle kilitlenir.

## 10. ProblemDetails davranışı

| Durum | UI davranışı |
| --- | --- |
| `400 validation_error` | Alan hatalarını ilgili kontrollere ve uzun form summary'ye bağla |
| `400 business_rule_violation` | Güvenli draft'ı koru, genel iş kuralı mesajını göster |
| `401` | En fazla bir refresh; başarısızsa login |
| `403` | Yetki mesajı; refresh deneme |
| `404` | Kaydı yenile veya güvenli liste dönüşü |
| `409 conflict` | Güncel detayı oku, kullanıcıdan yeni karar al |
| `409 concurrency_conflict` | Başka değişiklik mesajı ve karşılaştırmalı recovery |
| `429` | `Retry-After` rehberini koru |
| `500` | Güvenli genel hata ve varsa `traceId` |
| Timeout | Unknown outcome/idempotency durumuna uygun recovery |
| Non-JSON upstream | Upstream body/URL/secret sızdırmadan güvenli hata |

## 11. Subagent çalışma modeli

Her MVP'de en fazla üç paralel yardımcı hat kullanılacaktır:

| Rol | Görev |
| --- | --- |
| Ana ajan | Mimari sahiplik, shared accounting foundation, implementasyon, bütünleştirme ve son doğrulama |
| Contract subagent | OpenAPI–Markdown–controller–DTO–validator matrisi ve contract gap raporu |
| Quality subagent | Vitest, Playwright, accessibility, pointer, lifecycle ve hata senaryoları |
| Visual subagent | Yalnız gerekli ekranlarda desktop/mobile screenshot incelemesi ve görsel regresyon raporu |

Kurallar:

- Aynı dosyada paralel implementasyon yapılmaz.
- Subagent öncelikle bağımsız ve bounded denetim/test işi alır.
- Mimari karar ve birleşim ana ajanda kalır.
- Contract kararı verilmeden belirsiz aksiyon geliştirilmez.
- Her MVP sonunda subagent bulguları bu belgeye ve gerekiyorsa API iyileştirme kaydına işlenir.

## 12. Test stratejisi

### Vitest unit

- Enum ve status label
- Query parser ve pagination
- Form normalization ve payload builder
- Lifecycle action matrix
- Idempotency intent state machine
- ProblemDetails field mapping
- Rapor kolon haritaları

Backend para, KDV, FIFO, kâr veya bakiye hesaplamaları frontend unit testinde yeniden yazılmayacaktır.

### Vitest integration

- Server Action/API method, path, body ve header
- Admin auth sınırı
- `204` ve farklı başarılı `2xx` cevaplar
- Authoritative response/revalidation
- Aynı intent'te idempotency key korunması
- `409` sonrasında `refresh=true`, otomatik retry olmaması
- Güvenli ProblemDetails serialization

### Playwright PR seviyesi

- Deterministic mock API
- Desktop Chromium: 1440×1000
- Mobile Chromium: 390×844
- Role/label tabanlı selector
- Console, pageerror, request failure ve unexpected HTTP takibi
- Double-click, conflict, Retry-After, loading ve keyboard senaryoları
- Trace ve hata screenshot'ı

### Release smoke

- Yalnız izole gerçek API/DB ortamı
- Run-owned fixture/data
- Alış post
- Satış post
- Payment/reversal
- Shared development/production verisini silmeme
- Credential, cookie, token ve ödeme verisini artifact/log içine yazmama

### Accessibility

- WCAG 2.2 AA hedefi
- Temsilî settled state'lerde axe
- Tab, Shift+Tab, Enter, Space ve Escape
- Focus trap/restore
- Validation association ve summary
- 200% zoom ve 400% reflow
- Contrast ve reduced-motion
- Gerçek ekran okuyucu çalıştırılamazsa sonuç `Doğrulanmadı`

## 13. Her MVP için kalite kapısı

- [x] İlgili contract audit tamamlandı.
- [ ] Generated OpenAPI type drift kontrol edildi: kontrol çalıştırıldı; CurrentAccount dışındaki mevcut sipariş cancellation/polling drift'i nedeniyle repo-genel komut başarısız.
- [x] Typecheck geçti.
- [x] ESLint geçti.
- [x] İlgili Vitest testleri geçti.
- [x] Desktop Playwright geçti.
- [x] Mobile Playwright geçti.
- [x] Double-click/idempotency senaryosu geçti.
- [x] `400/401/403/404/409/429/500` kapsamı doğrulandı.
- [x] Loading, data-empty, error ve disabled durumları doğrulandı; filtered-empty backend filtre sözleşmesi olmadığı için uygulanamaz.
- [x] Keyboard ve focus akışı doğrulandı.
- [x] Pointer ve disabled cursor davranışı doğrulandı.
- [x] Uzun unvan, null alan ve yüksek TRY tutarı stress testi yapıldı.
- [x] Desktop ve mobile screenshot görsel olarak incelendi.
- [x] Production build geçti.
- [x] Kalan riskler bu belgeye ve `API-IMPROVEMENT-RECOMMENDATIONS.md` dosyasına yazıldı.

## 14. Genel ilerleme

| MVP | Durum | Başlangıç | Bitiş | Not |
| --- | --- | --- | --- | --- |
| MVP 1 — Temel ve cari hesaplar | Tamamlandı | 2026-08-24 | 2026-08-24 | Contract audit, iki revizyon turu, Vitest ve desktop/mobile Playwright tamamlandı |
| MVP 2 — Alış ve giderler | Sözleşme engelli | 2026-08-24 | - | Güvenli UI dilimi tamamlandı; `ADM-ACC-005`–`ADM-ACC-009` API blokörleri bekleniyor |
| MVP 3 — Satış ve faturalar | Tamamlandı | 2026-08-24 | 2026-08-24 | Runtime lifecycle matrisi, iki revizyon turu, Vitest ve desktop/mobile Playwright tamamlandı |
| MVP 4 — Ödeme ve hazine | Tamamlandı | 2026-08-24 | 2026-08-24 | Kuruş hassasiyetli allocation, ödeme lifecycle, kasa/banka ekstreleri ve atomik transfer doğrulandı |
| MVP 5 — Maliyet ve raporlar | Tamamlandı | 2026-08-24 | 2026-08-24 | 28 rapor, FIFO maliyet çalışma alanı, stale recovery ve desktop/mobile kalite geçişi tamamlandı |

Durum değerleri: `Bekliyor`, `Sözleşme engelli`, `Devam ediyor`, `Doğrulama`, `Tamamlandı`.

## 15. MVP 1 doğrulama kaydı

- Contract: OpenAPI, Markdown, controller, DTO, validator ve runtime mapping karşılaştırıldı.
- API açıkları: `ADM-ACC-001`–`ADM-ACC-004` kaydedildi.
- Vitest: Accounting adapter/action/form/query/presentation ve navigasyon senaryoları eklendi; tam admin paketi geçti.
- Playwright: İzole mock API ile desktop ve mobile toplam 18 senaryo geçti.
- Browser: 1440px ve 390px çalışma merkezi, liste, create form ve ekstre screenshot'ları incelendi.
- Build: Next.js production build geçti.
- Bilinen repo-genel engel: `api:types:check`, Accounting dışındaki mevcut generated OpenAPI drift'i nedeniyle başarısız.
- Ekran okuyucu: Otomatik accessibility tree ve klavye akışı doğrulandı; gerçek ekran okuyucu oturumu yapılmadı.

## 16. MVP 2 doğrulama kaydı

- Contract: OpenAPI, Markdown, controller, application service, DTO, validator ve domain entity davranışı karşılaştırıldı.
- API açıkları: `ADM-ACC-005`–`ADM-ACC-009` kaydedildi; veri kaybı veya yanlış maliyet üretecek UI yolları açılmadı.
- Uygulama: Alış faturası sicili, create/detail, stok hareketi tahsisi, post/cancel, borç/FIFO görünümü, fatura gideri, gider kategorisi ve genel gider çalışma alanı eklendi.
- Vitest: Tam Admin paketi 60 dosyada 235 test ile geçti; alış adapter/form/query testleri bu pakete dahildir.
- Playwright: İzole mock API ile masaüstü ve mobil toplam 26 muhasebe senaryosu geçti.
- Browser: Alış faturası post akışı ile gider çalışma alanının 1440px ve 390px settled state screenshot'ları incelendi.
- Build: Next.js production build geçti; tüm yeni accounting route'ları dynamic server route olarak üretildi.
- Güvenlik: DTO round-trip blokörü nedeniyle kayıtlı Draft edit yolu salt okunur; giderli belge post işlemi maliyet blokörü çözülene kadar disabled durumdadır.
- Ekran okuyucu: Otomatik accessibility tree, klavye/focus ve pointer davranışı doğrulandı; gerçek ekran okuyucu oturumu yapılmadı.

## 17. MVP 3 doğrulama kaydı

- Contract: OpenAPI, endpoint Markdown, controller, application handler, DTO, validator, domain entity ve persistence davranışı karşılaştırıldı.
- API açıkları: `ADM-ACC-010`–`ADM-ACC-012` kaydedildi; bağımsız fatura iptali veya otomatik conflict retry gibi güvenli olmayan UI yolları açılmadı.
- Uygulama: Muhasebe satışı ile satış faturası sicilleri; create/detail/Draft edit; satışla birlikte, doğrudan ve mevcut satıştan fatura; post; yalnız satış üzerinden cancel; stok/FIFO/alacak/maliyet/kâr denetim izi eklendi.
- Idempotency: Satış ve doğrudan fatura create intent'i zorunlu, form ömrü boyunca stabil key taşır; double-submit istemci tarafında da engellenir.
- Vitest: Tam Admin paketi 63 dosyada 242 test ile geçti; sales adapter/form/query ve navigasyon senaryoları pakete dahildir.
- Playwright: İzole mock API ile satış senaryoları masaüstü ve mobilde create, direct invoice, post, from-order, cancel ve readonly sınırlarını kapsar.
- Browser: 1440px ve 390px sicil ile iptal edilmiş detay screenshot'ları incelendi; yatay belge tabloları odaklanabilir ve mobilde açık kaydırma ipucu taşır.
- Build: Next.js production build geçti; sekiz yeni sales order/invoice route'u dynamic server route olarak üretildi.
- Ekran okuyucu: Otomatik accessibility tree, klavye/focus ve pointer/disabled davranışı doğrulandı; gerçek ekran okuyucu oturumu yapılmadı.

## 18. MVP 4 doğrulama kaydı

- Contract: OpenAPI, payment/financial endpoint Markdown dokümanları, controller, application service, DTO, validator ve runtime davranışı karşılaştırıldı.
- API açıkları: `ADM-ACC-013`–`ADM-ACC-017` kaydedildi; güvenli olmayan tek-bacaklı transfer, transfer bacağı reversal ve yanıltıcı iptal sonrası avans yolları açılmadı.
- Uygulama: Ödeme/tahsilat sicili, açık kalem dağıtımı, tedarikçi avansı, makbuz detayı ve cancel; kasa/banka defteri, hesap oluşturma, ekstre, güvenli manuel hareket, reversal ve atomik banka transferi eklendi.
- Idempotency ve tutar güvenliği: Create intent değişiminde anahtar yenilenir, double-submit istemci tarafında engellenir, dönen ödeme/transfer intent ile doğrulanır ve toplamlar kuruş düzeyinde karşılaştırılır.
- Vitest: Tam Admin paketi 69 dosyada 259 test ile geçti; payment/treasury adapter, form, query ve presentation senaryoları pakete dahildir.
- Playwright: İzole mock API ile masaüstü ve gerçek 390 px mobil görünümde toplam 42 muhasebe senaryosu geçti; ödeme create/cancel, manuel hareket ve atomik transfer MVP 4 kapsamındadır.
- Browser: Ödeme makbuzu, kasa ekstresi ve banka transfer ekstresi 1440 px ile 390 px settled-state screenshot'larında incelendi; geniş tablolar odaklanabilir, yatay kaydırma ipucu taşır ve sayfa gövdesini genişletmez.
- Build: Next.js production build geçti; payment ve treasury route'ları dynamic server route olarak üretildi.
- Etkileşim: Ana aksiyonlar minimum 40–44 px hedefe, pointer ve disabled cursor davranışına sahiptir; dialog/focus ve otomatik accessibility tree akışları doğrulandı. Gerçek ekran okuyucu oturumu yapılmadı.

## 19. MVP 5 doğrulama kaydı

- Contract: 28 rapor endpoint'i ile açılış katmanı GET/PATCH, varyant maliyet geçmişi; OpenAPI, controller, application reader/validator ve DTO projection düzeyinde karşılaştırıldı.
- API açıkları: `ADM-ACC-018`–`ADM-ACC-019` kaydedildi; UI anlamsız ortak filtreleri açmadı, `Guid.Empty` rapor kimliğine güvenmedi ve sayfa satırlarını finansal toplam olarak sunmadı.
- Uygulama: Varyant arama/seçim, OpeningBalance birim maliyet düzeltmesi, karşılaştırmalı stale recovery ve maliyet geçmişi; beş finans grubunda 28 rapor, rapora özel filtre/kolon kataloğu ve server-side pagination eklendi.
- Concurrency: İlk stale `409` sonrası güncel katman yeniden okundu, taslak korundu ve yeni token ile ikinci PATCH yalnız açık kullanıcı onayından sonra gönderildi; otomatik veya blind retry yapılmadı.
- Generated contract: Admin `src/generated/api.ts` güncel OpenAPI'den yeniden üretildi ve `api:types:check` geçti.
- Vitest: Tam Admin paketi 73 dosyada 267 test ile geçti; maliyet form/query ve rapor katalog/query testleri pakete dahildir.
- Playwright: İzole mock API ile masaüstü ve gerçek 390 px mobil görünümde toplam 50/50 muhasebe senaryosu geçti; yeni sekiz senaryo rapor kataloğu, rapora özel kolonlar, sahte toplam yokluğu, açılış maliyeti ve stale confirmation akışını kapsar.
- Browser: Rapor kataloğu, satış raporu ve FIFO maliyet ekranı 1440 px ile 390 px settled-state görsellerinde incelendi; normal mobil viewport ayrıca accessibility tree ve box ölçüleriyle doğrulandı.
- Build: Next.js production build geçti; `/accounting/costing`, `/accounting/reports` ve `/accounting/reports/[report]` dynamic server route olarak üretildi.
- Etkileşim: Link/button pointer davranışı, 40–44 px dokunma hedefleri, odaklanabilir yatay rapor bölgeleri, kalıcı success/conflict feedback ve disabled pending davranışı doğrulandı. Gerçek ekran okuyucu oturumu yapılmadı.

MVP 1, MVP 3, MVP 4 ve MVP 5 tamamlanmıştır. MVP 2'nin `ADM-ACC-005`–`ADM-ACC-009` blokörleri ve raporlama için `ADM-ACC-018`–`ADM-ACC-019` iyileştirmeleri açık kalır.
