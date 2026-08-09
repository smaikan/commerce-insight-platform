# Admin test ve performans rehberi

Bu rehber yalnızca izole edilmiş test ortamında çalıştırılır. Üretim hesabı, gerçek müşteri verisi, token, parola, adres veya ödeme verisi rapora ya da depoya yazılmaz.

## Öncelik sırası

1. Admin'i production modda çalıştırın: `pnpm build` ve `pnpm start`.
2. Aynı build, veri kümesi ve cihaz/ağ koşullarıyla `/orders` tarayıcı ölçümünü en az üç kez çalıştırın; medyanı karşılaştırın.
3. API yük testiyle `GET /api/orders` p50/p95/p99, hata oranı ve istek/saniye değerlerini kaydedin.
4. SQL Server Query Store veya execution plan ile en pahalı sorguyu doğrulayın. İndeks ya da arama mimarisi ancak bu kanıttan sonra değiştirilir.
5. Kod düzeyindeki değişikliklerde BenchmarkDotNet sonucu karşılaştırılır.

## Tarayıcı ölçümü

Önce bir production admin uygulamasını ve API'yi çalıştırın. Ardından yalnızca yerel/izole test hesabını environment variable ile verin:

```powershell
$env:ADMIN_BASE_URL = "http://localhost:3001"
$env:ALLOW_DEV_PERFORMANCE_TEST = "false"
$env:ADMIN_TEST_EMAIL = "test-admin@example.test"
$env:ADMIN_TEST_PASSWORD = "<secret>"
pnpm bench:orders:browser
```

Test, `/orders` navigasyonu için `responseStart`, DOMContentLoaded, load süresi ve transfer boyutunu `test-results` içine JSON eki olarak bırakır. Playwright tarayıcısı ilk kullanımda şu komutla kurulabilir:

```powershell
pnpm exec playwright install chromium
```

Bu test bir laboratuvar ölçümüdür; Core Web Vitals veya gerçek kullanıcı sonucu değildir. `next dev` varsayılan olarak reddedilir.

## API yük testi

[k6](https://grafana.com/docs/k6/latest/set-up/install-k6/) sistemde kurulu olmalıdır. Test, login ile tek bir geçici access token alır ve `GET /api/orders?PageNumber=1&PageSize=20` isteğini 5 sanal kullanıcıya kadar artırır.

```powershell
$env:API_BASE_URL = "http://localhost:3300"
$env:API_ADMIN_EMAIL = "test-admin@example.test"
$env:API_ADMIN_PASSWORD = "<secret>"
$env:ORDER_P95_MS = "1000"
k6 run admin/benchmarks/orders.k6.js
```

Raporlanacak değerler: `orders_list_duration` p50/p95/p99, `orders_list_failures`, istek/saniye, HTTP hata kodları, API CPU/bellek ve SQL logical reads. Eşik varsayılanı p95 `< 1000 ms`dir; hedef, test ortamının kapasitesine göre bilinçli biçimde değiştirilmelidir.

## .NET mikro-benchmark

API içindeki BenchmarkDotNet paketi CPU ve allocation regresyonlarını yakalar; SQL Server planı ya da ağ gecikmesini ölçmez.

```powershell
dotnet run -c Release --project ../API/benchmarks/ECommerce.Benchmarks -- --filter "*OrderList*"
```

Sonuçlar `API/BenchmarkDotNet.Artifacts/results` altında oluşur. Aynı donanımda, boşta olan makinede ve Release modunda karşılaştırma yapılır.

## Düzenli test matrisi

| Katman | Amaç | Ne zaman |
| --- | --- | --- |
| Vitest | URL filtreleri, eşleyiciler, saf UI davranışı | Her değişiklikte |
| Playwright performans | Giriş yapılmış sipariş ekranı navigasyonu | Sipariş/auth/layout değişikliğinde |
| k6 | API kapasitesi ve p95/p99 gecikmesi | API/DB değişikliği ve sürüm öncesi |
| BenchmarkDotNet | Handler/repository CPU ve allocation regresyonu | Sorgu/projection değişikliğinde |
| SQL execution plan | İndeks, `Contains` araması, `Count + Skip` maliyeti | Gerçekçi veri hacminde |

Her kayıtta build SHA, route, test verisi büyüklüğü, cache durumu, cihaz, örnek sayısı ve ölçüm tipi (lab/field) yazılır. Field Core Web Vitals için ayrı RUM veya CrUX verisi gerekir.
