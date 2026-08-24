# ELEVEN — E-Ticaret, Analitik ve Ön Muhasebe Platformu

ELEVEN; e-ticaret operasyonlarında karşılaşılan gerçek ihtiyaçlar, stok maliyet yönetimi ve veri analitiği gereksinimleri doğrultusunda geliştirdiğim uçtan uca bir e-ticaret platformudur.

Projenin temel amacı; yalnızca standart ürün ve sipariş CRUD işlemlerini yöneten klasik bir web sitesi oluşturmak değil; **kullanıcı etkileşimlerini veritabanı seviyesinde analiz edebilen, FIFO maliyet katmanlarıyla stok maliyetini izleyen ve hedefli cache geçersiz kılma kullanan** kurumsal bir altyapı inşa etmektir.

---

## Yapılandırılmış Production Adresleri

Deployment yapılandırması aşağıdaki adresleri kullanır; erişilebilirlik dağıtım ortamına bağlıdır:

- **Mağaza (Storefront):** [https://www.serhateleven.com.tr](https://www.serhateleven.com.tr)
- **Yönetim Paneli (Admin):** [https://admin.serhateleven.com.tr](https://admin.serhateleven.com.tr)
- **Backend API:** [https://api.serhateleven.com.tr](https://api.serhateleven.com.tr)

---

## Projenin Teknik Yaklaşımı

ELEVEN, e-ticaret işlemleriyle analitik ve ön muhasebe yeteneklerini aynı uygulama sınırları içinde ele alır:

| Alan | Uygulanan yaklaşım |
| :--- | :--- |
| **Analitik ve metrik modeli** | Ürün/varyant üzerindeki ömür boyu sayaçlar ile `ProductDailyMetric` günlük agregaları birlikte kullanılır. |
| **Ön muhasebe ve FIFO** | Satış, alış, cari hesap, kasa/banka ve FIFO maliyet katmanı akışları ayrı muhasebe modüllerinde yönetilir. |
| **Stok yönetimi** | İmzalı stok hareketleri sipariş, iade, sayım veya alış kaynaklarıyla ilişkilendirilebilir ve denetlenebilir. |
| **Cache güncelliği** | API mutasyonları, güvenli server-to-server on-demand revalidation ile ilgili Next.js tag/path hedeflerini geçersiz kılar. |
| **Görsel dağıtımı ve optimizasyon** | Yüklenen ham ve yüksek boyutlu PNG/JPEG görseller, Sharp motoru tarafından ziyaretçinin ekranına göre otomatik olarak AVIF/WebP formatlarına dönüştürülür, %90'a varan oranda sıkıştırılır ve 30 gün önbellekte saklanır. |
| **Public kimlikler** | Dahili `bigint` kullanıcı ve ürün kimlikleri, dış sözleşmede `P00001` ve `U00001` biçimindeki Base36 kodlamayla sunulur. |
| **Ödeme ve taksit** | Kart verisini uygulamaya almayan iyzico CheckoutForm akışı ile ürün fiyatına bağlı bilgilendirici taksit tablosu kullanılır. |

---

## Mimari ve Teknoloji Yığını

Proje, **Clean Architecture** prensiplerine uygun olarak ayrıştırılmış bir .NET Backend ve iki bağımsız Next.js uygulamasından (Admin & Storefront) oluşan monorepo yapısına sahiptir:

### Backend (.NET Core)
- **.NET 10 ve ASP.NET Core Web API:** İnce controller (Thin Controller) yapısı, merkezi middleware ve filter zincirleri.
- **Clean Architecture Katmanları:**
  - `ECommerce.Domain`: Saf domain entity'leri, aggregate root'lar, değişmezler (invariants), enum'lar ve domain exception'ları.
  - `ECommerce.Application`: CQRS mimarisi (MediatR), FluentValidation iş kuralları ve DTO sözleşmeleri.
  - `ECommerce.Persistence`: Entity Framework Core, SQL Server 2022, Fluent API konfigürasyonları ve migration yönetimi.
  - `ECommerce.Infrastructure`: Redis, JWT servisleri, PBKDF2 parola hashleme, e-posta ve İyzico entegrasyonları.
  - `ECommerce.API`: Endpoint yönlendirmeleri, çıktı önbellekleme (Output Caching) ve Swagger/OpenAPI dokümantasyonu.
- **Test:** xUnit, FluentAssertions, Moq ve WebApplicationFactory tabanlı kapsamlı entegrasyon testleri.

### Frontend (Next.js & React)
- **Next.js 16 (App Router) & React 19:** Server Components ve Server Actions odaklı, standalone çıktı üreten yapı.
- **Storefront (`UI/storefront`):** Yüksek performanslı müşteri deneyimi, dinamik filtreleme, varyant bazlı anlık fiyat/taksit senkronizasyonu, SEO uyumu (JSON-LD, Open Graph, Canonical) ve Core Web Vitals optimizasyonları.
- **Admin Panel (`UI/admin`):** Sipariş yönetimi, toplu ürün ve varyant düzenleme matrisi, doğrudan tarayıcıdan Cloudinary asenkron görsel yükleme, kupon/kampanya yönetimi, banner yönetimi ve ön muhasebe arayüzü.
- **Styling & Test:** Tailwind CSS v4 (CSS-first token mimarisi), Vitest ve Playwright E2E testleri.

---

## Temel Modüller

### 1. Katalog ve Varyant Matrisi
- Çok boyutlu varyant yapısı (beden, renk, materyal vb.), her varyanta özel SKU, barkod, stok ve net/brüt fiyat takibi.
- Ürün detay sayfasında (PDP) seçilen varyantın fiyatına göre başlık altındaki fiyatın ve İyzico taksit seçenekleri tablosunun anlık olarak güncellenmesi.
- Toplu varyant oluşturma, fiyat ve stok düzenleme matrisi.

### 2. Çift Seviyeli Analitik Motoru
- **Anlık Metrikler:** Ürün ve varyant bazında tıklanma, sepete ekleme, satın alma, favorileme, puanlama ve yorum sayaçları.
- **Günlük Agregalar:** `ProductDailyMetric` ve `ProductVariantDailyMetric` tabloları sayesinde her tıklama için sınırsız event satırı biriktirmeden tarih bazlı performans ve dönüşüm hunisi (Conversion Rate) takibi.

### 3. Dahili Ön Muhasebe ve Stok Maliyet Yönetimi
- Tamamlanan siparişlerin otomatik olarak `AccountingSalesOrder` kaydına dönüştürülmesi.
- Alış faturaları, KDV ayrıştırması ve navlun/ek maliyetlerin dağıtılması.
- **FIFO Maliyet Katmanları:** Satılan ürünlerin maliyetini (SMM) partilere göre gerçek giriş fiyatından hesaplama ve anlık stok değerlemesi.
- Cari hesap ekstreleri, tahsilat/ödeme kayıtları ve kasa/banka hareketleri.

### 4. Sepet, Sipariş ve İyzico Ödeme Akışı
- Misafir (Guest) ve üye sepetlerinin yönetimi, üye girişi yapıldığında sepetlerin otomatik birleştirilmesi.
- İyzico CheckoutForm ödeme oturumu başlatma, imzalı callback/retrieve doğrulaması ve sipariş durum geçişleri.
- Yüzdesel veya sabit tutarlı, sepet alt limitli ve kullanım kotalı kupon motoru.
- Müşteri panelinden iade/değişim talebi başlatma, admin teslim alma ve karar süreci ile stok etkilerinin yönetilmesi.

### 5. Dinamik Mağaza ve Yasal Sayfalar
- Ana sayfa masaüstü/mobil hero bannerları, duyuru bandı, lookbook ve koleksiyon vitrinlerinin panelden yönetimi.
- Mağaza kimlik ve iletişim bilgilerinin (logo, çalışma saatleri, destek e-postası/telefonu, adres, harita) tek merkezden yönetilmesi.
- KVKK ve Gizlilik Politikası, Mesafeli Satış Sözleşmesi ve Üyelik Sözleşmesi gibi yasal metinlerin veritabanındaki mağaza ayarlarından dinamik olarak beslenmesi.

---

## Eklenen İleri Düzey Yetenekler

- **Hedefli On-Demand Revalidation:** Panelden yapılan ilgili değişikliklerden sonra .NET API, Next.js `/api/revalidate` endpoint'ini güçlü bir ortak anahtarla ve allowlist kapsamındaki tag/path değerleriyle tetikler.
- **Otomatik AVIF ve WebP Görsel Pipeline:** Panele yüklenen ham ve yüksek boyutlu PNG/JPEG görseller, Sharp motoru tarafından istemcinin tarayıcısına ve ekran çözünürlüğüne (`deviceSizes`) göre dinamik olarak AVIF/WebP formatına dönüştürülür, %90'a varan oranda sıkıştırılır ve mobilde `<picture>` art direction ile çift indirme engellenir.
- **Cloudflare Turnstile ve Misafir Güvenliği:** İletişim formu ve misafir sipariş takibi gibi açık uçlarda bot koruması ve Redis tabanlı IP rate-limiting.
- **Mikro Etkileşimler ve UI Detayları:** Yatay kaydırılabilir kategori barları, yumuşak geçişli karuseller, sepet bildirimleri ve erişilebilir modal pencereleri.

---

## Geliştirilmeye Devam Eden Özellikler (Yol Haritası)

1. **Pazaryeri Entegrasyonları (Trendyol, Hepsiburada, Amazon):**
   - Ürün kataloğu, stok ve fiyatların pazaryerlerine otomatik senkronizasyonu; siparişlerin tek havuzda toplanması.
2. **Gelişmiş Analitik Raporlama ve Isı Haritaları:**
   - Günlük metrik tablolarından saatlik/haftalık satış eğilimleri, kategori performans karşılaştırmaları ve kohort analiz grafikleri.
3. **E-Fatura / E-Arşiv Entegrasyonu:**
   - Ön muhasebe faturalarının GİB onaylı özel entegratörler (Paraşüt, KolayBi, Uyumsoft vb.) üzerinden doğrudan e-fatura/e-arşiv olarak kesilmesi.
4. **Kargo Firması Entegrasyonları (Yurtiçi, Aras, MNG, Kolay Gelsin):**
   - Sipariş onaylandığında otomatik kargo barkodu ve takip numarası üretilmesi, kargo durumunun webhook ile güncellenmesi.
5. **Yapay Zekâ Destekli Öneri Motoru:**
   - Müşterinin sepet, favori ve gezinme geçmişine dayalı dinamik ürün öneri algoritmaları.

---

## Güvenlik Standartları

- **Parola Güvenliği:** PBKDF2-SHA256, 210.000 iterasyon ve kriptografik rastgele salt.
- **Token Güvenliği:** Refresh token'ların ham halleri veritabanında tutulmaz; SHA-256 hash'leri saklanır.
- **BFF (Backend-for-Frontend):** JWT token'ları tarayıcı JavaScript ortamına sızdırılmaz; `HttpOnly`, `SameSite=Lax` cookie'ler üzerinden yönetilir.
- **Brute-Force Koruması:** Hatalı girişlerde `AccessFailedCount` sayacı ve geçici hesap kilitleme (`LockoutEndAt`).
- **Eşzamanlılık (Concurrency):** Kritik stok hareketleri ve mağaza ayarlarında `concurrencyToken` ile çift işlem engeli.

---

## Proje Dizin Yapısı

```text
/opt/eleven
├── API/                               # ASP.NET Core Web API Çözümü
│   ├── src/
│   │   ├── ECommerce.Domain/          # Entity'ler, Değişmezler, Enum'lar
│   │   ├── ECommerce.Application/     # CQRS Handlers, DTO'lar, Validation kuralları
│   │   ├── ECommerce.Persistence/     # EF Core DbContext, Migrations, Mapping
│   │   ├── ECommerce.Infrastructure/  # Redis, JWT, Email, İyzico Servisleri
│   │   └── ECommerce.API/             # Controllers, Filters, Middleware
│   └── tests/
│       ├── ECommerce.UnitTests/       # Domain ve Application birim testleri
│       └── ECommerce.IntegrationTests/# WebApplicationFactory uçtan uca API testleri
│
├── UI/                                # Frontend Workspace (pnpm monorepo)
│   ├── storefront/                    # Müşteri Mağazası (Next.js 16 - Port 3000)
│   ├── admin/                         # Yönetim Paneli (Next.js 16 - Port 3001)
│   └── docs/                          # API sözleşmeleri, mimari rehberler ve dökümantasyon
│
├── docker-compose.yml                 # SQL Server, Redis, API ve UI konteyner orkestrasyonu
└── README.md                          # Proje Ana Dökümanı
```

---

## Yerel Kurulum ve Çalıştırma

Tüm sistemi (SQL Server, Redis, API, Storefront ve Admin Panel) Docker üzerinden tek komutla ayağa kaldırabilirsiniz:

### 1. Depoyu Klonlayın ve Ortam Dosyalarını Hazırlayın
```bash
git clone <repo-url>
cd eleven
cp .env.example .env
cp API/.env.example API/.env
cp UI/admin/.env.example UI/admin/.env
cp UI/storefront/.env.example UI/storefront/.env
```

`.env` dosyalarındaki boş değerleri yerel ortamınıza göre doldurun. Root `.env` içindeki `STOREFRONT_REVALIDATE_SECRET` en az 32 baytlık kriptografik rastgele bir değer olmalıdır.

### 2. Konteynerleri Başlatın
```bash
docker compose up -d --build
```

### 3. Yerel Servis Portları
- **Mağaza (Storefront):** `http://localhost:3000`
- **Yönetim Paneli (Admin):** `http://localhost:3001`
- **Backend API (Swagger):** `http://localhost:3300/swagger`
- **SQL Server:** `localhost:1433` (Veritabanı: `ECommerceDb`)
- **Redis:** `localhost:6379`

### 4. Testleri Çalıştırma
```bash
# Backend Testleri
cd API && dotnet test

# Storefront Birim Testleri
cd UI/storefront && pnpm test

# Admin Panel Birim Testleri
cd UI/admin && pnpm test
```

Playwright senaryoları, ilgili `playwright.*.config.ts` dosyası açıkça seçilerek ayrı çalıştırılır.
