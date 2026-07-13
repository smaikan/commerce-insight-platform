# E-Commerce Analytics

E-ticaret sektöründe edindiğim saha deneyimlerini yazılım mimarisiyle birleştirerek geliştirdiğim, kendi analiz altyapısına sahip profesyonel bir e-ticaret projesidir.

Projenin amacı yalnızca ürün, sepet ve sipariş kayıtlarını yöneten bir CRUD API oluşturmak değildir. Ürünlerin ve varyantların görüntülenme, sepete eklenme, satın alınma, favorilenme, puanlanma ve yorumlanma davranışlarını takip ederek işletmeye anlamlı veriler sunabilecek gerçekçi bir e-ticaret altyapısı geliştirmektir.

> Proje aktif geliştirme aşamasındadır. Domain, katalog CQRS akışları, Persistence ve güvenli kullanıcı/auth temelleri oluşturulmuştur. HTTP controllerları ve dışarı açılan iş endpointleri henüz eklenmemiştir.

## Projenin çıkış noktası

E-ticaret sistemlerinde yalnızca ürün ve sipariş kaydı tutmak yeterli değildir. Bir işletmenin şu sorulara da cevap verebilmesi gerekir:

- Hangi ürünler çok görüntüleniyor fakat satın alınmıyor?
- Hangi varyantlar sepete eklenmesine rağmen siparişe dönüşmüyor?
- Hangi ürün tipi, marka veya koleksiyon daha iyi performans gösteriyor?
- Favori sayısı yüksek ancak satış oranı düşük ürünler hangileri?
- Ürün ve varyant satışları günlere göre nasıl değişiyor?
- Stok hareketleri hangi işlem veya sipariş nedeniyle oluştu?
- Puan, yorum ve satış verileri birlikte değerlendirildiğinde hangi ürünler öne çıkıyor?

Bu proje, operasyonel e-ticaret verisini analiz edilebilir bir modele dönüştürmek üzere tasarlanıyor.

## Analiz yaklaşımı

Sistem her tıklama için ayrı bir event satırı oluşturmak yerine iki seviyeli bir metrik modeli kullanır.

### Ömür boyu özet sayaçları

Sık kullanılan toplam değerler hızlı okunabilmeleri için doğrudan ürün ve varyant üzerinde tutulur.

`Product` üzerinde:

- Tıklanma sayısı
- Toplam sepete eklenme sayısı
- Toplam satın alınma sayısı
- Favori sayısı
- Ortalama puan
- Puan sayısı
- Yorum sayısı

`ProductVariant` üzerinde:

- Sepete eklenme sayısı
- Satın alınma sayısı
- Güncel stok

### Günlük agregalar

Tarih bazlı raporlama için `ProductDailyMetric` ve `ProductVariantDailyMetric` tabloları kullanılır. Böylece her etkileşim için sınırsız event kaydı üretmeden günlük performans analizi yapılabilir.

Bu model ilerleyen aşamalarda şu analizleri destekleyecek şekilde geliştirilecektir:

- Görüntülenmeden sepete eklemeye dönüşüm oranı
- Sepetten satın almaya dönüşüm oranı
- Ürün ve varyant bazlı satış eğilimleri
- Marka, ürün tipi, koleksiyon ve etiket performansı
- Favori, puan, yorum ve satış ilişkisi
- Stok tükenme eğilimi ve stok hareket analizi
- Belirli tarih aralıklarında karşılaştırmalı performans

## Temel iş alanları

Domain modeli gerçek bir e-ticaret sisteminin ana kavramlarını içerir:

- Ürün, ürün tipi ve marka
- Ürün varyantları ve varyant bazlı stok
- Ürün görselleri
- Koleksiyonlar ve etiketler
- Günlük ürün ve varyant metrikleri
- Puanlar, yorumlar ve favoriler
- Paket ürünler
- Kullanıcı ve misafir sepetleri
- Sipariş ve sipariş kalemleri
- Ödeme kayıtları
- Teslimat ve fatura adresleri
- Kuponlar ve kupon kullanım geçmişi
- Stok hareketleri
- Kullanıcı, rol, hesap durumu ve güvenlik tokenları

Ana kategori kavramı olarak klasik `Category` yerine `ProductType` kullanılmaktadır. Her ürün bir ürün tipine bağlıdır; marka, koleksiyon ve etiket ilişkileri isteğe bağlıdır. Stok hiçbir zaman ürün üzerinde tutulmaz, satılabilir birim olan `ProductVariant` üzerinde yönetilir.

## Mimari

Proje Clean Architecture ve modüler monolith yaklaşımıyla geliştirilmektedir.

```text
HTTP Request
     |
     v
ECommerce.API
     |
     v
ECommerce.Application
     |
     v
ECommerce.Domain

ECommerce.Persistence ------> Application + Domain
ECommerce.Infrastructure ---> Application
```

Katman sorumlulukları:

| Katman | Sorumluluk |
|---|---|
| `ECommerce.Domain` | Entityler, enumlar, iş kuralları ve değişmezler |
| `ECommerce.Application` | CQRS command/query akışları, handlerlar, validatorlar, DTO'lar ve arayüzler |
| `ECommerce.Persistence` | EF Core, SQL Server, DbContext, configuration, migration ve repository implementasyonları |
| `ECommerce.Infrastructure` | JWT, parola hashleme, token üretme ve diğer dış teknik servisler |
| `ECommerce.API` | Controllerlar, middleware, authentication, authorization, Swagger ve DI bağlantıları |

Bağımlılıklar içeriye doğru ilerler. Domain hiçbir üst katmana bağlı değildir. Application, EF Core veya `AppDbContext` kullanmaz. API katmanında iş kuralı tutulmaz.

## Kullanılan teknolojiler

- .NET 10
- ASP.NET Core Web API
- Controller-based API
- Clean Architecture
- Modular Monolith
- Entity Framework Core
- SQL Server
- CQRS ve MediatR
- FluentValidation
- JWT Bearer Authentication
- Role-based Authorization
- Serilog
- Swagger / OpenAPI
- xUnit
- FluentAssertions
- Moq
- WebApplicationFactory

## Mevcut geliştirme durumu

| Alan | Durum |
|---|---|
| Clean Architecture çözüm yapısı | Hazır |
| Domain e-ticaret modeli | Büyük ölçüde hazır |
| EF Core configuration ve DbContext | Hazır |
| İlk migration ve User/Auth migrationı | Hazır |
| Ürün, varyant ve görsel CQRS akışları | Hazır |
| ProductType, Brand, Collection ve Tag CQRS akışları | Hazır |
| JWT, parola hashleme ve güvenlik token altyapısı | Hazır |
| Auth command akışları | Controller dışı katmanlarda hazır |
| API controllerları ve HTTP endpointleri | Henüz eklenmedi |
| Sepet, sipariş, ödeme ve kupon use-case'leri | Planlandı |
| Analiz sorguları ve raporlama algoritmaları | Planlandı |
| Production operasyon özellikleri | Planlandı |

Katalog tarafında tekli ve toplu ürün oluşturma, güncelleme, durum/aktiflik/öne çıkarma yönetimi, ürün sorguları, varyant fiyat ve stok yönetimi ile görsel işlemleri bulunmaktadır.

Auth tarafında kullanıcı kaydı, giriş, refresh token rotasyonu, çıkış, e-posta doğrulama ve parola sıfırlama command akışları oluşturulmuştur.

Ayrıntılı geliştirme ve inceleme notları için [proje ilerlemesi notlarım.md](<proje ilerlemesi notlarım.md>) dosyasına bakabilirsiniz.

## Güvenlik yaklaşımı

- Parolalar Domain içinde işlenmez; hazırlanmış hash Domain'e verilir.
- Parola hashleme PBKDF2-SHA256 ve rastgele salt ile yapılır.
- JWT access token üretimi Infrastructure katmanında tutulur.
- Refresh token ve güvenlik tokenlarının ham değerleri veritabanında saklanmaz.
- Token hashleri saklanır; ham token yalnızca üretildiği anda kullanılır.
- E-posta doğrulama ve parola sıfırlama tokenları tek kullanımlık ve sürelidir.
- Geçici giriş kilidi `AccessFailedCount` ve `LockoutEndAt` ile yönetilir.
- Kullanıcı yaşam döngüsü durumu geçici lockout durumundan ayrı tutulur.
- Rol bilgisi JWT claimlerine eklenir.
- JWT secret kaynak kodda tutulmaz ve en az 32 byte olmalıdır.

## Proje yapısı

```text
ECommerce.sln
src/
  ECommerce.API/
  ECommerce.Application/
  ECommerce.Domain/
  ECommerce.Persistence/
  ECommerce.Infrastructure/
tests/
  ECommerce.UnitTests/
  ECommerce.IntegrationTests/
```

Application katmanı özellik bazlı klasörlenir:

```text
Feature/
  Commands/
    UseCase/
      Command.cs
      CommandHandler.cs
      CommandValidator.cs
  Queries/
    UseCase/
      Query.cs
      QueryHandler.cs
      QueryValidator.cs
  Dtos/
```

## Gereksinimler

- .NET 10 SDK
- SQL Server veya geliştirme için SQL Server LocalDB
- EF Core CLI aracı

Kurulu SDK'yı kontrol etmek için:

```powershell
dotnet --version
```

EF Core aracını kurmak veya güncellemek için:

```powershell
dotnet tool install --global dotnet-ef
```

Araç zaten kuruluysa:

```powershell
dotnet tool update --global dotnet-ef
```

## Yerel geliştirme kurulumu

### 1. Projeyi geri yükleyin

```powershell
dotnet restore ECommerce.sln
```

### 2. Yapılandırmayı hazırlayın

Development ortamında varsayılan bağlantı SQL Server LocalDB üzerindeki `ECommerceDb` veritabanını kullanır.

JWT secret değerini kaynak koda veya `appsettings.json` dosyasına yazmayın. Development için Secret Manager kullanabilirsiniz:

```powershell
dotnet user-secrets init --project src\ECommerce.API\ECommerce.API.csproj
dotnet user-secrets set "Jwt:SecretKey" "en-az-32-byte-uzunlugunda-guvenli-bir-secret" --project src\ECommerce.API\ECommerce.API.csproj
```

Farklı bir SQL Server bağlantısı kullanılacaksa:

```powershell
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=...;Database=ECommerceDb;..." --project src\ECommerce.API\ECommerce.API.csproj
```

Production ortamında secret ve bağlantı bilgilerini güvenli bir secret store üzerinden sağlayın.

### 3. Migrationları uygulayın

```powershell
dotnet ef database update --project src\ECommerce.Persistence\ECommerce.Persistence.csproj --startup-project src\ECommerce.API\ECommerce.API.csproj
```

### 4. Projeyi derleyin

```powershell
dotnet build ECommerce.sln
```

### 5. API'yi çalıştırın

```powershell
dotnet run --project src\ECommerce.API\ECommerce.API.csproj
```

Development ortamında Swagger arayüzü uygulamanın yayınladığı adresin `/swagger` yolunda açılır.

> Henüz controller eklenmediği için Swagger arayüzünde iş endpointleri görünmeyecektir.

## Testler

Tüm testleri çalıştırmak için:

```powershell
dotnet test ECommerce.sln
```

Yalnızca unit testleri çalıştırmak için:

```powershell
dotnet test tests\ECommerce.UnitTests\ECommerce.UnitTests.csproj
```

Yalnızca integration testlerini çalıştırmak için:

```powershell
dotnet test tests\ECommerce.IntegrationTests\ECommerce.IntegrationTests.csproj
```

Proje aktif geliştirme aşamasında olduğu için test durumu her zaman tamamen yeşil olmayabilir. Bilinen sorunlar ve öncelikler ilerleme notlarında tutulur.

## Migration işlemleri

Yeni migration oluşturmak için:

```powershell
dotnet ef migrations add MigrationName --project src\ECommerce.Persistence\ECommerce.Persistence.csproj --startup-project src\ECommerce.API\ECommerce.API.csproj --output-dir Migrations
```

Migration listesini görmek için:

```powershell
dotnet ef migrations list --project src\ECommerce.Persistence\ECommerce.Persistence.csproj --startup-project src\ECommerce.API\ECommerce.API.csproj
```

## API geliştirme kuralları

- İş endpointleri Minimal API ile yazılmaz.
- Controllerlar `[ApiController]` ve `[Route("api/[controller]")]` kullanır.
- Controllerlar yalnızca HTTP isteğini alır, Application katmanını çağırır ve HTTP cevabı döner.
- Business logic controller içine yazılmaz.
- Request/response modelleri persistence entitylerinden ayrılır.
- Validation Application katmanında FluentValidation ile yürütülür.
- Hatalar merkezi `ProblemDetails` sözleşmesine dönüştürülecektir.
- Authorization controller veya action sınırında açıkça uygulanacaktır.

## Yol haritası

Kısa vadeli teknik hedefler:

1. Auth zaman yönetimi ve mevcut test sorunlarının düzeltilmesi
2. Parola değişiminde aktif refresh tokenların iptal edilmesi
3. Paket güvenlik uyarılarının kapatılması
4. Stok ve token işlemlerine concurrency koruması eklenmesi
5. Stok güncellemelerinin `InventoryTransaction` ile atomik hale getirilmesi
6. Merkezi exception handling ve `ProblemDetails`
7. Auth ve katalog controllerlarının eklenmesi
8. Pagination, filtreleme ve sıralama desteği
9. Gerçek HTTP integration testleri

İş özelliği hedefleri:

1. Sepet yönetimi
2. Sipariş oluşturma ve transaction içinde stok azaltma
3. Ödeme akışları
4. Adres ve kupon yönetimi
5. Favori, puan ve yorum akışları
6. Günlük metrik güncelleme servisleri
7. Ürün ve varyant dönüşüm analizleri
8. Tarih aralığı, ürün tipi, marka, koleksiyon ve etiket bazlı raporlar
9. Yönetim paneli ve raporlama ekranlarını destekleyen API sözleşmeleri

## Projenin hedefi

Bu proje tamamlandığında:

- Gerçek e-ticaret iş kurallarını koruyan,
- Güvenli kullanıcı ve yetkilendirme altyapısına sahip,
- Sipariş, ödeme ve stok işlemlerini tutarlı yöneten,
- Ürün performansını yalnızca satış üzerinden değil bütün müşteri davranışlarıyla değerlendiren,
- Günlük ve ömür boyu metriklerden anlamlı analizler çıkarabilen,
- Test edilebilir, sürdürülebilir ve genişletilebilir

bir e-ticaret platformu altyapısı sunmayı hedeflemektedir.

## Lisans

Bu proje kişisel geliştirme ve portföy çalışması olarak hazırlanmaktadır. Lisanslama koşulları proje olgunlaştığında ayrıca belirtilecektir.
