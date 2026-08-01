# SERANTIS Frontend Çalışma Sözleşmesi

Bu dosya, `UI/` workspace'i altındaki bütün gelecek Codex çalışmalarında bağlayıcıdır. Buradaki kurallar frontend uygulamalarına aittir; API kodunu, API belgelerini veya Accounting uygulamasını değiştirme yetkisi vermez.

## 1. Proje genel bakışı

SERANTIS; mağaza, ürün ve stok operasyonları, siparişler, müşteriler, kampanyalar, muhasebe, gelecekteki pazaryeri bağlantıları, raporlama ve analitiği tek profesyonel sistemde birleştirmeyi amaçlayan bir e-ticaret platformudur.

Mevcut gerçek depo yapısı:

- Frontend workspace kökü: `UI/`
- Admin uygulaması: `admin/`
- Storefront uygulaması: `storefront/`
- API kökü: `../API/`
- API çalışma sözleşmesi: `../API/AGENTS.md`
- Frontend için API belgeleri: `docs/api/`
- Ayrıntılı Accounting frontend belgeleri: `docs/api/api-accounting-docs/`
- OpenAPI belgesi: `docs/api/api-project-docs/openapi-controller-contract.json`

Frontend bugün Next.js `16.2.12`, React `19.2.4`, TypeScript strict mode, App Router, pnpm, Tailwind CSS v4, ESLint ve Vitest kullanır. Tailwind v4 CSS-first yapıdadır; ayrı bir `tailwind.config.*` dosyası yoktur. Yeni bir Tailwind config dosyası sırf alışkanlık nedeniyle oluşturulamaz.

Mevcut `admin/src/app/page.tsx` create-next-app başlangıcı ve `admin/src/app/product/[slug]` SEO denemesi, tamamlanmış Admin Panel arayüzü değildir. Phase 1 yalnız `admin/` uygulamasını kapsar. `storefront/` ayrı bir uygulama olarak kurulmuştur ancak public Storefront uygulaması bu Phase 1 kapsamının dışındadır; admin layout'u, auth state'i, operasyon componentleri ve navigation'ı Storefront ile paylaşılmaz.

## 2. Ürün kimliği ve marka

**SERANTIS geçici çalışma adıdır; kesin ticari marka veya domain adı değildir.** Uygulama adı merkezi bir config/environment değerinden okunur. Varsayılan geliştirme değeri `SERANTIS` olabilir; feature componentleri, metadata, hostname, renkler veya asset'ler bu adı kalıcı marka varsayımıyla hard-code etmez.

- Kalıcı logo henüz yoktur. Onay verilene kadar merkezi uygulama adıyla üretilen sade metin tabanlı `SERANTIS` wordmark kullanılır.
- Logo, monogram, maskot veya kalıcı marka işareti üretilmez.
- Mevcut kalıcı marka paleti yoktur. Kullanıcı, nötr yüzeyler ve ölçülü mavi aksandan oluşan küçük provisional token temelini onaylamıştır; bu temel kalıcı marka paleti gibi sunulmaz ve kolay değiştirilebilir tutulur.
- `serantis.com`, `www.serantis.com`, `admin.serantis.com`, `api.serantis.com` veya başka bir production hostname varsayılmaz.
- Yönetim arayüzü ciddi, güvenilir ve operasyon odaklı görünmelidir. Bir landing page veya jenerik AI dashboard görünümüne dönüşmemelidir.
- Sahte istatistik, müşteri sayısı, satış toplamı, büyüme oranı, sosyal kanıt, slogan veya demo iş verisi gerçekmiş gibi gösterilmez.

## 3. Mevcut frontend aşaması

Mevcut Next.js workspace'i bağımsız `admin/` ve `storefront/` uygulamalarını içerir. Phase 1'de yalnız Admin Panel geliştirilir; public Storefront uygulaması sonraki ayrı kapsamda geliştirilecektir. İki uygulama bağımsız deploy edilebilir.

Durum terimleri kesin olarak şu anlamlara gelir:

- **Phase 1:** Kullanıcı onaylı ilk uygulama dilimi; şimdi geliştirilecek alan.
- **Planned next:** Backend desteği olabilir fakat Phase 1'de sayfası tamamlanmayacak alan.
- **Future module:** Backend/frontend sözleşmesi henüz olmayan uzun vadeli alan.
- **Placeholder/disabled:** Bilgi mimarisinde yeri ayrılmış fakat tıklanabilir route'u ve gerçek verisi olmayan öğe.

Depo gerçekliği: Şu anda admin shell, BFF auth, admin route'ları veya admin tasarım sistemi uygulanmış değildir. Dolayısıyla bu dosyadaki “Phase 1” ifadesi mevcut tamamlanmış sayfa değil, sıradaki onaylı uygulama kapsamıdır.

Phase 1 yalnız şunları kapsar:

1. Gerekli login ve admin route koruması.
2. Admin application shell.
3. Responsive sidebar ve topbar/header.
4. Dashboard giriş sayfası.
5. E-ticaret Orders liste ve detay temeli.
6. Products listesi.
7. Add Product akışı.

Accounting, marketplace integrations, customers, campaigns/coupons, reports, inventory/stock operations, administrators ve settings Phase 1'de otomatik olarak tamamlanmış sayfa sayılmaz.

## 4. Doğruluk kaynakları

Bir özellik üzerinde çalışmadan önce yalnız genel dokümanı değil, ilgili endpoint dosyasını ve gerçek DTO'yu da bulun.

Kaynak önceliği:

1. Kullanıcının güncel ve açık kapsam kararı ile bu `AGENTS.md`.
2. `docs/api/api-project-docs/openapi-controller-contract.json`: belgelenmiş wire schema, nullable/required alan ve numeric enum sözleşmesi.
3. `docs/api/api-project-docs/08-endpoint-sozlesmeleri/` ve konuya ait Markdown belgeleri: onaylı route, workflow ve frontend davranışı.
4. `docs/api/api-accounting-docs/`: Accounting frontend'i için güncel, ayrıntılı iş ve UI sözleşmesi.
5. Gerektiğinde `../API/src/`: controller attribute'ları, DTO/validator ve gerçek runtime davranışını doğrulamak için kullanılır.
6. `../API/docs/accounting-module-spec.md`: Accounting'in tarihsel tasarım kaynağıdır; güncel controller ve `api-accounting-docs` ile çeliştiğinde mevcut uygulanmış sözleşme önceliklidir.

API dokümantasyonu frontend sözleşmesinin kaynağıdır. Kaynak kodda bulunan fakat OpenAPI ve endpoint belgelerinde bulunmayan bir endpoint frontend tarafından “hazır sözleşme” kabul edilemez. Böyle bir durumda dur, farkı dosya/route düzeyinde raporla ve doküman güncellenmeden entegrasyon kurma.

Şunlar kesinlikle tahmin edilmez:

- Endpoint, query parametresi, filtre veya sort alanı.
- Request/response alanı, nullable davranış veya numeric enum değeri.
- Auth, refresh, rol veya yetki davranışı.
- Idempotency, concurrency veya retry davranışı.
- Stok, fiyat, vergi, indirim, kampanya, fatura veya muhasebe hesabı.
- Desteklenmeyen lifecycle geçişi.

Eksik bilgi kullanıcı deneyimini veya veri doğruluğunu etkiliyorsa çalışmayı o sınırda durdur ve “missing contract” olarak raporla.

## 5. Zorunlu skill'ler

İlgili işte aşağıdaki skill'in `SKILL.md` ve yönlendirdiği gerekli referanslar okunmalıdır. Sadece skill adını anmak yeterli değildir.

| Skill | SERANTIS'te uygulanacak zorunlu davranış |
| --- | --- |
| `nextjs-ecommerce-architecture` | App Router, İngilizce route'lar, ince route dosyaları, Server Component varsayılanı, `src/modules` sahipliği, server-only API sınırı, açık cache kararı ve admin/storefront ayrımı. |
| `api-integration-auth` | OpenAPI/source drift denetimi, merkezi typed client, BFF, ayrı HttpOnly access/refresh cookie, tek refresh denemesi, ProblemDetails normalizasyonu, CSRF ve token sızıntısı kontrolleri. |
| `admin-dashboard-design` | Kompakt shell, gerçek API verisi, URL tabanlı filtreler, okunabilir tablolar, progressive form yapısı, tutarlı drawer/dialog ve gerçek lifecycle aksiyonları. |
| `visual-design-review` | Önce kanıtlı sorun raporu, sonra düzeltme; desktop/mobile ve gerçek veri durumlarıyla ekran görüntüsü karşılaştırması; jenerik AI tasarım belirtilerinin kaldırılması. |
| `performance-core-web-vitals` | Production build üzerinden ölçüm, dar Client Component sınırı, overfetch/waterfall analizi, doğru görsel/font/script kullanımı ve field/lab metriği ayrımı. |
| `testing-accessibility` | Phase 1 kritik akışları, mobil kullanım, keyboard/focus/form/dialog denetimi, console/network hata kaydı ve WCAG 2.2 AA hedefi. |
| `ecommerce-seo-review` | Admin için auth + noindex; gelecekteki storefront için route indexability matrisi, metadata, canonical, OG, sitemap, robots, structured data ve CWV doğrulaması. |

Runtime incelemesinde mevcutsa:

- Next DevTools MCP, Next.js route/runtime ve sürüme uygun framework davranışı için kullanılır.
- Chrome DevTools MCP, network, cookie attribute, console, accessibility, Lighthouse/performance trace ve ekran görüntüsü için kullanılır.
- Playwright MCP, login, protected navigation, ürün/sipariş akışları ve responsive durumların tekrarlanabilir keşfi için kullanılır.

MCP çıktısı kaynak incelemesi, kalıcı testler, lint, type-check ve production build yerine geçmez. Araç yoksa eşdeğer yerel kontrol yapılır ve doğrulanamayan kısım belirtilir. Token, cookie, parola, Authorization header veya kişisel veri MCP çıktısına ya da rapora yazılmaz.

## 6. Depo ve mimari kuralları

Mevcut App Router ve `src/` düzeni korunur. Hedef yapı aşağıdaki yöndedir; yalnız aktif dilim için gereken klasörler oluşturulur:

```text
src/
  app/
    (auth)/
      login/
    (admin)/
      layout.tsx
      dashboard/
        page.tsx
      orders/
        page.tsx
      products/
        page.tsx
        new/
          page.tsx
    api/auth/              # yalnız gerçek BFF HTTP sınırları
    page.tsx               # session durumuna göre /login veya /dashboard redirect
    layout.tsx
    robots.ts
    sitemap.ts
  modules/
    admin-shell/
    auth/
    orders/
    products/
    accounting/            # yalnız açıkça istendiğinde
  components/
    ui/
  lib/
    api/
    auth/
    formatting/
    validation/
  generated/
    api.ts                 # onaylı OpenAPI generation sonrası
```

Kurallar:

- Static URL segmentleri, route group adları ve dynamic parametre adları İngilizce, lowercase ve `kebab-case` olur. UI metni Türkçe olabilir.
- Bu ayrı Admin Panel uygulamasında `/admin` URL öneki kullanılmaz. Phase 1 URL'leri `/login`, `/dashboard`, `/orders`, `/products` ve `/products/new` olur. Hostname daha sonra admin subdomain olsa bile route yapısı değişmez.
- Route group URL'yi değiştirmek için kullanılmaz; layout, auth ve rendering sınırı sağlar.
- `page.tsx`, `layout.tsx`, `loading.tsx`, `error.tsx` ve `not-found.tsx` yalnız routing, params/searchParams çözümleme, ilk fetch ve feature composition yapar.
- İş kuralları ve endpoint çağrıları sunum componentlerinde tutulmaz.
- Business UI, action, form schema, mapper, status label ve feature API operasyonu sahip `src/modules/<feature>` altında kalır.
- `src/components/ui` yalnız domain bilmeyen, gerçekten ortak primitive'ler içindir. `src/modules/admin-shell` sidebar, topbar ve page frame'i sahiplenir.
- Bir modül başka modülün private component/action/API dosyasını import etmez. Gerçekten iki tüketici oluşmadan kod shared alana taşınmaz.
- Büyük `utils.ts`, `helpers.ts`, belirsiz `services/`, gereksiz barrel export veya farklı iş sorumluluklarını birleştiren “generic” component oluşturulmaz.
- Shared pagination, ProblemDetails ve generated wire type tekrar yazılmaz.
- Yeni dependency ancak somut ihtiyaç, bundle/bakım etkisi ve açık kullanıcı onayıyla eklenir.
- Mevcut Tailwind v4 `@theme`/CSS variable yaklaşımı kullanılır; ikinci styling sistemi eklenmez.

### Host-agnostic yapılandırma

- Final commercial ad, domain, hosting provider, API hostname ve deployment topolojisi henüz kesin değildir. Routing hostname'den bağımsız kalır; Next.js DNS kaydı oluşturmaz.
- Origin ve API adresleri merkezi environment/config katmanından, `ADMIN_APP_ORIGIN`, `STOREFRONT_APP_ORIGIN`, `INTERNAL_API_BASE_URL` ve yalnız doğrudan browser erişimi açıkça gerekiyorsa `BROWSER_API_BASE_URL` eşdeğeri değerlerden okunur.
- Production hostname veya localhost portu source code içinde hard-code edilmez. Local adresler repository/config tarafından tanımlanan environment değerlerinden gelir.
- Secret, token ve internal API adresi `NEXT_PUBLIC_*` değişkenine konmaz. Browser'a açılması gereken non-secret runtime config ayrı ve açık bir karardır.
- Muhtemel gelecek topoloji root/www Storefront, ayrı admin subdomain ve ayrı API subdomain'dir; bu yalnız provisional deployment yönüdür, kesin mimari sayılmaz.

### Server ve Client Component sınırı

- Page/layout ve ilk veri okuması Server Component olur.
- `"use client"` yalnız event handler, browser API, drawer/dialog, karmaşık form state, interactive table veya gerçekten gerekli optimistic UI içeren en küçük leaf'e konur.
- Sırf `next/image`, statik markup, server-known formatlama veya ilk fetch için Client Component kullanılmaz.
- Client sınırına yalnız küçük ve serializable props geçirilir; bütün API graph'ı aktarılmaz.
- Bir interaktif kontrol için layout veya bütün page tree client yapılmaz.

### State sahipliği

1. Filtre, arama, sıralama, sayfa ve paylaşılabilir tab: URL search params.
2. Entity, liste, bakiye ve lifecycle state: API/server state.
3. Form draft, drawer, dialog, row selection: en yakın feature-local state.
4. Global client store: yalnız browser-owned state birden çok ilişkisiz route'ta gerçekten ortaksa.

Redux, Zustand veya TanStack Query ile başlanmaz. Server Components, Server Actions, URL state ve local state yetmiyorsa kanıtla ve önce onay al.

## 7. Admin ve storefront ayrımı

### Admin Panel

Admin, kimliği doğrulanmış operasyon yazılımıdır:

- Öncelikleri netlik, hız, erişilebilir veri yoğunluğu, keyboard kullanımı, güvenilir API state'i ve veri tazeliğidir.
- Admin verisi Next.js shared cache'e konmaz; varsayılan `no-store`/private davranıştır.
- Admin route'ları auth ile korunur ve route/layout metadata üzerinden `robots: { index: false, follow: false }` alır.
- `robots.ts` güvenlik mekanizması değildir. Gizli veri yalnız auth/authorization ile korunur.
- Admin sayfalarına storefront canonical, Product JSON-LD veya sitemap davranışı uygulanmaz.

### Public storefront

Storefront `storefront/` altında ayrı bir Next.js uygulaması olarak kuruludur; public özellikleri sonraki ayrı kapsamda geliştirilecektir:

- Server-rendered, crawlable ürün ve collection sayfaları hedeflenir.
- Metadata, canonical, Open Graph, structured data, sitemap, image optimization ve Core Web Vitals storefront sorumluluğudur.
- Admin tasarım yoğunluğu storefront'a, storefront SEO/cache kuralları admin'e taşınmaz.
- Admin-only layout, navigation, authentication/session state veya operational component Storefront'a bağlanmaz. Shared package ancak iki gerçek tüketici ve açık ortak ihtiyaç oluştuğunda çıkarılır.
- Mevcut `/product/[slug]` denemesi ve canonical `/products/{slug}` aynı route değildir. Ayrıca slug endpointleri kaynak kodda bulunup mevcut OpenAPI'de yoktur. Bu sözleşme düzeltilmeden public ürün route stratejisi genişletilmez.

## 8. Sidebar ve bilgi mimarisi

Phase 1'de sidebar aşağıdaki bilgi mimarisini temsil eder. Grup başlıkları açılıp kapanabilir; kullanıcı grup başlığına bastığında alt öğeler görünür. `Planned`, `Future` ve `Placeholder` alt öğeler de görünür fakat gerçek route/contract uygulanana kadar disabled kalır, navigasyon yapmaz ve “Planlandı/Yakında” metniyle açıklanır.

| Grup | Öğe | URL/konum | Durum | Sözleşme notu |
| --- | --- | --- | --- | --- |
| Overview | Dashboard | `/dashboard` | Phase 1 | Genel dashboard metric endpointi yok; sahte kart gösterme. |
| Commerce | Orders | `/orders` | Phase 1 | E-ticaret `Order`; Accounting sales değildir. |
| Commerce | Products | `/products` | Phase 1 | API ürün listesi. |
| Commerce | Add Product | `/products/new` | Phase 1 | Sidebar'da Products altında ikincil öğe/quick action olabilir. |
| Commerce | Collections | `/collections` hedefi; şimdilik disabled | Planned next | API destekler. Klasik `Category` entity yok; ana kategori kavramı `ProductType`tır. |
| Commerce | Campaigns | grup/placeholder | Planned next | Mevcut backend yalnız `Coupons` sözleşmesini destekler; genel campaign motoru varsayılmaz. |
| Commerce | Coupons | `/coupons` hedefi; şimdilik disabled | Planned next | Campaigns altındaki gerçek belgelenmiş capability. |
| Operations | Stock Operations | `/inventory/stock-movements` hedefi; şimdilik disabled | Planned next | Gerçek terim `StockMovement`; stok doğrudan edit edilmez. |
| Operations | Customers | `/customers` hedefi; şimdilik disabled | Planned next | Ayrı customer API yok; Admin Users API'sinin belgelenmiş Customer rol filtresine dayanır. |
| Accounting | Accounting Overview | disabled | Placeholder | Aggregate overview endpointi yoktur. |
| Accounting | Current Accounts | `/accounting/current-accounts` hedefi; şimdilik disabled | Planned next | Backend mevcut; frontend Phase 1 dışı. |
| Accounting | Purchase Invoices | `/accounting/purchase-invoices` hedefi; şimdilik disabled | Planned next | Backend mevcut; frontend Phase 1 dışı. |
| Accounting | Accounting Sales Orders | `/accounting/sales-orders` hedefi; şimdilik disabled | Planned next | E-ticaret Orders'dan açıkça ayrı adlandırılır. |
| Accounting | Sales Invoices | `/accounting/sales-invoices` hedefi; şimdilik disabled | Planned next | Opsiyonel Accounting belgesi. |
| Accounting | Payments and Collections | `/accounting/payments` hedefi; şimdilik disabled | Planned next | PaymentAllocation cari harekete yapılır. |
| Accounting | Cash and Bank | `/accounting/treasury` hedefi; şimdilik disabled | Planned next | Kasa, banka, statement, financial transaction ve transfer yeteneklerini gruplayabilir. |
| Accounting | Expenses | `/accounting/expenses` hedefi; şimdilik disabled | Planned next | Mevcut lifecycle create/list ile sınırlıdır. |
| Accounting | Accounting Reports | `/accounting/reports` hedefi; şimdilik disabled | Planned next | Her raporun ayrı kolon anlamı vardır. |
| Marketplace Integrations | Integrations Overview | disabled | Future module | Backend provider/integration sözleşmesi yok. |
| Marketplace Integrations | Marketplace Connections | disabled | Future module | Bağlantı API'si yok. |
| Marketplace Integrations | Product Synchronization | disabled | Future module | Sync state/log contract yok. |
| Marketplace Integrations | Order Synchronization | disabled | Future module | Sync state/log contract yok. |
| System | Administrators | `/administrators` hedefi; şimdilik disabled | Planned next | Admin Users API rol/status işlemlerini destekler. |
| System | Settings | disabled | Placeholder | Genel settings endpointi yok; shipping methods ve tax rates ayrı capability'dir. |

Sidebar controller sayısının aynası olmaz. Nadir konfigürasyonlar altta kalır; her aksiyon bir üst seviye linke dönüşmez. Desktop'ta kalıcı/çökebilir sidebar, mobilde focus trap ve focus restore sağlayan erişilebilir drawer kullanılır. Link gizlemek authorization değildir.

## 9. API entegrasyon kuralları

- Tek bir `server-only` typed API client; `INTERNAL_API_BASE_URL` eşdeğeri base URL, Bearer injection, JSON, timeout/abort, empty `204`, ProblemDetails ve güvenli retry davranışını yönetir.
- Tercih edilen authenticated browser akışı `Browser → aynı-origin Next.js BFF → ASP.NET Core API` şeklindedir. Browser ASP.NET access/refresh tokenlarını doğrudan yönetmez.
- Internal API base URL yalnız server environment değişkenidir; `NEXT_PUBLIC_` olamaz. Secret/token hiçbir public env değişkenine konmaz.
- Endpoint stringleri visual componentlere veya farklı feature'lara dağılmaz.
- Server Component ilk GET için ortak server-only fonksiyonu doğrudan çağırır. Kendi Next Route Handler'ına internal HTTP isteği atmaz.
- Browser-facing authenticated operasyonlar kontrollü BFF Route Handler veya uygun Server Action sınırından geçer. Route Handler auth cookie, browser proxy, upload/download, callback, webhook veya gerçek browser-facing HTTP sınırı gerektiğinde kullanılır.
- ASP.NET endpointlerinin tamamı mekanik olarak Next Route Handler ile mirror edilmez.
- Request cancellation/timeout desteklenir. Kullanıcı cancellation ile timeout birbirinden ayrılır.
- Response content-type ve status kontrol edilmeden JSON parse edilmez; `204` body parse edilmez.
- `400/401/403/404/409/429/500`, network, timeout ve non-JSON upstream failure merkezi normalize edilir.
- Güvenli hata modeli `status`, `code`, `detail`, `errors`, `traceId`, `timestamp` ve varsa `Retry-After` değerini korur; stack trace, token ve secret URL göstermez.
- `400` alan hataları forma eşlenir; global hata da korunur. `409` sonrasında veri yeniden okunur, otomatik overwrite/retry yapılmaz.
- Non-idempotent mutation otomatik retry edilmez. Belgelenmiş idempotent intent tekrarında aynı key korunur.
- Duplicate submit engellenir; loading, success, empty ve error state açık olur. Hata sessizce yutulmaz.
- Frontend preview hesaplayabilir; fiyat, indirim, vergi, shipping, stok, fatura, bakiye, paid/remaining, FIFO maliyet ve kâr için API son otoritedir.

Wire type'lar uzun vadede OpenAPI'den `src/generated/api.ts` içine üretilir ve dosya elle değiştirilmez. Kullanıcı `openapi-typescript` eklenmesine izin vermiştir; fakat kurulum ancak API/OpenAPI belgeleri önce güncellendikten sonra, ilgili uygulama görevinde yapılır. Güncel olmayan OpenAPI'den broad generated type katmanı kurulmaz. Bu sürede gerekli dar tipler yalnız belgelenmiş contracttan çıkarılır, tüm API DTO'ları elle çoğaltılmaz.

## 10. Authentication kuralları

ASP.NET API JWT Bearer kullanır ve login/refresh response'unda access + refresh token ile backend expiry zamanlarını döndürür. Frontend BFF modeli uygular:

- Access ve refresh token browser JavaScript'ine, localStorage/sessionStorage'a, serialized props'a veya HTML'e verilmez.
- Tokenlar ayrı `HttpOnly`, production'da `Secure`, `SameSite=Lax`, `path=/`, Domain belirtilmemiş cookie'lerde tutulur.
- Cookie'ler host-only olur; ilk aşamada cross-subdomain cookie paylaşımı veya SSO uygulanmaz. Admin ve gelecekteki Storefront session'ları ayrı kalır.
- Cookie expiry backend response'undaki exact expiry değeridir.
- Cookie set/rotate/delete yalnız Server Action veya Route Handler'da yapılır; Server Component render sırasında cookie yazamaz.
- Login server-to-server `/api/auth/login` çağırır, iki cookie'yi yazar ve client'a yalnız güvenli session/user bilgisi döndürür.
- Refresh `/api/auth/refresh-token` ile refresh body gönderir ve dönen iki cookie'yi birlikte rotate eder.
- Refresh sonrası orijinal istek en fazla bir kez tekrarlanır; loop ve paralel refresh race engellenir.
- Logout upstream `/api/auth/logout` çağrısını dener ve upstream hata verse bile local cookie'leri `finally` içinde temizler.
- Next.js 16'da `middleware.ts` değil `proxy.ts` kullanılır. Proxy yalnız hızlı/optimistic route gate ve redirect içindir; tam session yönetimi veya authorization sınırı değildir.
- Her Server Action, Route Handler ve server-side data operation session/role kontrolünü yeniden yapar; ASP.NET son authorization otoritesidir.
- JWT payload decode etmek authorization kanıtı değildir. Expiry için yalnız yardımcı olabilir.
- Admin olmayan kullanıcı için `403`, oturum kaybı için `401` davranışı korunur.
- `returnTo` yalnız relative same-origin path olarak doğrulanır.
- Cookie-authenticated mutation boundary'leri POST semantics ve Origin/same-origin kontrolüyle CSRF'e karşı korunur.
- State-changing BFF isteklerinde SameSite koruması, Origin doğrulaması, uygun olduğunda Referer doğrulaması ve seçilen session modelinin gerektirdiği CSRF koruması birlikte değerlendirilir.
- Authenticated response shared cache'e girmez; browser history/back ile logout sonrası kullanılabilir private content kalmamalıdır.

### Route ve redirect sözleşmesi

- Unauthenticated kullanıcı `/` ziyaretinde `/login`'e redirect edilir.
- Authenticated kullanıcı `/` ziyaretinde `/dashboard`'a redirect edilir.
- Unauthenticated kullanıcı protected Admin route'larında `/login`'e redirect edilir.
- Authenticated kullanıcı `/login` ziyaretinde `/dashboard`'a redirect edilir.
- Next.js route group'ları layout/auth sınırı içindir ve URL'de görünmez. `/admin` prefix eklenmez.
- Fake auth, hard-coded admin credential veya undocumented token davranışı oluşturulmaz. Redirect ve protection implementasyonundan önce auth belgeleri ile API-side `AGENTS.md` yeniden okunur.
- API auth sözleşmesi BFF akışını karşılamıyorsa route yapısı ve login UI temeli kurulabilir; eksik contract belgelenir ve auth davranışı uydurulmadan çalışma o sınırda durur.

### Credential ve test güvenliği

- Gerçek veya tekrar kullanılabilir credential; source code, `AGENTS.md`, README, committed env, frontend config, `appsettings.json` veya test source'a yazılmaz.
- Development seed yalnız Development ortamında, açıkça etkinleştirildiğinde ve production olmayan veritabanında çalışabilir. `ENABLE_DEVELOPMENT_SEED`, `SEED_ADMIN_EMAIL`, `SEED_ADMIN_PASSWORD` eşdeğeri secret/config kullanır; gerçek password hasher ile idempotent çalışır ve değerler eksikse atlanır.
- Integration testleri izole/ephemeral test database kullanır, kullanıcıyı setup sırasında runtime-generated test credential ile oluşturur ve suite'ler arasında state'i resetler. Development veya production database'e bağlanmaz.
- Local E2E credential uncommitted local env'den, CI credential encrypted secret store'dan gelir. Dedicated least-privilege test hesabı kullanılır; production admin hesabı kullanılmaz.
- Production bilinen parolalı admin'i otomatik seed etmez. İlk admin explicit secure bootstrap/deployment secret veya onaylı manuel operasyonla oluşturulur. Secret, token, cookie, connection string ve parola loglanmaz.

## 11. Product feature kuralları

### Product list

Phase 1 ürün listesi `GET /api/products` kullanır. Belgelenmiş query alanları:

- `pageNumber`, `pageSize`
- `search`
- `typeId`, `brandId`
- `status`, `isActive`, `isFeatured`
- `sortBy`, `descending`

`ProductSortBy`: `0 DisplayOrder`, `1 Title`, `2 CreatedAt`, `3 PopularityScore`. `ProductStatus`: `0 Draft`, `1 Active`, `2 Passive`, `3 Archived`. Numeric wire değerleri generated/verified contracttan gelir; UI label'ları ayrı map'te tutulur.

- Filtre ve sayfalama URL'de tutulur; filtre değişince `pageNumber=1` olur.
- Loading, empty dataset, no filtered result, API error ve retry durumları ayrıdır.
- Server pagination kullanılır; bütün ürünler client'a çekilmez.
- Liste satırı için ayrı detail çağrısı yapılmaz.
- Mevcut endpoint `PagedResult<ProductDto>` döndürür ve variants/tags graph'ını da taşır; ayrı `ProductSummaryDto` yoktur. Kullanıcı Phase 1 öncesinde backend'e küçük product list summary ve main-image alanı eklenmesini onaylamıştır. Önce API/OpenAPI/endpoint belgeleri güncellenir; frontend yalnız güncellenmiş sözleşme yayımlandıktan sonra bu alanları kullanır. N+1 image/detail fetch ile eksik contract gizlenmez.
- Güncel Product DTO listede image içermez. Yeni belgelenmiş main-image sözleşmesi gelene kadar thumbnail uydurulmaz.

### Add Product

Route `/products/new` olur. Form tek dev component olmaz; aşağıdaki sorumluluklara ayrılır:

1. Basic information: title, main SKU, URL, description.
2. Organization: ProductType, brand, collections, tag adları ve tax rate.
3. State: numeric ProductStatus, active, featured, display order.
4. Variants: her ürün için en az bir variant; name, value, SKU, price, stock, optional compare-at price, barcode, material, active ve belgelenmiş opening cost alanları.
5. SEO: SEO title ve description.
6. Images: ürün create response'undaki public product ID alındıktan sonra ayrı ProductImage endpointleriyle URL, alt text, main ve display order.

Kesin kurallar:

- Klasik `Category` entity oluşturulmaz. Ana kategori kavramı `ProductType`, merchandising grubu `Collection`dır.
- Variant `name` ve `value` gerçek backend modeline göre ayrı gönderilir. Backend kombinasyon üretmez; birleştirilmiş seçenek en fazla üç parçalı olabilir.
- `hasVariants` ve `netPrice` response-only'dir; request'e konmaz.
- Product create en az bir variant ile atomiktir. Image upload endpointi belgelenmemiştir; yalnız URL tabanlı ProductImage sözleşmesi vardır.
- Product create ile image işlemleri ayrı endpointler olduğundan UI yanlış bir “tamamı tek transaction” vaadi vermez. Kısmi başarıyı açık raporlar ve oluşturulan product ID'yi korur.
- Stock normal ürün alanı gibi güncellenmez. Opening stock create sözleşmesine göre; sonraki stok değişimi signed `StockMovement` ile yapılır.
- Collection, ProductType, Brand ve TaxRate selector'ları yalnız mevcut list/pagination sözleşmesini kullanır; belgelenmemiş search uydurmaz.
- Form validation server kurallarını taklit eden ikinci business engine olmaz. Client validation kullanıcı girdisi ve açık length/required sınırları içindir; API tekrar doğrular.
- Uzun ürün adı, yüksek fiyat, çok variant, sıfır stock, eksik görsel ve uzun validation mesajı test edilir.

## 12. Order feature kuralları

`Order`, authenticated customer/cart checkout aggregate'ıdır. `AccountingSalesOrder` değildir. Route, type, DTO, label ve service dosyalarında bu kavramlar asla karıştırılmaz.

Phase 1 admin Orders:

- Liste: `GET /api/orders`.
- Detay: `GET /api/orders/admin/{id}`.
- Liste query'si yalnız `pageNumber`, `pageSize`, `status`, `createdFromUtc`, `createdToUtc` destekler.
- Mevcut `OrderSummaryDto`: `id`, `orderNumber`, `status`, `grandTotal`, `itemCount`, `createdAt`, `paidAt`.
- Güncel liste response'unda customer adı/ID'si ve free-text search sözleşmesi yoktur. Kullanıcı Phase 1 öncesinde Orders listesine belgelenmiş search/customer alanlarının backend tarafından eklenmesini onaylamıştır. API/OpenAPI ve endpoint belgeleri güncellenene kadar bu kolon ve filtreler uygulanmaz; detail N+1 çağrısı yapılmaz.
- Sort veya payment filter hâlâ belgelenmemiştir; ayrıca onaylı API contractı oluşmadan UI'da gösterilmez.
- Detail, immutable item ve shipping address snapshot'larını, payments, totals ve lifecycle timestamp'lerini API'den geldiği gibi gösterir.
- Phase 1 varsayılanı liste, filtre, pagination, status display, detay, loading/empty/error state'tir. Status mutation ayrıca istendiğinde exact transition kuralları tekrar doğrulanır.
- Generic status endpointi refund veya return statuslarını set etmez; bunlar dedicated workflow'dur. Unsupported action eklenmez.
- Kaynak kodda bulunan fakat docs/OpenAPI'de olmayan `/api/orders/import` ve `/api/orders/import/bulk` frontend ya da marketplace sözleşmesi sayılmaz ve kullanılmaz.

## 13. Accounting feature kuralları

Accounting backend'de ayrı ve gelişmiş bir modüldür; frontend Phase 1 dışındadır. Sidebar alanı ayrılır fakat açık istek olmadan sayfa/route/API entegrasyonu oluşturulmaz.

Domain ayrımları:

- `CurrentAccount`: Accounting müşteri/tedarikçi master kaydıdır. Türü Customer, Supplier veya CustomerAndSupplier olabilir. Accounting adres alanları mevcut modelde doğrudan CurrentAccount üzerindedir; ayrı Supplier veya CurrentAccountAddress uydurulmaz.
- `PurchaseInvoice`: önceden yaratılmış uygun pozitif Purchase `StockMovement` miktarlarına allocation yapar. Posting hiçbir zaman yeni fiziksel `StockMovement` oluşturmaz; supplier debt ve FIFO cost layer oluşturabilir.
- `AccountingSalesOrder`: `UserId`, Cart veya e-ticaret Order gerektirmez. Doğrudan verilen `ProductVariant` satırlarını kullanır. Posting existing StockMovement altyapısında `AccountingSale` stock-out, FIFO consumption ve positive total varsa tek customer receivable oluşturur.
- `SalesInvoice`: AccountingSalesOrder'a bağlı opsiyonel belgedir. İkinci stock movement veya ikinci receivable oluşturmaz.
- `StockMovement`: tek fiziksel stok ledger'ıdır. ProductVariant stock yalnız bunun transactionally updated read cache'idir.
- `InventoryCostLayer`: maliyet kaynağıdır; fiziksel stock kaynağı değildir. FIFO consumption satış maliyetini belirler.
- `CurrentAccountTransaction`: supplier debt, customer receivable, payment ve reversal için değişmez cari ledger hareketidir.
- `Payment`: allocation'ı SalesInvoice'a değil `CurrentAccountTransactionId`'ye yapar. CustomerCollection en az bir receivable allocation ister; SupplierPayment allocations boşsa unallocated supplier advance olabilir.
- Cash/Bank balance doğrudan yazılmaz; `FinancialTransaction` hareketlerinden türetilir. Payment'ta tam olarak bir cash veya bank account seçilir.

Lifecycle ve UI:

- AccountingSalesOrder, SalesInvoice ve PurchaseInvoice: Draft, Posted, Cancelled.
- Yalnız Draft düzenlenir. Post/cancel/reversal sonrası detay yeniden okunur.
- Cancelled/reversed kayıt silinmez; original ve reversal geçmişi gösterilir.
- `409 conflict` ve `concurrency_conflict` draft'ı korur, current state'i yeniler ve kullanıcı kararı ister.
- Retry aynı user intent ise aynı Idempotency-Key korunur.
- TRY ve exchange rate `1` mevcut Accounting sözleşmesidir.
- API-calculated VAT, discount, totals, paid/remaining, FIFO cost, valuation, balance ve profit kesin otoritedir.
- Accounting report'ları ortak `AccountingReportRowDto` kullansa da `amount/secondaryAmount/tertiaryAmount` anlamı rapora göre değişir. Her rapor kendi kolon map'ine sahip olur; tek generic finance table yapılmaz.
- Rapor response'unda grand total/devreden bakiye sözleşmesi yoktur. Current page satırları genel toplam gibi sunulmaz.

Belgelenmemiş accounting özellikleri oluşturulmaz: opening current/cash/bank balance, sales/purchase return invoice, debit-credit/FX difference note, general expense update/post/cancel/delete, attachment/archive, financial period/closing, report export/print, bank reconciliation/import, cheque/promissory note, granular non-admin accounting role veya harici e-invoice/ERP/marketplace entegrasyonu.

## 14. Marketplace integration durumu

Marketplace integrations henüz uygulanmamıştır. PrePazar benzeri sistem uzun vadeli plandır.

- Trendyol, Hepsiburada, Amazon, Shopify, PrestaShop veya başka provider bağlı kabul edilmez.
- Sahte connection, sync status, product/order mapping, webhook, error log veya marketplace verisi oluşturulmaz.
- Accounting enum'unda `MarketplaceCommission` bulunması marketplace bağlantısı olduğu anlamına gelmez.
- Kaynak koddaki generic order import/performance metric operasyonları belgelenmiş provider adapter veya synchronization modülü değildir.
- Sidebar grubu future/disabled kalır; route ve page oluşturulmaz.
- Gelecekte backend contract onaylandığında provider-adapter yapısı kullanılır. Connection secretları, marketplace products/orders/stocks/prices ve sync logs ayrı backend sözleşmelerine dayanır.

## 15. Performans kuralları

- Önce ölç, sonra optimize et. Development server veya tek Lighthouse skoru Core Web Vitals kanıtı değildir.
- Page/layout Server Component, client leaf küçük tutulur. Route-local state için global provider eklenmez.
- Initial data client `useEffect` ile çekilip waterfall oluşturulmaz.
- Bağımsız server istekleri birlikte başlatılır ve uygun yerde `Promise.all`/Suspense kullanılır.
- Liste endpointi ve server pagination kullanılır; detail graph veya bütün dataset alınmaz.
- Büyük client-side sort/filter/aggregate yapılmaz; desteklenen server query kullanılır.
- Admin/accounting/auth/order verisi shared cache'e girmez. Admin'de tazelik, public synthetic skor iyileştirmesinden önce gelir.
- Mevcut Next config `cacheComponents` açmamıştır. Bu özellik ölçüm ve açık onay olmadan etkinleştirilmez.
- Storefront catalog cache'i gelecekte explicit freshness, tags ve dar invalidation ile tasarlanır.
- Product/content görsellerinde `next/image`, sabit dimension/aspect ratio ve doğru `sizes` kullanılır. Gerçek LCP görseli dışında her grid görseli eager/priority yapılmaz.
- `next/font` ve tek font ailesi korunur; gereksiz weight/font/icon seti eklenmez.
- Dynamic import yalnız gerçekten non-critical ağır interaktif modülde ölçülebilir yarar sağlıyorsa kullanılır; ana içerik/LCP gizlenmez.
- Pagination varsayılandır. Virtualization yalnız gerçekten büyük interactive dataset ölçüldüğünde ve accessibility korunabildiğinde kullanılır.
- Loading skeleton final geometriyi korur; aşırı shimmer ve layout shift üretmez.
- Storefront için LCP, INP ve CLS field verisi 75. percentile mobile/desktop ayrımıyla değerlendirilir. Lighthouse lab diagnostic'tir; field pass/fail değildir.

## 16. SEO kuralları

### Admin

- Bu uygulamadaki bütün authenticated Admin route'ları (`/dashboard`, `/orders/**`, `/products/**` ve gelecekteki operasyon route'ları), `/login` ve internal operation route'ları `noindex` olur; sitemap'e girmez.
- Auth gizlilik sağlar; robots/noindex tek başına gizlilik sağlamaz.
- Admin page'lerine Product/ProductGroup/Breadcrumb rich result markup eklenmez.

### Gelecekteki storefront

- Ayrı Storefront uygulamasında merkezi yapılandırılmış application name ile root `metadataBase`, title template, default description ve Open Graph defaults tanımlanır; geçici `SERANTIS` adı veya kesinleşmemiş domain kalıcı değer gibi hard-code edilmez.
- Dynamic product/collection route'u aynı authoritative fetch ile `generateMetadata` ve page verisini üretir/deduplicate eder.
- Title, description, visible `h1`, canonical ve Open Graph aynı page intent'i taşır.
- Canonical absolute HTTPS ve tracking/sort/view parametresiz olur.
- Home, useful populated collection/category landing ve active public product indexlenebilir.
- Internal search ve arbitrary low-value filters varsayılan `noindex, follow`; tracking/sort duplicates clean URL'ye canonical olur.
- Pagination kendi adresine sahip olur; her page page 1'e canonical edilmez.
- `sitemap.ts` yalnız canonical, indexlenebilir, `200` URL'leri ve gerçek `lastModified` değerlerini içerir.
- `robots.ts` crawl kontrolü, metadata index kararıdır. `noindex` görülmesi istenen public URL robots ile körlemesine bloklanmaz.
- Product JSON-LD yalnız görünür gerçek ürün verisiyle; variant family gerçekten uygunsa ProductGroup; breadcrumb görünür gerçek hierarchy ile oluşturulur.
- Rating, review, GTIN, discount, shipping, returns veya stock uydurulmaz. JSON-LD `<` karakterini escape eder ve initial server HTML'de yer alır.
- Core Web Vitals ve rendered HTML production-like ortamda doğrulanmadan SEO başarısı iddia edilmez.

## 17. Mobile-first kuralları

- Layout önce dar mobil genişlikte tasarlanır, sonra desktop yoğunluğu eklenir.
- Sidebar mobilde erişilebilir drawer olur; background inert, focus contained ve kapanınca trigger'a geri döner.
- Dokunmatik hedefler rahatça kullanılabilir, kritik aksiyon yalnız hover ile görünmez.
- Form rail/yan kolonlar mobilde karar sırasına göre stack edilir; submit ve validation erişilebilir kalır.
- Büyük table mobilde önem sırasına göre condensed row/card, kontrollü horizontal scroll veya column reduction kullanır. ID, status ve primary action kaybolmaz.
- Filter paneli mobilde açılıp kapanabilir; aktif filtreler ve clear action görünür kalır.
- Sticky action yalnız içeriği, focus edilen kontrolü ve virtual keyboard'u örtmüyorsa kullanılır.
- Narrow mobile, geniş mobile, gerekli tablet ve desktop viewport'ları test edilir. Yalnız desktop ekran görüntüsü kabul kanıtı değildir.

## 18. Accessibility kuralları

Hedef WCAG 2.2 AA'dır.

- Semantic landmarks, tek anlamlı `h1`, düzenli heading sırası ve skip link kullanılır.
- Button aksiyon, link navigasyon içindir. Tıklanabilir `div` kullanılmaz.
- Her input persistent programmatic label, gerekli autocomplete/input type/inputMode ve bağlı hata mesajı alır.
- Uzun formda error summary invalid field'lara bağlanır; safe input korunur, parola/token geri doldurulmaz.
- Focus her interactive elementte görünür ve overflow ile kesilmez.
- Menu, dialog, drawer, combobox, tab ve table sort state keyboard ile çalışır; focus trap/restore doğrudur.
- Status, selected, error veya success yalnız renkle anlatılmaz.
- Default, hover, focus, selected, disabled ve error kontrastı doğrulanır.
- Disabled state okunabilir olur ve nedeni açık değilse açıklanır. Busy state yalnız opacity ile gösterilmez; `aria-busy`/uygun announcement kullanılır.
- Async loading, save result ve önemli error uygun live region davranışıyla duyurulur; focus rastgele taşınmaz.
- Reduced motion, 200% zoom ve uygun yerde 400% reflow kontrol edilir.
- Gerçek screen reader testi yoksa “not verified” denir; accessibility tree/axe sonucu screen-reader pass sayılmaz.

## 19. Görsel tasarım kuralları

SERANTIS Admin karakteri kompakt, veri odaklı, hızlı, işlevsel, desktop operasyonlarına uygun fakat mobil kullanılabilir ve az dekoratiftir.

- Tailwind v4 `@theme` ve CSS variables ile küçük token temeli oluşturulmadan çok sayıda page yapılmaz.
- Token rolleri: page, surface, border, foreground, muted, primary action, focus ve semantic success/warning/danger/info.
- Mavi; primary action, link, selected nav ve focus cue için ölçülü kullanılır. Her surface maviye boyanmaz.
- Başlangıç ölçüleri bir kontrattır, dogma değildir: page heading 20–24px, body/control 14px, desktop control 32–40px, mobile target yaklaşık 44px, table row 48–56px, sidebar yaklaşık 240–256px, topbar yaklaşık 52–56px. Gerçek içerikle doğrulanır.
- 4px tabanlı küçük spacing scale; control için 6–8px, ana grouping/overlay için 10–12px radius kullanılır. Her component kendi radius/shadow değerini icat etmez.
- Borders sakin yüzeyleri ayırır; shadow menu/popover/drawer/dialog için saklanır.
- Her bölüm card içine alınmaz. Heading, divider, whitespace ve layout grouping tercih edilir.
- Bir page veya bounded form bölgesinde tek primary action vardır. Destructive red yalnız gerçek karar noktasında kullanılır.
- Status badge yalnız gerçek semantic state içindir; her label badge olmaz.
- Gradient, glow, glassmorphism, büyük blur/shadow, dev hero, renkli eyebrow, gereksiz animation ve dekoratif chart varsayılan olarak yasaktır.
- Hazır component kütüphanesi default görünümüyle bırakılmaz; ayrıca yeni UI library onaysız eklenmez.
- Dashboard metric yalnız endpoint, period, filter ve scope doğrulanmışsa gösterilir. Current page toplamı global metric değildir.
- Loading final yapıya benzer; empty state kısa ve gerçek next action içerir; error neyin başarısız olduğunu ve geçerli recovery'yi söyler; fake illustration/metric kullanılmaz.
- Uzun ad, missing image, high price, out-of-stock, empty, timeout, long error ve dense data durumlarıyla test edilir.
- Referansla çalışma halinde önce baseline ve sorun raporu, sonra değişiklik; aynı fixture/viewport/state ile before/after screenshot ve yeniden değerlendirme yapılır.
- Görsel iyileştirme accessibility, LCP, CLS, INP veya bundle maliyetini kötüleştiremez.

## 20. Test ve doğrulama komutları

Paket yöneticisi pnpm'dir. Workspace geneli doğrulama komutları `UI/` kökünden çalıştırılır; yalnız bir uygulama hedefleniyorsa ilgili `*:admin` veya `*:storefront` script'i kullanılır:

```powershell
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```

Workspace ile `admin/` ve `storefront/` uygulamalarının her birinde `typecheck` script'i vardır. Mevcut unit runner Vitest'tir. Playwright Test ve axe dependency'leri kurulu değildir; açık onay olmadan yeni test framework/library eklenmez.

Kullanıcı `openapi-typescript`, uygun bir form-validation dependency'si ve browser/accessibility test dependency'lerinin eklenmesine izin vermiştir. Bu onay bu AGENTS güncellemesinde paket kurma talimatı değildir. İlgili implementation görevinde mevcut package sürümleri kontrol edilir, en küçük gerekli set seçilir, pnpm ile kurulur ve lockfile/bundle/bakım etkisi raporlanır. Başka yeni dependency'ler için ayrıca onay gerekir.

Phase 1 için minimum test matrisi:

- Login: valid/invalid, validation, protected route, refresh expiry/failure, logout ve 401/403 ayrımı.
- Products: loading, populated, empty, filters, sort, pagination, API failure ve Add Product linki.
- Add Product: required alanlar, variant name/value, minimum one variant, duplicate submit, API field/global errors ve partial image failure.
- Orders: status/date filters, pagination, detail, 404, empty, API failure; undocumented search beklenmez.
- Responsive: sidebar drawer, filters, tables, complex form ve overlays en az desktop + mobile.
- Accessibility: keyboard/focus, labels/errors, contrast, open drawer/dialog, reduced motion ve screen-reader durumu.
- Runtime: unexpected console error, page exception, failed request, unexpected 4xx/5xx, redirect/refresh loop, hydration warning ve broken image/font.

Test fixture ve credential secret tutulur; repo'ya cookie, token, storage state, parola, adres veya ödeme verisi yazılmaz. Test ortamında güvenli seed/reset sözleşmesi yoksa shared data silinmez; blocker raporlanır.

## 21. Yasak eylemler

- Belgelenmemiş endpoint, DTO, enum, filter, sort, role, transition veya workflow icat etmek.
- Frontend component içinde backend pricing, stock, discount, tax, accounting veya lifecycle kuralını yeniden uygulamak.
- API kodunu, migrations'ı, API dokümantasyonunu veya Accounting kodunu frontend görevinin yan etkisi olarak değiştirmek.
- Marketplace bağlantısı veya sync state'i varmış gibi page/data üretmek.
- Sahte business metric veya placeholder veriyi gerçek veri gibi göstermek.
- Bütün page/layout tree'ye gereksiz `"use client"` eklemek.
- Raw `fetch` çağrılarını visual componentlere dağıtmak veya endpoint stringlerini kopyalamak.
- Token/secret'ı `NEXT_PUBLIC_`, browser storage, log, HTML veya analytics'e koymak.
- Kullanıcının açıkça onayladığı OpenAPI generation, form validation ve browser/accessibility test araçları dışındaki package, test framework, state library, UI kit, font veya icon setini onaysız eklemek.
- Onaysız kalıcı logo veya marka paleti oluşturmak.
- Her içeriği card yapmak; aşırı gradient, glass, blur, glow, shadow, radius ve animation kullanmak.
- Mobile'ı sonradan düzeltilecek ikincil kapsam saymak.
- Error'ı yutmak, yalnız toast ile kritik sonucu bildirmek veya form inputunu gereksiz kaybetmek.
- Planned/disabled sidebar öğesine boş veya fake page route'u oluşturmak.
- Storefront SEO/cache kurallarını admin'e veya admin no-store kuralını tüm storefront'a körlemesine taşımak.
- Kullanıcı açıkça istemeden commit, push veya deployment yapmak.

## 22. Definition of done

Bir frontend dilimi yalnız şu koşullarda tamamdır:

1. İlgili Markdown endpoint sözleşmesi, OpenAPI operasyonu ve gerektiğinde controller/DTO doğrulanmıştır.
2. Contract gap yoktur veya ayrı blocker olarak raporlanıp kullanıcı kararı alınmıştır.
3. URL/code İngilizce, UI copy kararlaştırılmış dilde ve route file incedir.
4. Server Component varsayılanı korunmuş, client leaf ve props minimumdur.
5. API server-only typed boundary'den çağrılır; token ve private data client'a sızmaz.
6. Loading, empty, validation, permission, not-found, conflict, timeout ve unexpected error durumlarından ilgili olanlar uygulanmıştır.
7. Mobile ve desktop layout; keyboard, focus, labels ve contrast doğrulanmıştır.
8. Fake metric/data yoktur; backend-calculated değerler otoritedir.
9. Admin route noindex ve auth/authorization kontrollüdür.
10. `UI/` kökünden `pnpm lint`, `pnpm typecheck`, `pnpm test`, `pnpm build` geçer veya exact blocker çıktısı raporlanır.
11. Runtime erişimi varsa console/network ve ilgili browser akışları kontrol edilmiştir; yoksa neyin doğrulanmadığı yazılmıştır.
12. Diff yalnız istenen kapsamı içerir; yeni dependency/route/abstraction gerekçesiz eklenmemiştir.

## 23. Phase 1 uygulama kapsamı

Uygulama sırası:

1. API/OpenAPI/endpoint belgelerini önce güncelle: bilinen drift, product list summary + main image ve Orders search/customer contractları frontend entegrasyonundan önce tamamlanır.
2. Güncel auth belgeleri ve API-side `AGENTS.md` üzerinden BFF yeterliliğini doğrula; eksik contract varsa auth davranışı uydurmadan raporla ve dur.
3. Onaylanmış gerekli OpenAPI generation, form validation ve browser/accessibility test araçlarını ilgili implementation kapsamında kur.
4. Küçük provisional Tailwind v4 nötr + ölçülü mavi token temeli, merkezi uygulama config'i ve geçici text wordmark.
5. `/login`, root redirect sözleşmesi ve BFF login/refresh/logout ile protected route gate.
6. Admin shell: responsive sidebar, topbar, page frame ve noindex layout.
7. Dashboard entry: doğrulanmamış metric olmadan gerçek quick links/operational entry.
8. Products list: güncellenmiş list contractındaki filter/sort/pagination/summary/main-image ve durumlar.
9. Add Product: progressive groups, real variant model, server validation ve partial image workflow.
10. Orders list/detail: güncellenmiş search/customer desteği ile belgelenmiş filter/pagination ve gerçek summary/detail fields.
11. Lint, type-check, unit tests, production build, mobile/accessibility/runtime doğrulaması.

Phase 1; accounting sayfaları, marketplace route'ları, customers, coupons/campaigns, stock operations, administrators, settings veya storefront implementasyonu içermez.

## 24. Gelecek aşamalar

Kullanıcı Phase 1'i kabul ettikten ve contractlar netleştikten sonra önerilen sıralama:

1. Collections/ProductType/Brand/TaxRate yönetimi ve Product detail/edit/image/variant operasyonları.
2. StockMovement operasyonları, returns ve Coupons.
3. Customers ve Administrators.
4. Accounting frontend'i: Current Accounts → purchase/accounting sales documents → invoices → payments/treasury → expenses/costing → report-specific screens.
5. Public storefront: home, collections, product routes, cart ve checkout; SEO/CWV matrisiyle.
6. Marketplace integrations: ancak provider connection, mapping, sync ve log backend sözleşmeleri onaylandıktan sonra provider-adapter mimarisiyle.

Bu sıra otomatik yetki değildir. Her yeni aşama ayrı kullanıcı kapsamı ve contract incelemesi ister.

## 25. Bilinen bilinmeyenler ve stop koşulları

| Konu | Mevcut kanıt/çelişki | Zorunlu davranış |
| --- | --- | --- |
| OpenAPI güncelliği | OpenAPI 159 path/208 schema; kaynak kodda belgelenmemiş product SEO/performance ve order import operasyonları var. | Kullanıcı belgelerin önce güncellenmesini seçti. Güncelleme tamamlanmadan frontend contract entegrasyonuna başlama ve source-only endpoint kullanma. |
| OpenAPI auth security | Global Bearer security public AuthController operasyonlarına da uygulanıyor; `security: []` override yok. | Runtime `[AllowAnonymous]` davranışını koru; contract gap'i raporla. |
| Auth success/error schema | OpenAPI register/logout/forgot/reset exact success statuslarını ve ProblemDetails error schema'larını eksik gösteriyor. | Controller + auth docs ile doğrula; generated error tipi uydurma. |
| Generated endpoint docs | Bazı dosyalar create/logout statusunu `200`, array örneğini object ve bazı list response'larını boş gösteriyor. | Controller/OpenAPI/functional docs çapraz kontrolü olmadan code üretme. |
| Product list DTO | `GET /api/products` optimized projection kullanıyor fakat `PagedResult<ProductDto>` ile variants/tags taşır; summary/main image contractı yok. | Kullanıcı küçük admin summary + main image eklenmesini onayladı. Backend ve docs güncellenene kadar kullanma; N+1 yapma. |
| Public product cache/scope | Aynı product list/detail controller'ı public ve 30 saniyelik output cache kullanıyor; admin için ayrı endpoint yok. | Admin freshness ve unpublished-data kapsamı backend ile netleşmeden aggressive frontend cache ekleme. |
| Product route/SEO | Mevcut frontend `/product/[slug]`, canonical `/products/{slug}`; by-url/seo-index yalnız yeni source'ta, OpenAPI'de yok. | Storefront route stratejisini dondur; docs güncellemesi ve kullanıcı kararı bekle. |
| Phase 1 root route | Karar verildi: current app Admin Panel'dir; `/admin` prefix yoktur. | `/`, session durumuna göre `/login` veya `/dashboard`'a redirect olur; protected route ve login ters redirect kuralları uygulanır. |
| Product images | Yalnız URL tabanlı ProductImage CRUD var; upload/storage contractı yok. | Upload UI/provider icat etme; URL workflow kullan veya yeni contract iste. |
| Product list thumbnail | Product list DTO image dönmüyor. | Placeholder gerçek ürün görseli gibi sunma; N+1 yerine backend projection kararı iste. |
| Variant docs | Genel catalog create örneği `value` alanını atlıyor; ayrıntılı endpoint ve kaynak `name + value` zorunlu diyor. | Ayrıntılı endpoint/source modelini kullan; docs drift'i raporla. |
| Orders search/customer | Güncel Admin list yalnız status/date/page filter; summary'de customer yok. | Kullanıcı search/customer alanlarının backend'e eklenmesini onayladı. Güncellenmiş API/docs yayımlanana kadar UI'da kullanma. |
| Dashboard metrics | Genel dashboard aggregate endpointi yok. | Fake metric/chart gösterme; dashboard contractı gelene kadar entry/quick links ile sınırla. |
| Campaigns | Yalnız Coupon API belgeli; genel campaign modeli yok. | Campaigns parent planned; sadece Coupon capability gerçek sayfa olabilir. |
| Accounting Overview | Aggregate overview endpointi yok. | Disabled placeholder; report satırlarını sahte toplam kartlarına çevirmeme. |
| Settings | Genel Settings endpointi yok; ShippingMethod ve TaxRate ayrı. | Generic settings page oluşturma; kapsam kararı bekle. |
| Marketplace | Provider connection/sync/log contractı ve controller yok. | Tüm marketplace navigation future/disabled; fake data yok. |
| Accounting historical spec | Eski spec payment'ı invoice'a allocate eder ve bazı cancellation'ları milestone dışı sayar; güncel API CurrentAccountTransaction allocation ve cancel endpointlerini uygular. | Güncel `api-accounting-docs` + controller kullan; historical öneriyi frontend contractı sayma. |
| CurrentAccount search | Liste yalnız page/pageSize destekler. | Searchable selector uydurma; büyük veri UX'i için backend search contractı iste. |
| Environment/deployment | Final domain/hosting/API hostname ve bağımsız deployment ayrıntıları yoktur. Aynı-origin BFF, host-only HttpOnly cookie ve ayrı Admin/Storefront session'ı provisional olarak onaylandı. | Environment tabanlı, host-agnostic kal; final origin/CORS/CSRF/session store kararlarını varsayma. |
| Generated types dependency | `openapi-typescript` kurulu değil. | Kullanıcı kuruluma izin verdi; yalnız OpenAPI güncellendikten sonra ilgili implementation görevinde ekle. |
| Form validation library | Zod veya başka form/validation library kurulu değil. | Kullanıcı uygun dependency'ye izin verdi; gerçek form ihtiyacına göre en küçük seçimi implementation sırasında yap ve etkisini raporla. |
| Browser/a11y tests | Vitest var; Playwright Test ve axe kurulu değil. | Kullanıcı browser/a11y test dependency'lerine izin verdi. İzole test verisi ve credential kuralları sağlanmadan destructive/shared-state E2E çalıştırma. |
| Design foundation | Kalıcı palette, token seti, icon standardı ve logo yok. | Provisional nötr + ölçülü mavi token temeli ve sade icon yaklaşımı onaylandı; text wordmark dışına ve kalıcı marka iddiasına çıkma. |

### Kayda geçirilmiş kullanıcı kararları

1. Backend OpenAPI ve endpoint belgeleri frontend entegrasyonundan önce güncellenecek.
2. Product admin listesine summary + main image, Orders listesine search + customer sözleşmeleri backend tarafında eklenecek; frontend yalnız belgelenmiş yeni contractı kullanacak.
3. `openapi-typescript`, uygun form validation ve browser/accessibility test dependency'lerine izin verildi.
4. Planned/future sidebar grupları açılır olacak; alt öğeler görünür fakat uygulanana kadar disabled ve “Planlandı/Yakında” etiketli kalacak.
5. Provisional nötr + ölçülü mavi token temeli ve sade icon yaklaşımı onaylandı.
6. Aynı-origin BFF, host-only HttpOnly cookie, ayrı Admin/Storefront session'ları ve güvenli seed/test credential ilkeleri provisional mimari olarak kabul edildi.
7. `/admin` URL prefix kullanılmayacak; `/`, `/login`, `/dashboard`, `/products`, `/products/new` ve `/orders` redirect/route sözleşmesi onaylandı.

### Hâlâ çözülmemiş deployment kararları

- Final commercial product name ve kalıcı marka kimliği.
- Final domain, root/admin subdomain seçimi, API hostname ve hosting provider.
- Admin ve Storefront'un kesin deployment biçimi.
- Final BFF session storage mekanizması.
- Production için final cookie scope/SameSite ve cross-origin CORS/CSRF ayrıntıları.
- Gelecekte cross-subdomain SSO gerekip gerekmeyeceği.
- Production ilk administrator bootstrap operasyonunun kesin uygulaması.

Bu bilinmeyenler Phase 1 route ve UI foundation çalışmalarını engellemez. Ancak bir görev production güvenliği, session persistence, domain/cookie kapsamı, deployment veya kalıcı marka değeri gerektirirse tahminle ilerleme; ilgili sınırda dur ve kullanıcı onayı iste. Güncellenmesi onaylanan API contractları belgelerde görünmeden ilgili frontend alanlarını uygulama.
