# Admin Paneli Kimlik Doğrulama ve Yetkilendirme Değişiklik Raporu

**Uygulama:** `UI/admin`  
**Rapor tarihi:** 3 Ağustos 2026  
**Kapsam:** Yönetici girişi, oturum yönetimi, token yenileme, çıkış, Admin rolü kontrolü, korumalı route/veri/mutation sınırları ve doğrulama testleri

## 1. Yönetici özeti

Admin paneline yalnızca backend tarafından doğrulanmış, durumu aktif ve rolü Admin olan kullanıcıların erişebilmesi için server-first bir kimlik doğrulama yapısı kuruldu.

Yetkilendirme kararı tarayıcıda veya yalnızca route korumasında verilmez. Kullanıcı, access token ile ASP.NET API üzerindeki `/api/users/me` endpoint'inden doğrulanır. Dönen kullanıcı için aşağıdaki iki koşul birlikte aranır:

| Alan | Zorunlu değer | Anlamı |
| --- | ---: | --- |
| `role` | `2` | `UserRole.Admin` |
| `status` | `1` | `UserStatus.Active` |

Bu şartlardan biri sağlanmazsa panel oturumu oluşturulmaz. Customer rolündeki veya pasif durumdaki bir kullanıcı doğru e-posta ve parola ile backend login işleminden geçse bile Admin paneline alınmaz. Login işlemi sırasında backend'in oluşturduğu refresh oturumu iptal edilmeye çalışılır ve token'lar tarayıcıya yazılmaz.

Erişim kontrolü birden fazla katmanda uygulanır:

1. `proxy.ts`, korumalı URL'lerde yalnızca hızlı cookie varlığı kontrolü yapar.
2. Admin layout, shell'i oluşturmadan önce backend üzerinden Admin oturumunu doğrular.
3. Her korumalı sayfa kendi veri sınırında Admin oturumunu tekrar zorunlu tutar.
4. Her Server Action, form verisini işlemeden ve mutation başlatmadan önce Admin yetkisini doğrular.
5. Backend API, Bearer token doğrulaması ve `AdminOnly` politikasıyla nihai yetki merciidir.

Bu katmanlı yapı sayesinde Proxy'nin atlanması, doğrudan Server Action çağrılması veya sahte bir cookie gönderilmesi panel yetkisi kazandırmaz.

## 2. Kaynak sözleşmeler ve doğrulanan backend davranışı

Auth uygulamasından önce aşağıdaki kaynaklar karşılaştırıldı:

- [OpenAPI sözleşmesi](docs/api/api-project-docs/openapi-controller-contract.json)
- Auth endpoint ve workflow dokümanları: `docs/api/api-project-docs/`
- [AuthController](../API/src/ECommerce.API/Controllers/User/AuthController.cs)
- [UsersController](../API/src/ECommerce.API/Controllers/User/UsersController.cs)
- [AuthorizationPolicies](../API/src/ECommerce.API/Security/AuthorizationPolicies.cs)
- [UserRole](../API/src/ECommerce.Domain/Enums/User/UserRole.cs)
- [UserStatus](../API/src/ECommerce.Domain/Enums/User/UserStatus.cs)

Frontend'in kullandığı backend işlemleri:

| İşlem | Endpoint | Frontend amacı |
| --- | --- | --- |
| Login | `POST /api/auth/login` | E-posta/parolayı backend'de doğrulamak ve token çiftini almak |
| Oturum doğrulama | `GET /api/users/me` | JWT decode'a güvenmeden kullanıcıyı, rolü ve durumu backend'den doğrulamak |
| Refresh | `POST /api/auth/refresh-token` | Access ve refresh token'ı birlikte döndürmek |
| Logout | `POST /api/auth/logout` | Refresh oturumunu backend'de iptal etmek |

Login ve refresh cevabında `user`, `accessToken`, `refreshToken` ve iki token'ın ayrı sona erme zamanları bulunur. Frontend cookie sürelerini tahmin etmez; backend'in verdiği kesin tarihler kullanılır.

### 2.1 OpenAPI sözleşme farkları

Sözleşme denetimi şu sonucu verdi:

- OpenAPI `3.0.4`
- `173` path
- `226` schema
- `32` numeric enum
- `7` sözleşme uyarısı

Bilinen farklar:

- OpenAPI'deki global Bearer güvenliği, runtime'da `[AllowAnonymous]` olan auth endpoint'lerine de uygulanmış görünüyor.
- Register `201`, logout/reset `204` ve forgot-password `202` başarı durumları eksik.
- Auth hata cevapları ve `ProblemDetails` şeması OpenAPI'de eksik.

Bu farklar nedeniyle auth endpoint'lerinin gerçek erişim davranışı controller kaynaklarından; wire tipleri OpenAPI'den; kullanıcı akışı Markdown dokümanlarından doğrulandı. API veya API dokümanları bu frontend çalışması kapsamında değiştirilmedi.

## 3. Kurulan mimari

Tarayıcı token'ları doğrudan yönetmez. Genel akış şöyledir:

1. Tarayıcı login formunu same-origin bir Next.js Server Action'a gönderir.
2. Server Action, ASP.NET API'ye server-to-server login isteği yapar.
3. Auth cevabı çalışma zamanında doğrulanır.
4. Kullanıcı `/api/users/me` ile yeniden doğrulanır.
5. Kullanıcı aktif Admin ise token'lar ayrı HttpOnly cookie'lere yazılır.
6. Server Component ve Server Action'lar backend çağrılarında access token'ı yalnızca sunucu tarafında Bearer header olarak kullanır.
7. Access token geçersizse ve refresh cookie varsa, kontrollü refresh akışı bir kez çalışır.
8. Refresh başarısızsa veya kullanıcı Admin değilse cookie'ler temizlenir.

Uygulama içindeki sorumluluk dağılımı:

| Katman | Sorumluluk |
| --- | --- |
| `src/app` | Route, layout, yönlendirme ve sayfa kompozisyonu |
| `src/modules/auth` | Login formu, login/logout Server Action'ları ve kullanıcıya gösterilen auth durumları |
| `src/lib/auth` | Cookie, sözleşme doğrulama, rol politikası, backend auth çağrıları ve session DAL |
| `src/lib/api` | Server-only API origin, Bearer ekleme, timeout, `no-store` ve güvenli hata normalizasyonu |
| `src/generated/api.ts` | OpenAPI'den üretilen wire tipleri |
| `src/proxy.ts` | Yalnızca optimistik cookie varlığı kontrolü ve güvenli yönlendirme |

## 4. Eklenen dosyalar ve amaçları

### 4.1 Auth çekirdeği

#### `admin/src/lib/auth/constants.ts`

- `ADMIN_ROLE = 2` ve `ACTIVE_USER_STATUS = 1` sabitlerini merkezi hale getirir.
- Development cookie adlarını `ecommerce_admin_access` ve `ecommerce_admin_refresh` olarak tanımlar.
- Production'da cookie adlarına `__Host-` öneki ekler.
- Korumalı route öneklerini `/dashboard`, `/products` ve `/orders` olarak tanımlar.

`__Host-` öneki production'da cookie'nin `Secure`, `Path=/` ve domainsiz host-only kullanımını browser seviyesinde güçlendirmek için seçildi.

#### `admin/src/lib/auth/policy.ts`

- Login formundaki e-posta ve parola alanlarını doğrular.
- E-posta için maksimum `320`, parola için maksimum `128` karakter sınırı uygular.
- Başarısız doğrulamada parola değerini action state'e geri koymaz.
- `returnTo` hedefini yalnızca uygulamaya ait korumalı, göreli admin route'larıyla sınırlar.
- `https://...`, `//...`, ters slash içeren veya auth döngüsü oluşturabilecek hedefleri `/dashboard` değerine düşürür.
- Cookie güvenlik seçeneklerini test edilebilir tek bir fonksiyonda üretir.
- Proxy'nin koruyacağı route'ları sınıflandırır.

Amaç, açık yönlendirme saldırısını önlemek, parola değerini hata dönüşlerinde taşımamak ve cookie politikasının tek kaynaktan yönetilmesini sağlamaktır.

#### `admin/src/lib/auth/contracts.ts`

- Auth tiplerini elle tekrar yazmak yerine `src/generated/api.ts` içindeki OpenAPI tiplerinden alias üretir.
- Login ve refresh cevabını çalışma zamanında doğrular.
- Boş token, eksik token, bozuk tarih ve süresi geçmiş token cevabını cookie yazılmadan önce reddeder.
- Kullanıcı DTO'sunda kimlik, e-posta, ad, soyad, rol, durum ve oluşturulma tarihi gibi yetki kararında kullanılan alanları doğrular.
- `assertActiveAdmin` ile yalnızca `role=2` ve `status=1` kombinasyonunu kabul eder.
- Server-side işlemlerin kullandığı `AdminSession` tipini tanımlar.

Amaç, TypeScript tiplerinin runtime verisini doğrulamadığı gerçeğine karşı auth cevabında fail-closed davranmaktır.

#### `admin/src/lib/auth/cookies.ts`

- `server-only` sınırında çalışır.
- Access ve refresh token'ları ayrı cookie'lere yazar.
- Cookie'leri yalnızca server-side okur ve temizler.
- Her token için backend'in verdiği ayrı expiry tarihini kullanır.
- Cookie yazma ve silme seçeneklerini eşleştirir.

Cookie politikası:

| Seçenek | Değer |
| --- | --- |
| `HttpOnly` | `true` |
| `SameSite` | `Lax` |
| `Path` | `/` |
| `Domain` | Tanımlı değil; host-only |
| `Secure` | Production'da `true` |
| `Priority` | `high` |
| Expiry | Backend token expiry değeri |

Amaç, token'ların `localStorage`, `sessionStorage`, React state'i veya browser JavaScript'i tarafından okunmasını engellemektir.

#### `admin/src/lib/auth/backend.ts`

- Login, refresh, logout ve `/users/me` backend çağrılarını tek server-only modülde toplar.
- Login sırasında `deviceName: "SERANTIS Admin"` gönderir.
- Refresh token rotasyonunda aynı Node.js process içindeki eşzamanlı aynı-token isteklerini tek Promise altında birleştirir.
- JWT payload decode ederek rol kararı vermek yerine `/users/me` çağrısını kullanır.
- Her auth cevabını runtime parser'dan geçirir.

Amaç, auth endpoint dizelerini ve kritik token işlemlerini UI bileşenlerinden ayırmak, paralel refresh yarışını aynı process içinde azaltmak ve backend'i kimlik otoritesi olarak korumaktır.

#### `admin/src/lib/auth/session.ts`

Bu dosya sistemin server-only oturum/DAL sınırıdır.

- `verifyAdminAccessToken`, access token'ı `/users/me` ile doğrular ve aktif Admin şartını uygular.
- `getVerifiedAdminSession`, aynı Server Component render geçişinde layout ve sayfanın tekrar eden doğrulamasını React `cache` ile istek kapsamında birleştirir.
- `requireAdminPageSession`, korumalı sayfalarda `401` ve `403` davranışlarını ayırır.
- `getOptionalAdminSession`, login ve kök route için oturumu zorunlu kılmadan yalnızca doğrulanmış Admin döndürür.
- `requireAdminActionSession`, mutation başlamadan önce Admin yetkisini doğrular; yalnızca `401` durumunda bir kez refresh yapar.
- `refreshAdminSession`, refresh token'ı döndürür, yeni kullanıcının rolünü tekrar kontrol eder, iki cookie'yi birlikte değiştirir ve yeni access token'ı `/users/me` ile tekrar doğrular.
- `revokeAndClearSession`, backend logout başarısız olsa bile cookie'leri `finally` davranışıyla temizler.
- Kesin `401`, `403` veya bozuk auth cevabında session temizlenir; geçici `5xx`/network hatasında kullanılabilir refresh oturumunun gereksiz yere silinmemesi hedeflenir.

Amaç, layout'a veya Proxy'ye tek başına güvenmeden her server veri/mutation sınırında aynı Admin politikasını uygulamaktır.

### 4.2 Auth kullanıcı arayüzü ve Server Action'lar

#### `admin/src/modules/auth/types.ts`

- Login formunun `idle/error` durumunu tanımlar.
- Güvenli mesaj, e-posta, field error ve opsiyonel `traceId` taşır.
- Parola alanı action state içinde bulunmaz.

#### `admin/src/modules/auth/actions.ts`

Üç Server Action eklenmiştir:

- `loginAction`
- `logoutAction`
- `clearRejectedSessionAction`

`loginAction` sırası:

1. Form verisini server-side doğrular.
2. Backend login endpoint'ini çağırır.
3. Auth cevabındaki rol ve durumu kontrol eder.
4. Access token'ı `/users/me` üzerinden tekrar doğrular.
5. Yalnızca aktif Admin doğrulamasından sonra cookie'leri yazar.
6. Güvenli `returnTo` hedefine yönlendirir.

Customer veya pasif kullanıcı login olduğunda backend'in oluşturduğu refresh token, browser'a yazılmadan önce logout endpoint'iyle iptal edilmeye çalışılır. İptal çağrısı başarısız olsa bile token browser'a verilmediği için panel session'ı oluşmaz.

Login hata eşlemesi:

| Backend/istemci durumu | Kullanıcı mesajı yaklaşımı |
| --- | --- |
| Form doğrulama | Alan bazlı hata; parola geri doldurulmaz |
| `401` | Genel “E-posta veya parola hatalı” mesajı; hesap varlığı ifşa edilmez |
| `403` | Panelin yalnızca aktif Admin hesaplarına açık olduğu bildirilir |
| `429` | Çok fazla deneme uyarısı gösterilir |
| `5xx`/network | Auth servisinin geçici olarak kullanılamadığı bildirilir |
| Beklenmeyen hata | Hassas detay içermeyen genel hata gösterilir |

`logoutAction`, backend oturumunu iptal etmeye çalışır, iki cookie'yi temizler ve `/login?reason=logged_out` adresine yönlendirir.

#### `admin/src/modules/auth/components/login-form.tsx`

- Etkileşimi gereken en küçük Client Component sınırı olarak tasarlandı.
- `useActionState` ile Server Action'a bağlanır.
- Gönderim sırasında butonu devre dışı bırakarak yinelenen login niyetini azaltır.
- Başarısız login sonrasında parola input'unu temizler.
- Hata kutusuna odak taşıyarak klavye ve ekran okuyucu akışını iyileştirir.
- Kalıcı label, `autocomplete="username"`, `autocomplete="current-password"`, `inputMode="email"`, `aria-invalid` ve `aria-describedby` kullanır.
- E-posta güvenli biçimde korunabilir; parola hiçbir zaman yeniden doldurulmaz.
- Server Action sonucunda yalnızca güvenli hata alanları render edilir.

Amaç, token veya backend origin'i Client Component'e aktarmadan erişilebilir bir login deneyimi sağlamaktır.

### 4.3 Auth route ve sayfaları

#### `admin/src/app/(auth)/layout.tsx`

- Login ve erişim reddi sayfalarını Admin shell'den ayırır.
- Auth route grubunu `noindex`, `nofollow` ve `nocache` olarak işaretler.

#### `admin/src/app/(auth)/login/page.tsx`

- Daha önce doğrulanmış aktif Admin session'ı varsa `/dashboard` adresine yönlendirir.
- Query içindeki `returnTo` değerini allowlist kontrolünden geçirir.
- Logout, session expiry, yetki reddi ve geçici doğrulama hatalarını hassas ayrıntı vermeyen mesajlara dönüştürür.
- Login formunu sade, Admin panelinden bağımsız bir yüzeyde gösterir.

#### `admin/src/app/(auth)/access-denied/page.tsx`

- Geçerli Admin session varsa dashboard'a döndürür.
- Admin olmayan veya reddedilmiş session'a hiçbir panel shell'i ya da panel verisi göstermez.
- Session temizliğini GET linki yerine POST tabanlı Server Action ile yapar.

#### `admin/src/app/api/auth/refresh/route.ts`

- Access cookie bulunmadığında fakat refresh cookie olduğunda navigation refresh sınırı olarak çalışır.
- Refresh işlemini yalnızca bir kez yapar.
- Yeni token çiftini ve Admin rolünü doğrular.
- `returnTo` hedefini korumalı same-origin route allowlist'iyle sınırlar.
- Başarılı veya başarısız redirect cevaplarına `Cache-Control: no-store` ve `Pragma: no-cache` ekler.
- `403`, `5xx` ve geçersiz/sona ermiş session durumlarını farklı güvenli login nedenlerine dönüştürür.

#### `admin/src/proxy.ts`

- `/dashboard`, `/products/**` ve `/orders/**` route'larında cookie varlığına göre erken yönlendirme yapar.
- Access cookie varsa isteği geçirir; bu davranış yetki onayı değildir.
- Yalnız refresh cookie varsa refresh route'una yönlendirir.
- Hiç session cookie'si yoksa güvenli `returnTo` ile login'e yönlendirir.
- Cookie içeriğini decode etmez ve rol kararı vermez.

Proxy yalnızca kullanıcı deneyimi ve gereksiz render'ı azaltmak içindir. Sahte bir access cookie Proxy'yi geçebilse bile layout/sayfa DAL kontrolünde backend tarafından reddedilir.

### 4.4 Kök route, panel layout ve shell değişiklikleri

#### `admin/src/app/page.tsx`

- Doğrulanmış Admin session varsa `/dashboard` adresine yönlendirir.
- Access token yok fakat refresh cookie varsa refresh akışına gider.
- Session yoksa `/login` adresine yönlendirir.

#### `admin/src/app/(admin)/layout.tsx`

- Admin shell render edilmeden önce `requireAdminPageSession` çağırır.
- Doğrulanmış kullanıcı bilgilerini shell'e geçirir.
- Admin route grubunu `noindex` ve `nofollow` yapar.

#### `admin/src/modules/admin-shell/components/admin-shell.tsx`

- Sahte veya sabit kullanıcı yerine doğrulanmış backend kullanıcısının ad/soyadını gösterir.
- Çıkış işlemini POST tabanlı Server Action formuna bağlar.
- Token veya access token içeren session nesnesini Client Component'e aktarmaz.

### 4.5 Korumalı sayfalar ve ürün işlemleri

Aşağıdaki sayfalar yalnız layout kontrolüne güvenmemek için kendi server veri sınırlarında `requireAdminPageSession` çağırır:

- `admin/src/app/(admin)/dashboard/page.tsx`
- `admin/src/app/(admin)/products/page.tsx`
- `admin/src/app/(admin)/products/new/page.tsx`
- `admin/src/app/(admin)/products/[productId]/page.tsx`

`admin/src/modules/products/api.ts` değişiklikleri:

- Her ürün API fonksiyonu doğrulanmış bir `AdminSession` alır.
- Access token yalnızca server-only API client'a geçirilir.
- API client Bearer header'ını sunucuda ekler.
- Ham cookie varlığı veya Client Component auth state'i ürün yetkisi olarak kabul edilmez.

`admin/src/modules/products/actions.ts` değişiklikleri:

- Ürün oluşturma ve güncelleme Server Action'ları formu parse etmeden ve mutation başlatmadan önce `requireAdminActionSession` çağırır.
- Access token `401` dönerse, mutation başlamadan önce en fazla bir refresh yapılır.
- `403` durumunda refresh denenmez.
- Yetkisiz doğrudan Server Action çağrısı güvenli form hatasına dönüşür ve backend mutation'ı başlatılmaz.

Amaç, kullanıcının UI'daki butona ulaşamamasını güvenlik saymamak; mutation endpoint'ini doğrudan çağırma girişimini de aynı rol kontrolünden geçirmektir.

### 4.6 Server-only API katmanı

#### `admin/src/lib/api/config.ts`

- API origin'ini `INTERNAL_API_BASE_URL`, ardından `API_BASE_URL` üzerinden okur.
- Yerel geliştirme varsayılanı `http://localhost:3300` değeridir.
- Yalnız `http` veya `https` protokolünü kabul eder.
- Production'da HTTPS olmayan API origin'ini reddeder.
- `server-only` olduğu için API origin'i browser bundle'ına aktarılmaz.

#### `admin/src/lib/api/client.ts`

- Tüm backend çağrılarını tek server-only sınırda toplar.
- Yalnız uygulamaya ait `/...` göreli path kabul eder; `//...` ve mutlak harici URL reddedilir.
- Çağıranın doğrudan `Authorization` header sağlamasını reddeder; Bearer sadece server-only `accessToken` seçeneğinden eklenir.
- Tüm özel/auth isteklerini `cache: "no-store"` ile gönderir.
- Redirect'i otomatik takip etmez.
- `12` saniye timeout uygular.
- `204`, JSON, non-JSON, network ve timeout durumlarını güvenli şekilde ayırır.

#### `admin/src/lib/api/problem.ts`

- Backend ProblemDetails cevabını tek bir `ApiError` modeline dönüştürür.
- Yalnız güvenli `title`, `status`, `detail`, `code`, `traceId` ve doğrulanmış field error alanlarını taşır.
- Stack trace, token veya dahili upstream URL istemci durumuna eklenmez.

### 4.7 Üretilen tipler ve paket değişiklikleri

#### `admin/src/generated/api.ts`

- `docs/api/api-project-docs/openapi-controller-contract.json` dosyasından `openapi-typescript` ile üretildi.
- Auth DTO'ları ve diğer wire modelleri için tip kaynağıdır.
- Elle düzenlenmemelidir.

#### `admin/package.json`

Eklenen scriptler:

- `pnpm api:types`
- `pnpm api:types:check`

Eklenen paketler:

| Paket | Tür | Amaç |
| --- | --- | --- |
| `server-only` | dependency | Auth/API modüllerinin Client Component graph'ına yanlışlıkla alınmasını build aşamasında engellemek |
| `openapi-typescript` | devDependency | OpenAPI wire tiplerini üretmek ve drift kontrolü yapmak |

`UI/pnpm-lock.yaml` bu paketlerin çözülmüş sürümleriyle güncellendi.

#### `admin/src/lib/site-config.ts`

- API origin'i genel site config'ten kaldırıldı.
- API adresinin tarayıcıya taşınma riski azaltıldı.
- Admin uygulaması yerel origin varsayılanı `http://localhost:3001` olarak ayrıldı.

## 5. Ayrıntılı akışlar

### 5.1 Başarılı Admin login akışı

1. Kullanıcı `/login` formunu gönderir.
2. E-posta/parola server-side doğrulanır.
3. Parola yalnızca server-to-server login isteğinde kullanılır.
4. Backend `AuthResultDto` döndürür.
5. Token'ların dolu, tarihlerin geçerli ve gelecekte olduğu doğrulanır.
6. Login cevabındaki kullanıcı için aktif Admin kontrolü yapılır.
7. Access token ile `/api/users/me` çağrılır.
8. `/users/me` sonucu tekrar aktif Admin kontrolünden geçer.
9. Access ve refresh token ayrı HttpOnly cookie'lere yazılır.
10. Kullanıcı doğrulanmış güvenli `returnTo` hedefine yönlendirilir.

Cookie yazma işlemi 8. adımdan önce yapılmadığı için Customer veya pasif kullanıcı tarayıcısında panel token'ı oluşmaz.

### 5.2 Admin olmayan kullanıcının login akışı

1. Backend doğru credential nedeniyle bir token çifti döndürebilir.
2. Frontend `role !== 2` veya `status !== 1` sonucunu `403` olarak değerlendirir.
3. Backend'in oluşturduğu refresh token logout endpoint'iyle iptal edilmeye çalışılır.
4. Token çifti cookie'ye yazılmaz.
5. Kullanıcıya panelin yalnızca aktif Admin hesaplarına açık olduğu bildirilir.

Bu akışta kullanıcı authentication açısından geçerli olabilir fakat authorization açısından panel için geçersizdir.

### 5.3 Korumalı sayfaya misafir erişimi

1. Proxy cookie olmadığını görür.
2. Hedefi `safeReturnTo` ile doğrular.
3. Kullanıcı `/login?returnTo=...&reason=session_required` adresine yönlendirilir.
4. Proxy atlanarak sayfa doğrudan render edilmeye çalışılırsa layout ve sayfa session DAL kontrolü tekrar çalışır.

### 5.4 Access token sona erdiğinde navigation refresh

1. Access cookie yok, refresh cookie varsa Proxy `/api/auth/refresh` route'una yönlendirir.
2. Route refresh token'ı backend'e gönderir.
3. Backend yeni access ve refresh token çifti döndürür.
4. Yeni kullanıcı aktif Admin olarak doğrulanır.
5. İki cookie birlikte değiştirilir.
6. Yeni access token `/users/me` ile tekrar doğrulanır.
7. Kullanıcı allowlist içindeki hedef route'a döner.

Refresh başarısızsa kesin geçersiz session cookie'leri temizlenir ve kullanıcı güvenli bir nedenle login'e yönlendirilir. Recursive refresh döngüsü oluşturulmaz.

### 5.5 Server Action sırasında access token sona ermesi

1. Server Action ilk satırlarda Admin session ister.
2. Mevcut access token `/users/me` çağrısında `401` dönerse refresh en fazla bir kez yapılır.
3. Yeni token çifti ve Admin rolü doğrulanır.
4. Ancak bundan sonra form parse/mutation akışı devam eder.
5. `403` veya refresh başarısızlığında mutation çalıştırılmaz.

Non-idempotent ürün mutation'ı auth katmanı tarafından kör biçimde otomatik tekrar edilmez. Refresh, mutation başlamadan önce tamamlanır.

### 5.6 Logout akışı

1. Kullanıcı POST tabanlı Server Action formunu gönderir.
2. Refresh token yalnızca sunucuda okunur.
3. Backend logout endpoint'iyle refresh session iptal edilmeye çalışılır.
4. Backend çağrısı başarılı veya başarısız olsa da iki yerel cookie temizlenir.
5. Kullanıcı login ekranına yönlendirilir.

## 6. Güvenlik kontrolleri

| Risk | Uygulanan kontrol |
| --- | --- |
| Token'ın JavaScript tarafından okunması | Access/refresh token ayrı `HttpOnly` cookie'lerde |
| Token'ın Client Component'e sızması | Auth/API dosyaları `server-only`; Client'a session/token prop geçilmiyor |
| API origin'inin browser'a sızması | `INTERNAL_API_BASE_URL` server-only config içinde; `NEXT_PUBLIC_*` kullanılmıyor |
| JWT payload rolüne kör güven | Rol `/api/users/me` üzerinden backend'de doğrulanıyor |
| Customer'ın panele girmesi | `role=2` ve `status=1` çift kontrolü; reddedilen login token'ı cookie'ye yazılmıyor |
| Proxy bypass | Layout, sayfa, API fonksiyonu ve Server Action sınırlarında tekrar yetkilendirme |
| Doğrudan Server Action çağrısı | Mutation öncesi `requireAdminActionSession` |
| Açık yönlendirme | `returnTo` yalnız korumalı relative same-origin route allowlist'i kabul ediyor |
| Refresh döngüsü | Refresh en fazla bir kez; başarısızlık login'e gider |
| Paralel refresh yarışı | Aynı process içindeki aynı refresh token istekleri tek Promise'e birleştiriliyor |
| Session'ın logout hatasında kalması | Cookie temizliği upstream logout sonucundan bağımsız çalışıyor |
| Bozuk auth cevabının kabul edilmesi | Runtime auth/user/token/expiry doğrulaması |
| Shared cache'te özel veri | Auth ve admin API istekleri `no-store` |
| Sahte Authorization header | API client doğrudan `Authorization` header verilmesini reddediyor |
| Harici upstream URL çağrısı | API client yalnız uygulama-owned göreli path kabul ediyor |
| Hesap varlığı ifşası | `401` login mesajı e-posta/parola ayrımı yapmıyor |
| Parolanın hata state'inde kalması | Parola action state'e eklenmiyor ve hata sonrası input temizleniyor |
| Production'da şifresiz API | HTTP API origin production'da reddediliyor |
| Admin sayfalarının indekslenmesi | Root/auth/admin metadata noindex; robots tüm uygulamayı disallow ediyor |

## 7. Testler ve doğrulama sonuçları

### 7.1 Birim testleri

Eklenen auth test dosyaları:

- `admin/src/lib/auth/policy.test.ts`
- `admin/src/lib/auth/contracts.test.ts`

Doğrulanan senaryolar:

- Korumalı admin route sınıflandırması.
- Harici origin, protocol-relative URL, ters slash ve auth route `returnTo` reddi.
- Login doğrulamasında parolanın hata sonucuna taşınmaması.
- Production cookie adlarında `__Host-` kullanımı.
- `HttpOnly`, `Secure`, `SameSite=Lax`, `Path=/` ve domainsiz cookie politikası.
- Geçerli auth cevabının kabulü.
- Eksik veya süresi geçmiş token cevabının reddi.
- Customer rolünün `403` ile reddedilmesi.

Son test sonucu:

- `6` test dosyası geçti.
- Toplam `24/24` test geçti.
- Auth dışındaki mevcut ürün ve shell testleri de regression kapsamında geçti.

### 7.2 Statik ve build kontrolleri

| Komut/kontrol | Sonuç |
| --- | --- |
| `pnpm lint` | Geçti |
| `pnpm typecheck` | Geçti |
| `pnpm test` | Geçti, `24/24` |
| `pnpm build` | Geçti |
| `pnpm api:types:check` | Geçti |
| `git diff --check` | Hata yok; yalnız mevcut LF/CRLF uyarıları |

Production build'de aşağıdaki dinamik auth/admin route'ları başarıyla üretildi:

- `/`
- `/login`
- `/access-denied`
- `/api/auth/refresh`
- `/dashboard`
- `/products`
- `/products/new`
- `/products/[productId]`

### 7.3 Runtime yönlendirme ve bypass testleri

Yerel Admin uygulaması `http://localhost:3001`, API `http://localhost:3300` üzerinden test edildi.

| Senaryo | Gözlenen sonuç |
| --- | --- |
| Misafir `/` | `/login` yönlendirmesi |
| Misafir `/dashboard` | `307` ile login ve güvenli `returnTo` |
| Misafir `/products` | Login yönlendirmesi |
| Misafir `/products/new` | Login yönlendirmesi |
| Cookie olmadan refresh route | `303` ile `session_expired` login sonucu |
| Sahte access cookie ile `/dashboard` | Panel açılmadı; login yönlendirmesi |
| Sahte access + refresh cookie | Panel açılmadı; refresh doğrulaması sonrası login |
| Sahte refresh cookie | `303` ile `session_expired` login sonucu |
| Backend `/api/users/me` yetkisiz çağrı | `401` |

### 7.4 Hassas veri sızıntısı kontrolü

Derlenmiş `.next/static` istemci paketleri şu değerler için tarandı:

- Development access cookie adı
- Development refresh cookie adı
- `http://localhost:3300`
- `INTERNAL_API_BASE_URL`
- Sentetik test access/refresh token değerleri

Sonuç: İstemci bundle'larında eşleşme bulunmadı.

Login HTML çıktısında JWT benzeri `eyJ`, `refreshToken` veya dahili API origin'i bulunmadı. `"use client"` dosyalarında access token, refresh token, cookie adı veya API origin kullanımı bulunmadı.

## 8. Değiştirilen mevcut uygulama davranışları

- Eski root sayfa içeriği session-aware yönlendirmeyle değiştirildi.
- Admin layout, doğrulanmış kullanıcı olmadan shell render etmeyecek hale getirildi.
- Shell'e doğrulanmış kullanıcı adı ve server-side logout eklendi.
- Ürün sayfaları ve ürün mutation'ları Admin session'a bağlandı.
- Admin uygulamasının tamamı noindex/nofollow olarak işaretlendi.
- `robots.ts`, tüm Admin uygulamasını crawler erişimine kapatacak şekilde güncellendi.
- Genel `siteConfig` içindeki API URL kaldırılarak server-only API config oluşturuldu.
- OpenAPI tip üretim ve drift kontrol scriptleri eklendi.

Storefront, API kaynak kodu, API dokümanları, veritabanı veya migration dosyaları bu auth uygulaması kapsamında değiştirilmedi.

## 9. Production yapılandırması

Production deploy öncesinde en az aşağıdaki ortam değerleri açıkça ayarlanmalıdır:

| Değişken | Beklenti |
| --- | --- |
| `INTERNAL_API_BASE_URL` | Admin Next.js sunucusunun erişebildiği HTTPS API origin'i |
| `SITE_URL` | Gerçek Admin uygulaması HTTPS origin'i |
| `SITE_NAME` | Kullanılacak geçici/nihai uygulama adı |
| `NODE_ENV` | Production deploy'da `production` |

Production'da HTTP API origin kullanılırsa uygulama bilinçli olarak hata verir. Production cookie'leri `Secure` ve `__Host-` olacağı için Admin uygulamasının da HTTPS üzerinden sunulması gerekir.

CORS, Admin rolü kontrolünün yerine geçmez. Browser backend API'ye doğrudan token ile çağrı yapmadığı için ana auth akışı `Browser -> same-origin Next.js Server Action/Route -> ASP.NET API` şeklindedir. Backend CORS ayarları yalnız izin verilen frontend origin'leriyle sınırlandırılmaya devam etmelidir.

## 10. Doğrulanamayan veya deployment kararı bekleyen noktalar

### 10.1 Gerçek kullanıcı bilgileriyle E2E

Gerçek Admin ve Customer test credential'ı verilmediği için aşağıdaki iki test gerçek hesapla çalıştırılmadı:

- Gerçek aktif Admin hesabıyla login, refresh ve logout uçtan uca akışı.
- Gerçek Customer hesabının doğru credential ile login olduktan sonra reddedilmesi ve backend refresh oturumunun iptal edildiğinin veri kaynağından doğrulanması.

Bu davranışlar sentetik birim testleri, kaynak incelemesi ve sahte-cookie runtime testleriyle doğrulandı; gerçek hesap testi ayrıca yapılmalıdır.

### 10.2 Browser ve ekran okuyucu doğrulaması

Çalışma sırasında browser kontrol aracı kullanılabilir bir browser bulamadığı için görsel, gerçek klavye ve gerçek ekran okuyucu testi yapılmadı. Form semantiği kaynak ve lint üzerinden kontrol edildi. Bu alan `not verified` durumundadır.

### 10.3 Çok instance refresh koordinasyonu

Paralel refresh birleştirme şu anda process içindeki `Map` ile yapılır. Tek instance veya aynı process içindeki isteklerde yarış azaltılır. Birden fazla Next.js instance/container kullanılan production topolojisinde iki refresh isteği farklı instance'lara düşebilir. Backend refresh reuse detection güvenli biçimde fail-closed davranır; ancak kullanıcı oturumunun kapanmasına neden olabilecek operasyonel bir yarış oluşabilir.

Çok instance deploy kararı verildiğinde merkezi session/refresh koordinasyonu, sticky routing veya deployment'a uygun başka bir tek-uçuş mekanizması ayrıca değerlendirilmelidir. Bu durum yetkisiz erişim sağlamaz; erişilebilirlik/oturum sürekliliği riskidir.

### 10.4 Navigation refresh Route Handler yöntemi

Navigation refresh şu anda `GET /api/auth/refresh` ile çalışır ve yalnız HttpOnly refresh cookie'yi kullanır. `returnTo` allowlist, Admin rol doğrulaması ve `no-store` mevcuttur; bu route bir kullanıcıyı Admin yapamaz ve panel verisini dış origin'e göndermez.

Bununla birlikte refresh token rotasyonu state-changing bir işlemdir. Nihai production güvenlik tasarımında POST tabanlı same-origin refresh köprüsü veya Next.js navigation gereksinimiyle uyumlu ek CSRF/intent koruması değerlendirilmelidir. Mevcut GET yaklaşımında ana risk yetki atlama değil, harici top-level navigation ile gereksiz refresh/oturum kesintisi oluşturulabilmesidir.

### 10.5 Dokümantasyon drift'i

OpenAPI'deki public auth security, başarı status'ları ve ProblemDetails eksikleri düzeltilmelidir. Frontend mevcut durumda controller/runtime davranışına göre güvenli çalışır; fakat sözleşme drift'i ileride generated client veya testlerde yanlış varsayıma neden olabilir.

## 11. Kabul kriterlerinin durumu

| Kriter | Durum |
| --- | --- |
| Misafir panel route'larına ulaşamaz | Doğrulandı |
| Sahte access/refresh cookie panel yetkisi vermez | Doğrulandı |
| Customer rolü Admin kontrolünden geçmez | Birim testte doğrulandı; gerçek credential testi bekliyor |
| Pasif Admin kabul edilmez | Kod politikasıyla uygulanıyor; gerçek credential testi bekliyor |
| Token browser JavaScript'ine açılmaz | Kaynak ve client bundle taramasıyla doğrulandı |
| Token local/session storage'a yazılmaz | Kaynak taramasıyla doğrulandı |
| Refresh iki token'ı birlikte döndürür | Uygulandı; gerçek credential runtime testi bekliyor |
| Logout upstream hata verse de cookie temizler | Kod akışı ve test edilebilir sınırla uygulandı |
| Server Action doğrudan çağrısı rol kontrolünden geçer | Uygulandı |
| Özel Admin verisi shared cache'e girmez | `no-store` ile uygulandı |
| Production build | Geçti |
| OpenAPI tip drift kontrolü | Geçti |
| Gerçek browser E2E ve ekran okuyucu | Not verified |

## 12. Bakım notları

- Yeni bir Admin route eklendiğinde route öneki gerekiyorsa `PROTECTED_ADMIN_PREFIXES` ve Proxy matcher birlikte güncellenmelidir.
- Proxy'ye rol kontrolü eklenmemeli; gerçek kontrol session DAL ve backend'de kalmalıdır.
- Yeni bir Server Action, mutation'dan önce `requireAdminActionSession` çağırmalıdır.
- Yeni bir korumalı Server Component veri çağrısı, doğrulanmış `AdminSession` almalıdır.
- Access veya refresh token hiçbir Client Component prop'una, URL'ye, log'a, analytics'e veya `NEXT_PUBLIC_*` değişkenine eklenmemelidir.
- Backend auth DTO'su değiştiğinde önce OpenAPI güncellenmeli, ardından `pnpm api:types` ve `pnpm api:types:check` çalıştırılmalıdır.
- Role/status enum değerleri backend sözleşmesi değişmeden frontend'de değiştirilmemelidir.
- Gerçek production domain/topoloji belirlendiğinde cookie, CSRF, reverse proxy ve çok-instance refresh kararları yeniden tehdit modeliyle incelenmelidir.

## 13. Sonuç

Mevcut yapı, Admin panel erişimini yalnız aktif Admin kullanıcılara açacak şekilde authentication ve authorization ayrımını uygular. Token'lar browser JavaScript'inden uzak tutulur; rol kararı backend'den doğrulanır; route, veri ve mutation sınırları ayrı ayrı korunur; refresh ve logout fail-closed davranacak şekilde tasarlanmıştır.

Otomatik testler, production build, OpenAPI tip kontrolü, sahte-cookie bypass denemeleri ve client bundle sızıntı taraması başarıyla tamamlandı. Gerçek Admin/Customer credential E2E testi, gerçek browser erişilebilirlik testi ve nihai çok-instance production topolojisi ayrı doğrulama gerektirir.
