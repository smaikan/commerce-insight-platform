# Accounting Module Implementation Plan

## 1. Belgenin amacı ve durumu

Bu belge, `docs/accounting-module-spec.md` gereksinimlerini mevcut e-ticaret mimarisine zarar vermeden, küçük ve bağımsız doğrulanabilir milestone'lara ayırır.

Bu belge yalnızca uygulama planıdır:

- Hiçbir milestone uygulanmamıştır.
- Hiçbir migration oluşturulmamıştır.
- Core proje dosyaları değiştirilmemiştir.
- Her milestone ayrıca ve açıkça onaylanmadan başlatılamaz.
- Bir milestone tamamlandığında sonraki milestone otomatik olarak başlatılamaz.

## 2. Mutlak mimari ve muhasebe kuralları

1. `StockMovement`, projenin tek stok hareketi mekanizmasıdır.
2. `PurchaseInvoice` hiçbir koşulda `StockMovement` oluşturamaz.
3. `SalesInvoice` doğrudan `StockMovement` oluşturamaz.
4. Mevcut e-ticaret `Order` yalnız authenticated User/Cart checkout akışına aittir ve Accounting satışlarında kullanılmaz veya değiştirilmez.
5. `AccountingSalesOrder`, Accounting satış belgesidir; `CurrentAccountId` kullanır ve `UserId`, Cart veya e-ticaret Address gerektirmez.
6. `AccountingSalesOrderItem`, Accounting isteğinin doğrudan verdiği `ProductVariantId` ve quantity değerlerini kullanır.
7. Accounting stok çıkışı, mevcut ProductVariant/StockMovement altyapısında workflow-owned `StockMovementType.AccountingSale = 22` ile oluşturulur.
8. `StockMovement` üzerine AccountingSalesOrderId eklenmez; `AccountingSalesOrderStockMovement`, item ile oluşan movement'ı eşler.
9. `SalesInvoice` opsiyoneldir; tam olarak bir AccountingSalesOrder'a aittir ve bir AccountingSalesOrder en fazla bir SalesInvoice taşır.
10. AccountingSalesOrder posting tam olarak bir customer receivable etkisi oluşturur; SalesInvoice ikinci alacak etkisi oluşturamaz.
11. Harici e-fatura entegrasyonu sonraki bir kapsamdır ve internal SalesInvoice oluşturmadan ayrıdır.
12. Bu milestone tek örtük depo kullanır; Warehouse ve WarehouseId eklenmez.
13. `Product`, `ProductVariant`, `Warehouse`, `StockMovement` veya mevcut e-ticaret `Order`/`OrderItem` yapıları Accounting altında kopyalanamaz.
14. Core proje değişiklikleri açık kullanıcı onayı gerektirir; AccountingSale enum/direction/reference/configuration değişiklikleri bu milestone için onaylanmıştır.
15. Accounting kodu mümkün olduğunca `Accounting` klasörleri ve namespace'leri altında izole kalmalıdır.
16. Her uygulama görevi yalnızca onaylanmış tek milestone kapsamını ele almalıdır.
17. Her milestone build, hedefli testler, diff incelemesi ve zorunlu durma noktasıyla bitmelidir.
18. Sonraki milestone kendiliğinden uygulanamaz.
19. Frontend tarafından hesaplanmış toplam, KDV, indirim, maliyet veya kâr değerlerine güvenilemez.
20. Posted belgeler normal update akışıyla değiştirilemez ve fiziksel olarak silinemez.
21. Posting işlemleri transaction, idempotency, concurrency ve audit kurallarını birlikte sağlamalıdır.
22. FIFO tüketimi, AccountingSalesOrderItem ile onun mapped AccountingSale StockMovement kaydına bağlanmalıdır.

## 3. Mevcut mimariden alınan entegrasyon sınırları

Plan aşağıdaki gerçek proje bulgularına göre hazırlanmıştır:

- Proje .NET 10, controller tabanlı ASP.NET Core API, Clean Architecture, CQRS + MediatR, EF Core ve SQL Server kullanır.
- Domain, Application, Persistence, Infrastructure ve API bağımlılık yönleri korunacaktır.
- Repository interface'leri Application; implementasyonları Persistence katmanında bulunur.
- `AppDbContext`, EF configuration sınıflarını assembly üzerinden otomatik tarar. Accounting entity'leri için core `DbSet` eklemek zorunlu değildir; Accounting repository'leri `Set<T>()` kullanabilir.
- `IUnitOfWork.ExecuteInSerializableTransactionAsync` her çağrıda yeni transaction açar. Revised Accounting Sales posting kendi tek serializable transaction'ını kullanır.
- Mevcut CreateOrder akışı yalnız oturumdaki kullanıcının Cart'ını kabul eder ve yalnız e-ticaret checkout için kalır.
- E-ticaret Order stok çıkışı `CreateOrderCommandHandler` içinden `ProductVariant.ApplyStockMovement(... StockMovementType.Sale ...)` çağrısıyla gerçekleşmeye devam eder; Accounting bu akışı çağırmaz.
- AccountingSalesOrder posting aynı ProductVariant/StockMovement altyapısını `AccountingSale = 22` ile kullanır ve hareketleri AccountingSalesOrderStockMovement üzerinden item'lara bağlar.
- Order `Pending` oluşturulur ve stok bu aşamada azalır. Pozitif tutarlı Order için süreli rezervasyon başlatılır.
- Pending/Confirmed Order iptali pozitif `Cancellation` hareketiyle stoğu geri yükler.
- Teslim edilmiş Order iadeleri ayrı Return workflow ile pozitif `SaleReturn` hareketi oluşturur.
- `Warehouse` ve ayrı `Supplier` mevcut değildir. Onaylı kapsam tek örtük depo ve WarehouseId'siz Accounting satış modelidir.
- `ProductVariant.Stock`, `StockMovement.QuantityDelta` ve `OrderItem.Quantity` tam sayıdır.
- `OrderItem` para alanları iki ondalıkla sınırlıdır; Order üzerinde para birimi bulunmaz.
- KDV oranları statik enum değil, dinamik `TaxRate` kayıtlarıdır.
- Mevcut Order `Payment`, provider ödeme denemesidir; kısmi veya çoklu fatura tahsisini desteklemez.
- `AuditableEntity` yalnız `CreatedAt` ve `UpdatedAt` taşır; Accounting belge aktörleri ayrıca modellenmelidir.
- Unit testlerde xUnit, FluentAssertions ve Moq; integration testlerde SQLite ve `WebApplicationFactory` kullanılır.
- SQL Server'a özgü concurrency davranışları yalnız SQLite ile yeterli doğrulanamaz.
- Mevcut çalışma ağacında `TaxRate.CalculateNetPrice` metodu silinmiş olmasına rağmen çağrıları durmaktadır. Bu Accounting dışı baseline derleme blokajıdır ve ayrı core onayı olmadan düzeltilmeyecektir.

## 4. Planlanan Accounting klasör sınırları

Beklenen ana yerleşim:

```text
src/ECommerce.Domain/Accounting/
  Common/
  PurchaseInvoices/
  SalesOrders/
  SalesInvoices/
  CostLayers/
  CurrentAccounts/
  Payments/
  CashAndBank/
  Expenses/

src/ECommerce.Application/Accounting/
  Common/
  PurchaseInvoices/
  SalesOrders/
  SalesInvoices/
  CostLayers/
  CurrentAccounts/
  Payments/
  CashAndBank/
  Expenses/
  Reports/

src/ECommerce.Persistence/Accounting/
  Configurations/
  Repositories/

src/ECommerce.API/Controllers/Accounting/

tests/ECommerce.UnitTests/Accounting/
tests/ECommerce.IntegrationTests/Accounting/
```

Namespace'ler fiziksel ayrımla uyumlu olmalıdır:

- `ECommerce.Domain.Accounting.*`
- `ECommerce.Application.Accounting.*`
- `ECommerce.Persistence.Accounting.*`
- `ECommerce.API.Controllers.Accounting`

## 5. Milestone çalışma protokolü

### 5.1. Başlangıç kapısı

Her milestone başlamadan önce:

1. Yalnız ilgili milestone için açık onay alınır.
2. Milestone'un unresolved decision listesi kontrol edilir.
3. Gerekli core değişikliği varsa ayrıca açık core değişiklik onayı alınır.
4. `git status --short` ile başlangıç çalışma ağacı kaydedilir.
5. Önceden var olan kullanıcı değişiklikleri ayrıştırılır ve korunur.
6. Başlangıç build sonucu kaydedilir. Mevcut baseline hata varsa milestone hatasıyla karıştırılmaz.

### 5.2. Uygulama sırası

Bir milestone içindeki katman sırası:

1. Domain
2. Application
3. Persistence
4. API
5. Unit test
6. Integration test
7. Review ve doğrulama

Controller'lar ince kalmalı; iş kuralları handler/domain/service katmanında bulunmalıdır. Application katmanı `AppDbContext` veya EF Core kullanmamalıdır.

Yeni veya değiştirilen her constructor, metot, local function ve override üzerinde `AGENTS.md` formatına uygun kısa Türkçe `//` yorum bulunmalıdır.

### 5.3. Her milestone için zorunlu kapanış komutları

Milestone'a özel test filtrelerine ek olarak aşağıdaki komutlar çalıştırılır:

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build
git diff --check
git status --short
git diff --stat
git diff --name-only
```

Migration milestone'u dışındaki milestone'larda ayrıca şu kontrol yapılır:

```powershell
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

Beklenen sonuç boş olmalıdır.

### 5.4. Zorunlu durma noktası

Her milestone sonunda:

- Oluşturulan dosyalar listelenir.
- Değiştirilen mevcut dosyalar listelenir.
- Her mevcut dosyanın neden değiştiği açıklanır.
- Build sonucu raporlanır.
- Unit ve integration test sonuçları raporlanır.
- Diff özeti raporlanır.
- Açık riskler ve ertelenen kararlar raporlanır.
- Çalışma durdurulur.
- Sonraki milestone için ayrıca onay beklenir.

## 6. Önerilen sıradaki mimari ayarlamalar

Yüksek seviye önerideki sıra aşağıdaki nedenlerle küçük parçalara ayrılmış ve kısmen değiştirilmiştir:

1. **CurrentAccount erken alındı:** PurchaseInvoice draft yapısı, aktif Supplier veya CustomerAndSupplier tipindeki `CurrentAccountId` doğrulanmadan tamamlanamaz; ayrı Supplier master kullanılmaz.
2. **Cari ledger temeli posting'den önce alındı:** PurchaseInvoice posting aynı transaction'da supplier debt; SalesInvoice posting customer receivable oluşturmalıdır. Cari hareket altyapısı olmadan posting eksik ve geçici bir uygulama olur.
3. **InventoryCostLayer ve CostHistory posting'den önce alındı:** PurchaseInvoice posting bu kayıtları atomik olarak oluşturmalıdır. Posting'i önce yazmak sonradan yeniden tasarım gerektirir.
4. **PurchaseInvoiceExpense ikiye ayrıldı:** Satın alma maliyetine dağıtılan giderler PurchaseInvoice posting'den önce hazır olmalıdır. Genel işletme giderleri daha sonraki ayrı milestone'da ele alınır.
5. **E-ticaret Order entegrasyonu kaldırıldı:** Mevcut Order yalnız User/Cart checkout'a aittir; AccountingSalesOrder ayrı Accounting aggregate'ıdır.
6. **Sales kapsamı tek milestone'da birleştirildi:** Draft işlemleri, optional SalesInvoice, AccountingSale StockMovement mapping, FIFO, kârlılık, customer receivable ve posting tek revised M11 kapsamında tamamlanır.
7. **İptal akışları iki milestone'a ayrıldı:** Purchase cancellation tüketilmiş CostLayer; Accounting sales cancellation ise ileride ayrıca onaylanacak AccountingSale stok ve cari reversal kararlarına bağlıdır.
8. **Cash/Bank, Accounting Payment'tan önce alındı:** Accounting ödeme ve tahsilatlarının finansal hesabı ve `FinancialTransaction` kaynağı olmadan kaydedilmesi geçici/eksik model üretir.
9. **Sales cost/profitability revised M11'e dahil edildi:** COGS yalnız gerçek CostLayerConsumption kayıtlarından hesaplanır ve aynı posting transaction'ında item/header sonuçlarına yazılır.
10. **Migration en sona bırakıldı:** Önce EF model ve davranışları SQLite/Unit testlerle doğrulanır; onaylı model için tek kontrollü migration fazı yürütülür.

## 7. Milestone planı

### M01 — Shared Accounting enums ve value object'ler

**Durum:** Kısmen tamamlandı (2026-07-26) — Onaylanan invoice enumları, `AccountingPrecision`, `CurrencyCode`, `ExchangeRate`, `InvoiceMoney`, `VatRateSnapshot` ve `AccountingSourceType` tamamlandı; `DiscountDefinition` ile henüz sahibi kesinleşmemiş account/cost status enumları sonraki onaylı milestone'lara bırakıldı.

**Kapsam**

- Accounting klasör ve namespace temelini oluşturmak.
- `InvoiceStatus`, `PriceEntryMode`, discount, source, account ve cost status enumlarını tanımlamak.
- `CurrencyCode`, `ExchangeRate`, `InvoiceMoney`, `DiscountDefinition`, `VatRateSnapshot` ve merkezi yuvarlama politikasını modellemek.
- Ortak precision sabitlerini tek yerde tanımlamak.

**Karşılanan spesifikasyon gereksinimleri**

- Modular Architecture.
- Document statuses.
- Discount input tanımları.
- VAT input mode.
- Decimal precision ve merkezi rounding.
- CurrencyCode/ExchangeRate doğrulaması.
- Source ilişkileri için ortak tipler.

**Kapsam dışı**

- Fatura entity'leri.
- Hesaplama motoru.
- Repository, controller ve migration.
- Warehouse veya core entity değişikliği.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/Common/Enums/InvoiceStatus.cs`
- `src/ECommerce.Domain/Accounting/Common/Enums/PriceEntryMode.cs`
- `src/ECommerce.Domain/Accounting/Common/Enums/DiscountScope.cs`
- `src/ECommerce.Domain/Accounting/Common/Enums/DiscountType.cs`
- `src/ECommerce.Domain/Accounting/Common/Enums/DiscountTaxBasis.cs`
- `src/ECommerce.Domain/Accounting/Common/Enums/DiscountUnitBasis.cs`
- `src/ECommerce.Domain/Accounting/Common/Enums/AccountingSourceType.cs`
- `src/ECommerce.Domain/Accounting/Common/ValueObjects/CurrencyCode.cs`
- `src/ECommerce.Domain/Accounting/Common/ValueObjects/ExchangeRate.cs`
- `src/ECommerce.Domain/Accounting/Common/ValueObjects/InvoiceMoney.cs`
- `src/ECommerce.Domain/Accounting/Common/ValueObjects/DiscountDefinition.cs`
- `src/ECommerce.Domain/Accounting/Common/ValueObjects/VatRateSnapshot.cs`
- `src/ECommerce.Domain/Accounting/Common/AccountingPrecision.cs`
- `tests/ECommerce.UnitTests/Accounting/Common/AccountingValueObjectTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- Yok.

**Core değişiklik ihtimali**

- Hayır.

**Kabul kriterleri**

- Geçersiz enum/value object durumları DomainException ile reddedilir.
- Para hesaplarında `decimal` dışında tür kullanılmaz.
- CurrencyCode kanonik ve doğrulanmış biçimde saklanır.
- ExchangeRate sıfırdan büyüktür.
- Rounding policy deterministiktir.
- Core entity kopyası oluşturulmaz.

**Unit testler**

- CurrencyCode normalization/invalid code.
- ExchangeRate positive boundary.
- Percentage discount 0–100 sınırı.
- Negatif/fazla fixed discount input reddi için value object temel kuralları.
- VAT oranı ve money scale.
- `MidpointRounding.AwayFromZero` davranışı.

**Integration testler**

- Yok; bu milestone saf Domain yapısıdır.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.Common"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- UnitOfMeasure'ın enum veya dinamik katalog olması henüz belirlenmemiştir.
- Para birimi doğrulamasının yalnız biçim mi yoksa ISO katalog kontrolü mü yapacağı kararsızdır.

**Çözümlenmemiş kararlar**

- Base currency.
- Desteklenen CurrencyCode listesi.
- Rounding scale'lerinin kesin değerleri.
- UnitOfMeasure modeli.

**Önceki milestone bağımlılığı**

- Yok.

---

### M02 — Merkezi invoice, discount, VAT ve rounding engine

**Durum:** Tamamlandı (2026-07-26) — Kullanıcı tarafından birlikte onaylanan M01+M02 görevindeki merkezi hesaplama motoru ve ilgili unit test kapsamı tamamlandı.

**Kapsam**

- Purchase ve Sales Invoice tarafından ortak kullanılacak saf hesap motorunu oluşturmak.
- KDV dahil/hariç fiyat dönüşümü.
- Line ve invoice-level discount hesapları.
- Percentage, FixedPerUnit, FixedLineTotal ve FixedInvoiceTotal.
- Purchase/Sale unit ile StockUnit discount basis.
- Header toplamlarını satırlardan yeniden üretmek.
- Son uygun satıra deterministik rounding farkı atamak.

**Karşılanan spesifikasyon gereksinimleri**

- Required header totals.
- Purchase-specific final cost hesap formüllerinin gider hariç temel kısmı.
- Discount system ve invoice discount distribution.
- VAT-inclusive/exclusive hesaplama.
- Centralized calculation engine.
- Money precision ve rounding.
- Frontend toplamlarına güvenmeme.

**Kapsam dışı**

- Entity persistence.
- Product/Supplier/Customer lookup.
- Expense allocation.
- CostLayer/FIFO/kârlılık.
- Controller.

**Beklenen yeni dosyalar**

- `src/ECommerce.Application/Accounting/Common/Calculations/IInvoiceCalculationService.cs`
- `src/ECommerce.Application/Accounting/Common/Calculations/InvoiceCalculationService.cs`
- `src/ECommerce.Application/Accounting/Common/Calculations/InvoiceCalculationInput.cs`
- `src/ECommerce.Application/Accounting/Common/Calculations/InvoiceLineCalculationInput.cs`
- `src/ECommerce.Application/Accounting/Common/Calculations/InvoiceCalculationResult.cs`
- `src/ECommerce.Application/Accounting/Common/Calculations/InvoiceLineCalculationResult.cs`
- `src/ECommerce.Application/Accounting/Common/Calculations/InvoiceTotals.cs`
- `tests/ECommerce.UnitTests/Accounting/Calculations/InvoiceCalculationServiceTests.cs`
- `tests/ECommerce.UnitTests/Accounting/Calculations/InvoiceDiscountDistributionTests.cs`
- `tests/ECommerce.UnitTests/Accounting/Calculations/InvoiceRoundingTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- Runtime DI istenirse `src/ECommerce.Application/ApplicationServiceRegistration.cs`; yalnız açık core/composition onayıyla.

**Core değişiklik ihtimali**

- Hesap motoru için hayır.
- Merkezi DI kaydı için düşük etkili composition-root değişikliği gerekebilir ve onay ister.

**Kabul kriterleri**

- API'ye verilecek raw input dışında hesaplanmış frontend değerleri kabul edilmez.
- Satır toplamları ve header toplamları birebir eşleşir.
- Invoice discount paylarının toplamı invoice discount değerine eşittir.
- Farklı KDV oranlı satırlar birlikte hesaplanabilir.
- Net tutar negatif olamaz.
- Sıfır eligible base üzerinde sabit/percentage dağıtım güvenli biçimde reddedilir.

**Unit testler**

- Spesifikasyon hesaplama testleri 30–43'ün tamamı.
- Overflow sınırları.
- Son eligible line rounding farkı.
- Discount uygulanmayan satır.
- Bir invoice içinde farklı VAT oranları.

**Integration testler**

- Yok; servis saf Application hesabıdır.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.Calculations"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Aynı input için her platformda aynı decimal/rounding sonucu korunmalıdır.
- Dört ondalıklı unit price ile iki ondalıklı total arasında kayıp farkları doğru dağıtılmalıdır.

**Çözümlenmemiş kararlar**

- Quantity ve UnitsPerUnit maksimum scale/range.
- Invoice discount eligible base'in KDV dahil veya hariç seçimi.
- Sıfır net tutarlı satır politikası.

**Önceki milestone bağımlılığı**

- M01.

---

### M03 — Tek CurrentAccount master verisi

**Durum:** Purchase Accounting kapsamında onaylı tasarım düzeltmesiyle tamamlandı (2026-07-26).

“CurrentAccount is the single customer/supplier master record. Basic identity, communication, tax, and address information are stored directly in CurrentAccount. Separate Supplier and CurrentAccountAddress entities are not used.”

**Kapsam**

- Customer, Supplier ve CustomerAndSupplier tiplerini tek CurrentAccount master kaydında tutmak.
- Kimlik, iletişim, vergi ve tek güncel adres alanlarını doğrudan CurrentAccount üzerinde saklamak.
- CurrentAccount create/update/list/detail/activation use case'leri ve benzersiz kod kuralı.

**Karşılanan spesifikasyon gereksinimleri**

- Supplier rolünün CurrentAccountType.Supplier ile sağlanması.
- PurchaseInvoice.CurrentAccountId referansının hazırlanması.
- CurrentAccount master verisinin fatura snapshot'larından ayrılması.

**Kapsam dışı**

- PurchaseInvoice.
- Cari hareket ve supplier debt.
- Payment/bank.
- Core User değişikliği.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/CurrentAccounts/CurrentAccount.cs`
- CurrentAccount commands, queries, DTOs, validators and handlers under the Accounting application layer.
- `src/ECommerce.Persistence/Accounting/Configurations/PurchaseAccountingConfigurations.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/PurchaseAccountingRepositories.cs`
- `src/ECommerce.API/Controllers/Accounting/CurrentAccountsController.cs`
- Purchase Accounting unit and integration tests.

**Değişmesi gerekebilecek mevcut dosyalar**

- `src/ECommerce.Persistence/PersistenceServiceRegistration.cs` veya buradan çağrılacak yeni Accounting registration extension'ı.
- Gerekirse `src/ECommerce.Application/ApplicationServiceRegistration.cs`.

**Core değişiklik ihtimali**

- CurrentAccount domain modeli için hayır.
- Composition root bağlantısı için düşük etkili değişiklik olabilir; açık onay gerekir.

**Kabul kriterleri**

- Ayrı Supplier veya Accounting address entity/table bulunmaz.
- Customer/User kopyalanmaz.
- CurrentAccount kimliği ve zorunlu alanları doğrulanır.
- Pasif veya supplier rolü taşımayan CurrentAccount yeni PurchaseInvoice seçimine kapalıdır.
- Controller yalnız MediatR'a delegasyon yapar.

**Unit testler**

- Zorunlu ad/unvan ve kimlik alanları.
- Normalize edilen benzersiz supplier code/vergi kimliği kararı uygulanırsa sınırlar.
- Aktivasyon yaşam döngüsü.

**Integration testler**

- Repository CRUD ve unique index.
- Controller auth/authorization.
- Sayfalama ve takip edilmeyen read query.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting.PurchaseInvoices"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Vergi numarası, ülke, adres ve iletişim alanlarının zorunluluk politikası iş kurallarıyla genişletilebilir.

**Çözümlenmemiş kararlar**

- Vergi numarası benzersizliği.
- CurrentAccount silme yerine pasifleştirme politikası.

**Önceki milestone bağımlılığı**

- M01.

---

### M04 — CurrentAccount ve immutable cari hareket temeli

**Durum:** Purchase Accounting kapsamı tamamlandı (2026-07-26) — Tek CurrentAccount master kaydı ve kaynak belge bazlı idempotent supplier debt hareketi uygulandı; customer receivable satış milestone'una bırakıldı.

**Kapsam**

- `CurrentAccount` ve `CurrentAccountTransaction` domain yapıları.
- Customer rolündeki hesabı gerektiğinde mevcut UserId'ye bağlamak; Supplier rolünü aynı CurrentAccount kaydında taşımak.
- Debit/Credit hareketini tek kaynak olarak tutmak.
- SourceType/SourceId tabanlı idempotent hareket append servisi.
- Posting milestone'larının kullanacağı internal repository/service sözleşmesi.

**Karşılanan spesifikasyon gereksinimleri**

- Current account types.
- Customer/Supplier debit-credit movements.
- Bakiyenin hareketlerden türetilmesi.
- Source ilişkileri ve duplicate accounting record önleme.

**Kapsam dışı**

- Public ekstre/borç/alacak raporları.
- Invoice posting entegrasyonu.
- Payments.
- Cache balance.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/CurrentAccounts/CurrentAccount.cs`
- `src/ECommerce.Domain/Accounting/CurrentAccounts/CurrentAccountTransaction.cs`
- `src/ECommerce.Domain/Accounting/CurrentAccounts/CurrentAccountType.cs`
- `src/ECommerce.Domain/Accounting/CurrentAccounts/CurrentAccountTransactionType.cs`
- `src/ECommerce.Application/Accounting/CurrentAccounts/Interfaces/ICurrentAccountRepository.cs`
- `src/ECommerce.Application/Accounting/CurrentAccounts/Services/ICurrentAccountTransactionService.cs`
- `src/ECommerce.Application/Accounting/CurrentAccounts/Services/CurrentAccountTransactionService.cs`
- `src/ECommerce.Persistence/Accounting/Configurations/CurrentAccountConfiguration.cs`
- `src/ECommerce.Persistence/Accounting/Configurations/CurrentAccountTransactionConfiguration.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/CurrentAccountRepository.cs`
- `tests/ECommerce.UnitTests/Accounting/CurrentAccounts/CurrentAccountTests.cs`
- `tests/ECommerce.UnitTests/Accounting/CurrentAccounts/CurrentAccountTransactionServiceTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/CurrentAccounts/CurrentAccountPersistenceTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- Accounting DI extension'ını bağlamak için persistence/application registration dosyaları.

**Core değişiklik ihtimali**

- Hayır; User yalnız ID ile referans edilir.

**Kabul kriterleri**

- Bakiye doğrudan keyfi set edilemez.
- Her cari hareket debit veya credit etkisini açık taşır.
- Aynı SourceType/SourceId/TransactionType ikinci kez eklenemez.
- Opsiyonel UserId yalnız mevcut kullanıcıya bağlanır; supplier rolü için ayrı Supplier FK kullanılmaz.
- Posting servisleri bu milestone sonrasında hareket ekleyebilecek sözleşmeye sahiptir.

**Unit testler**

- Debit/credit invariant.
- Negatif ve sıfır hareket reddi.
- Customer/Supplier/Both account type bağlantıları.
- Source idempotency.

**Integration testler**

- FK ve unique source index.
- Hareket toplamından bakiye projection.
- Aynı source için concurrent duplicate denemesi.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.CurrentAccounts"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting.CurrentAccounts"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Çoklu para birimli cari bakiye tek sayı olarak gösterilemez.
- User bağlantısında birden fazla CurrentAccount politikası belirlenmelidir.

**Çözümlenmemiş kararlar**

- Tek CurrentAccount altında hareketlerin currency bazlı raporlanma politikası.
- `Both` hesabının gerçek kullanım senaryosu.
- Cached balance kullanılıp kullanılmayacağı.

**Önceki milestone bağımlılığı**

- M01 ve M03.

---

### M05 — PurchaseInvoice draft ve hesaplanan satırlar

**Durum:** Tamamlandı (2026-07-26) — Draft create/update, ayrı satır yönetimi, list/detail, snapshot alanları ve merkezi motorla bütün header/line toplamları uygulandı.

**Kapsam**

- `PurchaseInvoice` ve `PurchaseInvoiceLine` aggregate'ı.
- Draft create/update/list/detail.
- Aktif Supplier veya CustomerAndSupplier CurrentAccount doğrulaması.
- ProductVariant bağlantısı ve ürün/SKU/barkod snapshot'ları.
- Merkezi hesap motoruyla satır/header toplamları.
- CreatedBy/UpdatedBy ve concurrency token.
- `CurrentAccountId + InvoiceNumber` benzersizliği.

**Karşılanan spesifikasyon gereksinimleri**

- PurchaseInvoice entity alanları.
- PurchaseInvoiceLine alanları.
- Draft yaşam döngüsü.
- Ortak ve purchase-specific total alanları.
- Audit temel alanları.
- Invoice number unique constraint.
- Validation ve API endpointlerinin draft bölümü.

**Kapsam dışı**

- StockMovement allocation.
- Posting/cancellation.
- CostLayer/history.
- Supplier debt.
- Payment/gider allocation.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/PurchaseInvoices/PurchaseInvoice.cs`
- `src/ECommerce.Domain/Accounting/PurchaseInvoices/PurchaseInvoiceLine.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Interfaces/IPurchaseInvoiceRepository.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Commands/CreatePurchaseInvoice/*`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Commands/UpdatePurchaseInvoice/*`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Queries/GetPurchaseInvoiceById/*`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Queries/GetPurchaseInvoices/*`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Dtos/*`
- `src/ECommerce.Persistence/Accounting/Configurations/PurchaseInvoiceConfiguration.cs`
- `src/ECommerce.Persistence/Accounting/Configurations/PurchaseInvoiceLineConfiguration.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/PurchaseInvoiceRepository.cs`
- `src/ECommerce.API/Controllers/Accounting/PurchaseInvoicesController.cs`
- `tests/ECommerce.UnitTests/Accounting/PurchaseInvoices/PurchaseInvoiceTests.cs`
- `tests/ECommerce.UnitTests/Accounting/PurchaseInvoices/PurchaseInvoiceCommandHandlerTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/PurchaseInvoices/PurchaseInvoicePersistenceTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/Api/PurchaseInvoicesControllerTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- Accounting DI extension registration çağrıları.

**Core değişiklik ihtimali**

- Hayır. ProductVariant ve CurrentAccount ID üzerinden okunur.

**Kabul kriterleri**

- Draft fatura en az bir satır içerir.
- API tüm hesaplanmış alanları kendisi üretir.
- Draft hiçbir StockMovement, CostLayer veya cari hareket oluşturmaz.
- Yalnız Draft normal update edilebilir.
- Aynı CurrentAccountId/InvoiceNumber ikinci kez kaydedilemez.
- ProductVariant snapshot değerleri güvenilir persisted kayıttan alınır.

**Unit testler**

- Draft status.
- Satır ekleme/güncelleme/çıkarma.
- En az bir satır.
- Duplicate variant line politikası.
- Header/line totals.
- Posted/Cancelled update reddi için domain temeli.

**Integration testler**

- CurrentAccount FK.
- ProductVariant FK.
- CurrentAccountId+InvoiceNumber unique constraint.
- Create/update/list/detail controller akışı.
- Draft işlemin StockMovements satır sayısını değiştirmemesi.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.PurchaseInvoices"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting.PurchaseInvoices"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- StockQuantity decimal hesaplanıp mevcut stok int olduğu için allocation sözleşmesi henüz kurulmamıştır.
- Snapshot alanlarının ne zaman dondurulacağı Draft create veya Posted aşaması olarak netleştirilmelidir.

**Çözümlenmemiş kararlar**

- Duplicate ProductVariant satır politikası.
- UnitOfMeasure modeli.
- Snapshot'ın create veya post anında yenilenmesi.
- Draft invoice number değişebilir mi.

**Önceki milestone bağımlılığı**

- M01, M02 ve M03.

---

### M06 — PurchaseInvoiceStockAllocation

**Durum:** Tamamlandı (2026-07-26) — Yalnız mevcut pozitif Purchase StockMovement kayıtları için partial/multiple allocation, variant kontrolü, aşırı ve çift maliyetlendirme koruması uygulandı; proje tek depo modeliyle çalıştığından ayrı warehouse alanı eklenmedi.

**Kapsam**

- PurchaseInvoiceLine ile mevcut pozitif StockMovement arasında many-to-many allocation.
- Kısmi allocation.
- Bir satırın birden fazla hareketten; bir hareketin birden fazla faturadan miktar alması.
- `AvailableForCostAllocation` hesabı.
- Uygun hareket listeleme query'si.
- Wrong variant ve over-allocation koruması.
- Serializable concurrency stratejisi.

**Karşılanan spesifikasyon gereksinimleri**

- Purchase invoice stock allocation.
- StockMovement'a alan eklememe.
- Kısmi ve çoklu eşleştirme.
- Double costing engeli.
- Eligible positive movement validation.
- Concurrency gereksinimi.
- Available stock movement endpoint'i.

**Kapsam dışı**

- PurchaseInvoice posting.
- CostLayer oluşturma.
- Stock miktarı değiştirme.
- StockMovement core değişikliği.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/PurchaseInvoices/PurchaseInvoiceStockAllocation.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Interfaces/IAccountingStockMovementReader.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Interfaces/IPurchaseInvoiceStockAllocationRepository.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Commands/SetPurchaseInvoiceAllocations/*`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Queries/GetAvailableStockMovements/*`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Dtos/AvailableStockMovementDto.cs`
- `src/ECommerce.Persistence/Accounting/Configurations/PurchaseInvoiceStockAllocationConfiguration.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/AccountingStockMovementReader.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/PurchaseInvoiceStockAllocationRepository.cs`
- `tests/ECommerce.UnitTests/Accounting/PurchaseInvoices/PurchaseInvoiceStockAllocationTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/PurchaseInvoices/PurchaseInvoiceStockAllocationPersistenceTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- Yok; mevcut `IStockMovementRepository` ve `StockMovement` değiştirilmemelidir.

**Core değişiklik ihtimali**

- Hayır.

**Kabul kriterleri**

- Allocation toplamı fatura satırının StockQuantity değerini aşamaz.
- Hareketin toplam allocation'ı pozitif QuantityDelta değerini aşamaz.
- Yanlış ProductVariant hareketi satıra bağlanamaz.
- Aynı line/movement çifti duplicate olmaz.
- Concurrent iki fatura aynı miktarı aşırı tahsis edemez.
- Hiçbir allocation işlemi StockMovement veya ProductVariant.Stock oluşturmaz/değiştirmez.

**Unit testler**

- Partial allocation.
- Multiple movements per line.
- Multiple invoices per movement.
- Wrong variant.
- Quantity boundary.
- Eligible type policy.

**Integration testler**

- Spesifikasyon purchase tests 3–8.
- Unique/check constraint.
- Serializable concurrent allocation.
- Available movement projection ve remaining quantity.
- Allocation sırasında StockMovements sayısı ve ProductVariant.Stock değişmezliği.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~PurchaseInvoiceStockAllocation"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~PurchaseInvoiceStockAllocation"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- SQLite gerçek SQL Server kilitleme davranışını kanıtlamaz.
- Allocation sum check yalnız basit check constraint ile yapılamaz; transaction/locking gerektirir.

**Çözümlenmemiş kararlar**

- Eligible StockMovementType listesi.
- OpeningBalance maliyetlendirme yöntemi.
- Warehouse tutarlılığı; mevcut Warehouse yoktur.
- StockQuantity'nin tam sayı olma zorunluluğu.

**Önceki milestone bağımlılığı**

- M05.

---

### M07 — InventoryCostLayer

**Durum:** Tamamlandı (2026-07-26) — CostLayer yalnız onaylı allocation miktarından ve KDV hariç final birim maliyetten üretiliyor; fiziksel stok kaynağı veya ikinci stok sistemi değildir.

**Kapsam**

- `InventoryCostLayer` entity ve repository'si.
- Allocation miktarı ve PurchaseInvoiceLine final cost değerinden layer hazırlama.
- OriginalQuantity, RemainingQuantity ve maliyet snapshot'ları.
- Deterministik açık layer sorgusu.
- Optimistic concurrency/unique source koruması.

**Karşılanan spesifikasyon gereksinimleri**

- CostLayer system.
- CostLayer'ın stok miktarı kaynağı olmaması.
- Purchase allocation ve StockMovement bağlantısı.
- VAT hariç/dahil maliyetlerin saklanması.
- RemainingQuantity invariant.

**Kapsam dışı**

- Purchase posting orchestration.
- FIFO tüketimi.
- ProductVariantCostHistory.
- Warehouse core modeli.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/CostLayers/InventoryCostLayer.cs`
- `src/ECommerce.Domain/Accounting/CostLayers/CostLayerStatus.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Interfaces/IInventoryCostLayerRepository.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Services/IInventoryCostLayerFactory.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Services/InventoryCostLayerFactory.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Queries/GetCostLayers/*`
- `src/ECommerce.Application/Accounting/CostLayers/Queries/GetCostLayersByVariant/*`
- `src/ECommerce.Persistence/Accounting/Configurations/InventoryCostLayerConfiguration.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/InventoryCostLayerRepository.cs`
- `tests/ECommerce.UnitTests/Accounting/CostLayers/InventoryCostLayerTests.cs`
- `tests/ECommerce.UnitTests/Accounting/CostLayers/InventoryCostLayerFactoryTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/CostLayers/InventoryCostLayerPersistenceTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- Yok.

**Core değişiklik ihtimali**

- Tek depo kabul edilirse hayır.
- Gerçek Warehouse istenirse ProductVariant/StockMovement stok modeli için ayrı ve yüksek etkili core milestone/onayı gerekir.

**Kabul kriterleri**

- Layer yalnız mevcut allocation ve StockMovement ID'lerine bağlanır.
- Layer oluşturulması stok miktarını değiştirmez.
- RemainingQuantity, 0 ile OriginalQuantity arasındadır.
- Primary valuation cost KDV hariç final unit cost'tur.
- Aynı allocation/source ikinci layer'ı oluşturamaz.
- Open layer sıralaması CostDate, CreatedAt, Id şeklindedir.

**Unit testler**

- Quantity/cost invariant.
- RemainingQuantity sınırı.
- Total/unit cost mutabakatı.
- Duplicate source koruması.

**Integration testler**

- FK/unique/check constraint.
- Open layer query sırası.
- CostLayer eklenirken ProductVariant.Stock ve StockMovement sayısı değişmezliği.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.CostLayers.Inventory"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting.CostLayers.Inventory"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Warehouse kararı entity alanlarını doğrudan etkiler.
- Kesirli StockQuantity mevcut int stok sistemiyle uyumsuzdur.

**Çözümlenmemiş kararlar**

- Tek depo veya gerçek multi-warehouse.
- Layer status değerleri.
- CostDate için StockMovement.CreatedAt, InvoiceDate veya posting tarihi.

**Önceki milestone bağımlılığı**

- M05 ve M06.

---

### M08 — ProductVariantCostHistory

**Durum:** Tamamlandı (2026-07-26) — Önceki aktif maliyet kaydını kapatan ve mevcut stok snapshot'ıyla yeni maliyet geçmişi açan akış uygulandı.

**Kapsam**

- Raporlama amaçlı varyant maliyet geçmişi.
- Önceki aktif kaydın ValidTo/ClosingStockQuantity değerini kapatmak.
- Yeni maliyet kaydında OpeningStockQuantity snapshot'ı.
- SourceType/SourceId bağlantısı.

**Karşılanan spesifikasyon gereksinimleri**

- ProductVariantCostHistory.
- Geçerlilik aralığı.
- Açılış/kapanış stok snapshot'ları.
- Primary cost source olmama kuralı.

**Kapsam dışı**

- ProductVariant üzerine current cost alanı eklemek.
- FIFO.
- Posting orchestration.
- Warehouse core modeli.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/CostLayers/ProductVariantCostHistory.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Interfaces/IProductVariantCostHistoryRepository.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Services/IProductVariantCostHistoryService.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Services/ProductVariantCostHistoryService.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Queries/GetCostHistoryByVariant/*`
- `src/ECommerce.Persistence/Accounting/Configurations/ProductVariantCostHistoryConfiguration.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/ProductVariantCostHistoryRepository.cs`
- `tests/ECommerce.UnitTests/Accounting/CostLayers/ProductVariantCostHistoryTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/CostLayers/ProductVariantCostHistoryPersistenceTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- Yok.

**Core değişiklik ihtimali**

- Hayır; mevcut ProductVariant ID ve Stock snapshot'ı okunur.

**Kabul kriterleri**

- Bir varyant/currency/depo kapsamı için en fazla bir aktif history kaydı vardır.
- Yeni maliyet geldiğinde önceki kayıt deterministik kapanır.
- History tablosu stok veya FIFO source of truth olarak kullanılmaz.
- Opening/ClosingStockQuantity mevcut stoktan snapshot alınır; stok değiştirilmez.

**Unit testler**

- İlk history kaydı.
- Önceki history kapanışı.
- Aynı maliyet tekrarında no-op veya yeni kayıt politikası.
- Zaman aralığı invariant.

**Integration testler**

- Tek aktif kayıt unique/filter index.
- Tarihe göre sıralı history query.
- Transaction rollback.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~ProductVariantCostHistory"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~ProductVariantCostHistory"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Current stock snapshot'ı ile posting transaction'ı aynı bağlamda alınmazsa tutarsızlık oluşur.

**Çözümlenmemiş kararlar**

- Aynı maliyet tekrarında yeni history kaydı açılıp açılmayacağı.
- Currency ve Warehouse history partition anahtarı.

**Önceki milestone bağımlılığı**

- M07.

---

### M09 — PurchaseInvoiceExpense ve satın alma maliyet dağıtımı

**Durum (2026-07-27):** Tamamlandı. KDV hariç satır tutarı, stok miktarı ve manuel dağıtım; son satır yuvarlama farkı ve final maliyet yeniden hesabı uygulanmıştır.

**Kapsam**

- PurchaseInvoice'a bağlı maliyete dahil giderleri modellemek.
- KDV hariç satır tutarına göre oransal dağıtım.
- Son eligible satıra rounding farkı.
- PurchaseInvoiceLine final cost değerlerini yeniden hesaplamak.

**Karşılanan spesifikasyon gereksinimleri**

- PurchaseInvoiceExpense.
- AllocatedExpense alanları.
- TotalFinalCost formülleri.
- Gider dağıtımında deterministik rounding.

**Kapsam dışı**

- Genel işletme giderleri.
- Kasa/banka ödeme işlemi.
- Quantity/manual allocation yöntemleri, ayrıca onaylanmadıkça.
- Posting.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/PurchaseInvoices/PurchaseInvoiceExpense.cs`
- `src/ECommerce.Domain/Accounting/Expenses/ExpenseAllocationMethod.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Services/IPurchaseExpenseAllocationService.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Services/PurchaseExpenseAllocationService.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Commands/SetPurchaseInvoiceExpenses/*`
- `src/ECommerce.Persistence/Accounting/Configurations/PurchaseInvoiceExpenseConfiguration.cs`
- `tests/ECommerce.UnitTests/Accounting/PurchaseInvoices/PurchaseExpenseAllocationTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/PurchaseInvoices/PurchaseInvoiceExpensePersistenceTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- M05'te oluşturulmuş PurchaseInvoice/PurchaseInvoiceLine ve DTO/handler/config dosyaları.

**Core değişiklik ihtimali**

- Hayır; yalnız Accounting dosyaları değişir.

**Kabul kriterleri**

- Yalnız Draft invoice giderleri değiştirilebilir.
- Dağıtılan gider toplamı girilen gider toplamına tam eşittir.
- Final line cost ve header final cost satırlardan yeniden üretilir.
- Gider işlemi stok hareketi oluşturmaz.

**Unit testler**

- VAT-exclusive line amount proportional allocation.
- Son satır rounding.
- Sıfır eligible base.
- Final unit cost.

**Integration testler**

- Invoice/expense FK.
- Draft-only update.
- Persistence totals.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~PurchaseExpense"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~PurchaseInvoiceExpense"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Bir giderin KDV dahil/hariç tutarının nasıl girileceği net değildir.

**Çözümlenmemiş kararlar**

- İlk sürümde yalnız line amount proportional yönteminin yeterli olup olmadığı.
- Gider VAT oranı ve currency dönüşümü.

**Önceki milestone bağımlılığı**

- M02 ve M05.

---

### M10 — PurchaseInvoice posting workflow

**Durum:** Purchase Accounting kapsamında tamamlandı (2026-07-26) — Serializable transaction içinde CostLayer, cost history, supplier debt ve Posted geçişi atomik/idempotent uygulandı; PurchaseInvoice hiçbir StockMovement üretmiyor ve fiziksel stoğu değiştirmiyor.

**Kapsam**

- Tek serializable transaction içinde tam PurchaseInvoice posting.
- Status ve hesapların yeniden doğrulanması.
- Allocation remaining quantity concurrency kontrolü.
- CostLayer oluşturma.
- CostHistory güncelleme.
- Supplier current account debt oluşturma.
- PostedBy/PostedAt ve status.
- İdempotent tekrar çağrısı.

**Karşılanan spesifikasyon gereksinimleri**

- PurchaseInvoice posting'in bütün 10 transaction adımı.
- PurchaseInvoice hiçbir StockMovement oluşturmaz.
- CostLayer/final cost/supplier debt.
- Posted document immutability.
- Idempotency ve rollback.
- Audit/source ilişkileri.

**Kapsam dışı**

- Purchase cancellation.
- Payment.
- SalesInvoice.
- Migration.

**Beklenen yeni dosyalar**

- `src/ECommerce.Application/Accounting/PurchaseInvoices/Commands/PostPurchaseInvoice/PostPurchaseInvoiceCommand.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Commands/PostPurchaseInvoice/PostPurchaseInvoiceCommandValidator.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Commands/PostPurchaseInvoice/PostPurchaseInvoiceCommandHandler.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Services/IPurchaseInvoicePostingService.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Services/PurchaseInvoicePostingService.cs`
- `tests/ECommerce.UnitTests/Accounting/PurchaseInvoices/PostPurchaseInvoiceCommandHandlerTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/PurchaseInvoices/PurchaseInvoicePostingPersistenceTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- M05 PurchaseInvoicesController'a post endpoint'i.
- M05 PurchaseInvoice aggregate'ına kontrollü Post geçişi.
- M06 allocation, M07 layer, M08 history ve M04 current account Accounting dosyaları.

**Core değişiklik ihtimali**

- Hayır.

**Kabul kriterleri**

- Posting öncesi ve sonrası `StockMovements` satır sayısı aynıdır.
- ProductVariant.Stock değişmez.
- Tam tahsis, CostLayer, history, supplier debt ve Posted status tek commit'tir.
- Herhangi bir adım başarısız olursa hiçbir partial kayıt kalmaz.
- Aynı komut ikinci kez ikinci layer, allocation veya cari hareket oluşturmaz.
- Posted invoice normal update edilemez.

**Unit testler**

- Posted invoice ikinci kez post edilemez/idempotent sonuç politikası.
- Posting servis orchestration sırası.
- Supplier debt direction ve tutarı.
- Audit alanları.

**Integration testler**

- Spesifikasyon purchase testleri 1–12.
- Transaction testleri 52 ve 55'in purchase bölümü.
- Failure injection rollback.
- Concurrent allocation posting.
- StockMovement count ve stock balance unchanged assertion.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~PostPurchaseInvoice"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~PurchaseInvoicePosting"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Allocation concurrency yalnız gerçek SQL Server testinde kesin doğrulanabilir.
- Retry sırasında idempotency anahtarları yeterli değilse duplicate accounting records oluşabilir.

**Çözümlenmemiş kararlar**

- Posting için eksik allocation kabul edilip edilmeyeceği.
- InvoiceDate/PostedAt/CostDate ilişkisi.
- Posted komut tekrarında mevcut DTO mu, conflict mi döneceği.

**Önceki milestone bağımlılığı**

- M04–M09.

---

### M11 — Revised complete Accounting Sales milestone

**Durum:** Tamamlandı (2026-07-26). Ücretsiz satış, TRY/1, kargo ödeyeni, immutable ürün snapshot'ı, opsiyonel sıfır alış/açılış maliyeti ve kalan miktar yeniden değerleme kararları onaylanıp uygulandı. Önceki parçalı sales planı bu tek milestone'un alt çalışma paketlerine birleştirilmiştir; aşağıdaki paketler bağımsız milestone değildir.

**Kapsam**

- `AccountingSalesOrder` ve `AccountingSalesOrderItem` aggregate'ı.
- CurrentAccountId ile Customer veya CustomerAndSupplier doğrulaması; UserId, Cart ve e-ticaret Address bağımlılığının olmaması.
- Accounting isteğinden doğrudan gelen ProductVariant ve quantity satırları.
- Draft create/update, line management, list ve detail.
- Taslak SalesInvoice genel güncellemesi: `PUT /api/accounting/sales-invoices/{id}` başlık ve faturanın tamamını temsil eden `lines` listesiyle bağlı AccountingSalesOrder'ı tek transaction içinde yeniden hesaplar; listede olmayan satırlar kaldırılır, posted/cancelled belge düzenlenemez.
- Merkezi hesap motoruyla item/header totals ve CurrentAccount/Product/ProductVariant snapshot'ları.
- `CreateInvoice=false`, `CreateInvoice=true`, sonradan invoice oluşturma ve direct SalesInvoice entry akışları.
- Optional bire-bir `SalesInvoice` ve `SalesInvoiceLine`.
- `AccountingSale = 22` negatif StockMovement üretimi ve `AccountingSalesOrderStockMovement` mapping.
- Deterministik FIFO `CostLayerConsumption`, RemainingQuantity concurrency koruması, COGS ve profitability.
- AccountingSalesOrder kaynaklı exactly-once customer receivable.
- Atomic/idempotent posting ve tam rollback.
- CreatedBy/UpdatedBy/PostedBy/PostedAt, status ve concurrency.
- ProductVariant `Price > 0` invariant'ını değiştirmeden satış/alış belge satırında opsiyonel ve varsayılan sıfır ticari fiyat.
- İlk Product/ProductVariant/SKU/barkod snapshot'ını kilitleyen, yalnız taslak ticari alanları yerinde güncelleyen satır akışları.
- TRY ve kur 1 sözleşmesi.
- Opsiyonel kargo tutarı ile Seller/Customer ödeyen ayrımı; yalnız Customer kargosu alacağa eklenir.
- Pozitif açılış stoğu için sıfır varsayılanlı explicit OpeningBalance CostLayer ve yalnız RemainingQuantity yeniden değerleme desteği.

**Karşılanan spesifikasyon gereksinimleri**

- AccountingSalesOrder/AccountingSalesOrderItem alanları ve Draft/Posted durumları.
- CurrentAccount customer rolü ve immutable snapshot'lar.
- Header/item totals.
- Optional AccountingSalesOrder↔SalesInvoice bire-bir bağlantısı.
- Existing StockMovement altyapısı, AccountingSale mapping ve physical stock-out.
- FIFO, actual-consumption COGS, gross profit ve margin.
- Exactly-once receivable, transaction, idempotency ve rollback.
- Existing e-ticaret Order/Cart kodunun değişmemesi.
- Ücretsiz satışta stok çıkışı/FIFO devam eder; sıfır alacak kaydı oluşmaz ve gerçek FIFO maliyeti zarar olarak negatif kâra yansır.
- Belge ticari güncellemeleri Product/ProductVariant master verisini değiştirmez.

**Kapsam dışı**

- Harici e-fatura provider entegrasyonu.
- Payment/collection allocation.
- Cash/bank, expenses ve reports.
- Cancellation/reversal workflow; aggregate'taki Cancelled durumu sonraki onaylı kapsama ayrılmıştır.
- Migration.
- Gerçek multi-warehouse ve WarehouseId.
- Mevcut e-ticaret Order, OrderItem, Cart, User, Address, Product, ProductVariant veya Warehouse değişikliği.

**Oluşturulan dosyalar**

- `src/ECommerce.Domain/Accounting/SalesInvoices/SalesInvoice.cs` — opsiyonel iç fatura aggregate'ı ve değişmez header snapshot'ları.
- `src/ECommerce.Domain/Accounting/SalesInvoices/SalesInvoiceLine.cs` — sipariş item'ından üretilen ürün, fiyat, maliyet ve kârlılık snapshot'ı.
- `src/ECommerce.Domain/Accounting/SalesOrders/AccountingSalesOrder.cs` — CurrentAccount tabanlı Draft/Posted satış aggregate'ı ve header toplamları.
- `src/ECommerce.Domain/Accounting/SalesOrders/AccountingSalesOrderItem.cs` — request satırı, ürün/varyant snapshot'ı, stok mapping, FIFO ve kârlılık davranışı.
- `src/ECommerce.Domain/Accounting/SalesOrders/AccountingSalesOrderStockMovement.cs` — core StockMovement'a Accounting FK eklemeden item/movement bağı.
- `src/ECommerce.Domain/Accounting/CostLayers/CostLayerConsumption.cs` — gerçek FIFO katmanı, satış item'ı ve AccountingSale movement arasındaki değişmez maliyet izi.
- `src/ECommerce.Application/Accounting/SalesOrders/SalesAccountingContracts.cs` — input, DTO, repository ve reader sözleşmeleri.
- `src/ECommerce.Application/Accounting/SalesOrders/SalesAccountingCommands.cs` — draft, item, invoice, posting, liste ve detay CQRS istekleri.
- `src/ECommerce.Application/Accounting/SalesOrders/SalesAccountingHandlers.cs` — transaction koordinasyonu, posting, FIFO, receivable, idempotency ve DTO eşlemeleri.
- `src/ECommerce.Application/Accounting/SalesOrders/SalesAccountingValidators.cs` — header, satır, indirim, idempotency ve sayfalama doğrulamaları.
- `src/ECommerce.Persistence/Accounting/Configurations/SalesAccountingConfigurations.cs` — sales aggregate, bire-bir invoice, mapping, tüketim ve unique/concurrency EF modeli.
- `src/ECommerce.Persistence/Accounting/Repositories/SalesAccountingRepositories.cs` — sales graph, katalog, FIFO ve invoice repository uygulamaları.
- `src/ECommerce.API/Controllers/Accounting/AccountingSalesOrdersController.cs` — AccountingSalesOrder HTTP uçları.
- `src/ECommerce.API/Controllers/Accounting/SalesInvoicesController.cs` — direct/later invoice, update, post, liste ve detay HTTP uçları.
- `src/ECommerce.Domain/Accounting/CostLayers/ProductVariantCostHistorySourceType.cs` — PurchaseInvoice ve OpeningBalance history kaynak sözleşmesi.
- `src/ECommerce.Application/Accounting/CostLayers/OpeningBalanceCostLayerContracts.cs` — açılış maliyet seed'i, DTO ve repository/writer sözleşmeleri.
- `src/ECommerce.Application/Accounting/CostLayers/OpeningBalanceCostLayerOperations.cs` — varsayılan sıfır katman, kalan miktar yeniden değerleme ve history close/open akışı.
- `src/ECommerce.Application/Accounting/CostLayers/ProductVariantCostHistoryOperations.cs` — varyant bazlı kronolojik history DTO/query/validator/handler yüzeyi.
- `src/ECommerce.Persistence/Accounting/Repositories/OpeningBalanceCostLayerRepository.cs` — opening layer idempotency ve concurrency repository'si.
- `src/ECommerce.Persistence/Accounting/Repositories/ProductVariantCostHistoryRepository.cs` — deterministik salt-okunur history sorgusu.
- `src/ECommerce.API/Controllers/Accounting/InventoryCostLayersController.cs` — opening layer detay ve maliyet güncelleme uçları.
- `src/ECommerce.API/Controllers/Accounting/ProductVariantCostHistoryController.cs` — AdminOnly varyant maliyet geçmişi ucu.
- `tests/ECommerce.UnitTests/Accounting/SalesOrders/AccountingSalesArchitectureTests.cs` — e-ticaret Order/User/Cart/Address/Warehouse ve doğrudan invoice-stock bağımsızlık kanıtları.
- `tests/ECommerce.UnitTests/Accounting/SalesOrders/AccountingSalesOrderTests.cs` — lifecycle, FIFO, kârlılık, snapshot ve domain invariant testleri.
- `tests/ECommerce.UnitTests/Accounting/SalesOrders/SalesAccountingValidatorTests.cs` — satır ve invoice indirim kapsamı validasyon testleri.
- `tests/ECommerce.IntegrationTests/Accounting/SalesOrders/AccountingSalesPostingTests.cs` — gerçek persistence ile draft/post/FIFO/concurrency/idempotency/rollback kanıtları.
- `tests/ECommerce.IntegrationTests/Api/AccountingSalesControllersTests.cs` — route, HTTP fiili, AdminOnly, idempotency header ve 401/403 sözleşme testleri.
- `tests/ECommerce.UnitTests/Accounting/CostLayers/OpeningBalanceCostLayerTests.cs` — sıfır maliyet, hassasiyet, remaining-only update ve consumption immutability testleri.
- `tests/ECommerce.UnitTests/Accounting/CostLayers/OpeningBalanceCostLayerWriterTests.cs` — create seed, fallback, idempotency ve history testleri.
- `tests/ECommerce.UnitTests/Accounting/CostLayers/ProductVariantCostHistoryTests.cs` — source, invariant, query mapping ve validator testleri.
- `tests/ECommerce.IntegrationTests/Accounting/CostLayers/OpeningBalanceCostLayerCreationTests.cs` — ürün oluşturma akışlarının layer/history atomik persistence kanıtı.
- `tests/ECommerce.IntegrationTests/Accounting/CostLayers/ProductVariantCostHistoryPersistenceTests.cs` — source/index/tek aktif kayıt ve deterministik query kanıtı.

**Değiştirilen mevcut/onaylı dosyalar**

- `src/ECommerce.Domain/Accounting/CostLayers/InventoryCostLayer.cs` — FIFO tüketimi, RemainingQuantity ve uygulama yönetimli concurrency token.
- `src/ECommerce.Domain/Accounting/Common/Enums/AccountingSourceType.cs` — receivable kaynağı olarak AccountingSalesOrder.
- `src/ECommerce.Persistence/Accounting/Configurations/PurchaseAccountingConfigurations.cs` — InventoryCostLayer concurrency ve consumption navigation modeli.
- `src/ECommerce.Persistence/Accounting/AccountingPersistenceServiceRegistration.cs` — Sales repository/reader DI bağlantıları.
- `src/ECommerce.Domain/Enums/StockMovementType.cs` — kullanıcı onaylı `AccountingSale = 22`.
- `src/ECommerce.Domain/Entities/StockMovement.cs` — AccountingSale için yalnız negatif Out yön kuralı.
- `src/ECommerce.Persistence/Configurations/StockMovementConfiguration.cs` — AccountingSale type/direction check constraint desteği.
- `tests/ECommerce.UnitTests/Domain/StockMovementTests.cs` — orderless AccountingSale yön ve miktar regresyonu.
- `tests/ECommerce.IntegrationTests/Persistence/StockMovementPersistenceTests.cs` — AccountingSale persistence, constraint ve bakiye mutabakatı.
- Product ve varyant create command/validator/handler dosyaları — opsiyonel açılış maliyetini aynı UoW içinde OpeningBalance layer'a taşır; Product/ProductVariant entity'si değişmez.
- `src/ECommerce.Domain/Accounting/PurchaseInvoices/PurchaseInvoice.cs` ve `PurchaseInvoiceLine.cs` — yerinde ticari güncelleme, audit ve allocation miktarı invariant'ı.
- `src/ECommerce.Application/Accounting/PurchaseInvoices/PurchaseAccountingHandlers.cs` — immutable identity, allocation koruması ve bütün draft line mutasyonlarında serializable transaction.
- Purchase unit/integration testleri — sıfır maliyet, master/snapshot immutability, allocation koruması ve draft rollback kanıtları.
- `AGENTS.md`, `docs/accounting-module-spec.md` ve bu M11 bölümü — revised Accounting satış mutlak kuralları ve doğrulama kaydı.

Silinen dosya ve oluşturulan migration yoktur.

**Core değişiklik ihtimali**

- Onaylandı: `AccountingSale = 22` enum, direction/reference ve EF constraint değişiklikleri.
- `StockMovement` üzerine AccountingSalesOrderId eklenmez; ilişki Accounting mapping tablosundadır.
- Mevcut e-ticaret Order/OrderItem/Cart/User/Product/ProductVariant/Warehouse değişmez.

**Kabul kriterleri**

- Draft AccountingSalesOrder hiçbir StockMovement, consumption veya receivable oluşturmaz.
- AccountingSalesOrder üzerinde UserId/CartId/AddressId/WarehouseId bulunmaz ve Cart servisi kullanılmaz.
- Request ProductVariantId ve quantity değerleri persisted item'lara doğru aktarılır.
- Posted AccountingSalesOrder beklenen negatif AccountingSale hareketlerini ve mapping kayıtlarını oluşturur.
- Existing e-ticaret Orders sayısı değişmez ve Order kodu değiştirilmez.
- CreateInvoice=false invoice oluşturmaz; CreateInvoice=true tam bir invoice oluşturur.
- Sonradan invoice oluşturma stok veya receivable etkisini tekrarlamaz.
- Direct SalesInvoice entry exactly one AccountingSalesOrder oluşturur.
- SalesInvoice doğrudan StockMovement oluşturmaz.
- Pozitif GrandTotalIncludingVat ile Posted AccountingSalesOrder exactly one customer receivable oluşturur; sıfır tutarlı ücretsiz satış sıfır değerli cari hareket oluşturmaz.
- FIFO COGS ve profitability actual CostLayerConsumption kayıtlarına eşittir.
- Retry duplicate effect oluşturmaz; failure bütün etkileri rollback eder.
- Seller-paid shipping alacağa, KDV'ye, COGS'a veya gross profit'e eklenmez; Customer-paid shipping yalnız belge toplamı ve alacağa eklenir.
- Satış ve alış satırlarının ProductVariantId/SKU/barkod snapshot'ları değiştirilemez; fiyat, KDV, miktar, birim ve indirim yalnız belge üzerinde değişir.
- Açılış maliyeti girilmezse sıfır katman oluşur; sonraki maliyet güncellemesi yalnız tüketilmemiş miktarı etkiler.

**Unit testler**

- Domain lifecycle, item/header calculation ve snapshot testleri.
- UserId/Cart/Address/Warehouse yapısal bağımsızlık testleri.
- Optional invoice ve direct invoice command/handler testleri.
- Deterministic FIFO, multiple layer, RemainingQuantity ve profitability testleri.
- Posting orchestration, idempotency ve dependency boundary testleri.

**Integration testler**

- Revised Sales test matrisi 13–29 ve transaction testleri 53–55.
- CurrentAccount/ProductVariant FK, unique AccountingSalesOrder number ve optional one-to-one invoice.
- Draft no-effect; posted AccountingSale/mapping/stock balance.
- Existing e-ticaret Order count unchanged.
- Repost, later invoice ve direct invoice idempotency.
- FIFO, concurrency ve injected-failure full rollback.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Doğrulama sonucu (2026-07-26)**

- Sales unit: 30/30 başarılı.
- Sales integration ve API: 29/29 başarılı.
- Purchase unit: 14/14 başarılı.
- Purchase integration ve API: 15/15 başarılı.
- OpeningBalance ve ProductVariantCostHistory unit/integration: 13/13 ve 7/7 başarılı.
- Phase 3+4 Accounting unit: 138/138 başarılı.
- Phase 3+4 Accounting integration: 51/51 başarılı.
- Tüm solution unit/integration testleri: 415/415 ve 147/147 başarılı.
- Product/create/StockMovement core unit regresyonu: 36/36 başarılı.
- Existing OrdersController ve StockMovement persistence integration regresyonu: 6/6 başarılı.
- Final Release build: 0 uyarı, 0 hata.
- `git diff --check`: başarılı; migration diff'i yok.

**Riskler**

- SQL Server concurrency davranışı yalnız SQLite ile kesin kanıtlanamaz.
- StockMovement core constraint değişikliği mevcut e-ticaret Sale davranışını bozmamalıdır.

**Bu milestone kapsamında çözülen kararlar**

- Maliyet girilmeyen pozitif açılış stoğu explicit sıfır maliyetli OpeningBalance katmanı kullanır; geçmiş tüketimler sonradan yeniden değerlenmez.
- Aynı ProductVariant farklı LineNumber değerleriyle ayrı belge satırlarında bulunabilir; mevcut satırın varyant kimliği değiştirilemez.
- Sıfır net satışta gross profit gerçek FIFO maliyeti kadar negatif olabilir, margin sıfır döner.
- Yalnız TRY ve kur 1 desteklenir.
- ShippingTotal varsayılan sıfırdır; Customer öderse alacağa eklenir, Seller öderse eklenmez ve her iki durumda FIFO/COGS/kârlılıktan ayrıdır.

**Önceki milestone bağımlılığı**

- M01–M08, M10 ve tamamlanmış Phase 3 Accounting altyapısı.

---

#### M11-A — AccountingSalesOrder stock-out yüzeyi

Bu bölüm bağımsız milestone değildir; revised M11 içindeki stock-out çalışma paketidir.

**Kapsam**

- AccountingSalesOrder'ın CurrentAccount ve request-supplied ProductVariant satırlarını kullanması.
- Existing ProductVariant/StockMovement aggregate yöntemiyle `AccountingSale = 22` negatif hareket üretmek.
- Her hareketi `AccountingSalesOrderStockMovement` ile doğru AccountingSalesOrderItem'a bağlamak.
- StockMovement'a AccountingSalesOrderId eklememek.
- Tek örtük depo kapsamında WarehouseId kullanmamak.
- E-ticaret CreateOrder/Cart workflow'unu değiştirmemek ve çağırmamak.

**Karşılanan spesifikasyon gereksinimleri**

- AccountingSalesOrder posting mevcut stock infrastructure'ı kullanır.
- SalesInvoice doğrudan StockMovement oluşturmaz.
- Stock-out AccountingSalesOrder posting tarafından AccountingSale olarak oluşturulur.
- Core entity duplicate edilmez.
- Mapping ile source izlenebilirliği sağlanır.

**Kapsam dışı**

- E-ticaret Order/OrderItem/Cart değişikliği.
- StockMovement'a AccountingSalesOrderId eklemek.
- Warehouse entity veya WarehouseId.
- Sales cancellation.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/SalesOrders/AccountingSalesOrderStockMovement.cs`
- Accounting stock-out writer interface ve service'i.
- AccountingSalesOrderStockMovement EF configuration ve repository'si.
- Accounting sales stock-out unit ve integration testleri.

**Değişmesi gerekebilecek mevcut dosyalar**

- `src/ECommerce.Domain/Enums/StockMovementType.cs`
- `src/ECommerce.Domain/Entities/StockMovement.cs`
- `src/ECommerce.Persistence/Configurations/StockMovementConfiguration.cs`
- StockMovement unit/persistence testleri.
- Accounting DI registration.

**Core değişiklik ihtimali**

- Onaylandı: AccountingSale enum/direction/required-reference/database constraint desteği.
- E-ticaret Order, OrderItem, Cart, User, Product, ProductVariant ve Warehouse değişikliği onaylanmamıştır ve yapılmaz.

**Kabul kriterleri**

- Mevcut `POST /api/orders` davranışı ve testleri değişmeden geçer.
- AccountingSalesOrder post her item için beklenen AccountingSale çıkışını mevcut ProductVariant aggregate yöntemiyle üretir.
- Mapping item, movement ve ProductVariant tutarlılığını korur.
- AccountingSale e-ticaret OrderId gerektirmez; mevcut Sale davranışı OrderId gerektirmeye devam eder.
- Retry ikinci movement veya mapping oluşturmaz.
- Failure ProductVariant.Stock, movement, mapping ve AccountingSalesOrder status değişikliklerini rollback eder.
- Accounting production kodu ICartRepository/IOrderRepository kullanmaz.

**Unit testler**

- Accounting input mapping ve CurrentAccount/ProductVariant validation.
- AccountingSale direction/reference.
- Mapping invariant ve duplicate koruması.
- Cart/Order dependency absence.

**Integration testler**

- Mevcut CreateOrder testlerinin tam regresyonu.
- AccountingSalesOrder ile AccountingSale movement ve mapping.
- Insufficient stock rollback.
- Transaction rollback ile movement/mapping/status etkisinin kalmaması.
- Retry duplicate movement koruması.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~CreateOrder|FullyQualifiedName~AccountingSalesOrder"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~CreateOrder|FullyQualifiedName~AccountingSalesOrder"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- StockMovement constraint değişikliğinin mevcut Sale checkout regresyonuna yol açması.
- Aynı variant için deterministik movement/mapping sırası.

**Çözülen kararlar**

- Aynı ProductVariant farklı LineNumber ile ayrı satırlarda tutulabilir; mevcut satırın ProductVariant kimliği değiştirilemez.
- Maliyet girilmemiş pozitif açılış stoğu explicit sıfır maliyetli katman üzerinden satılabilir.

**M11 içi bağımlılık**

- M11 aggregate/item ve onaylı AccountingSale core değişikliği.

---

#### M11-B — FIFO CostLayerConsumption

Bu bölüm bağımsız milestone değildir; revised M11 içindeki FIFO çalışma paketidir.

**Kapsam**

- AccountingSalesOrder tarafından oluşturulan mapped AccountingSale StockMovement kayıtlarına bağlı FIFO tüketimi.
- CostDate, CreatedAt, Id deterministik sırası.
- Bir sale movement'ın birden fazla layer tüketmesi.
- `RemainingQuantity` concurrency güvenliği.
- Consumption kayıtlarıyla purchase→sale izlenebilirliği.

**Karşılanan spesifikasyon gereksinimleri**

- FIFO CostLayer consumption.
- CostLayerConsumption alanları.
- Tüketimin SalesInvoice tarafından doğrudan yapılmaması.
- AccountingSalesOrderItem ve mapped AccountingSale stock-out bağlantısı.
- Concurrent over-consumption engeli.

**Kapsam dışı**

- SalesInvoice posting orchestration.
- Profit calculation.
- Return cost restoration.
- Purchase cancellation.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/CostLayers/CostLayerConsumption.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Interfaces/ICostLayerConsumptionRepository.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Services/IFifoCostLayerConsumptionService.cs`
- `src/ECommerce.Application/Accounting/CostLayers/Services/FifoCostLayerConsumptionService.cs`
- `src/ECommerce.Persistence/Accounting/Configurations/CostLayerConsumptionConfiguration.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/CostLayerConsumptionRepository.cs`
- `tests/ECommerce.UnitTests/Accounting/CostLayers/FifoCostLayerConsumptionServiceTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/CostLayers/FifoCostLayerConsumptionPersistenceTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- M07 InventoryCostLayer Accounting dosyaları.
- M11 AccountingSalesOrder stock-out ve mapping dosyaları.

**Core değişiklik ihtimali**

- Hayır; FIFO yalnız AccountingSalesOrder kapsamındadır ve e-ticaret Order workflow'una hook eklenmez.

**Kabul kriterleri**

- FIFO oldest-first deterministiktir.
- Bir sale birden fazla layer tüketebilir.
- RemainingQuantity negatif olamaz.
- Consumption, AccountingSalesOrderId, AccountingSalesOrderItemId ve gerçek StockMovementId taşır.
- Aynı AccountingSale movement miktarı ikinci kez tüketilemez.
- Concurrent satışlar aynı layer'ı aşırı tüketemez.

**Unit testler**

- Spesifikasyon FIFO/COGS testleri 44–48 ve concurrency testi 51.
- Deterministic tie-break.
- Multiple layer.
- Sıfır maliyetli explicit OpeningBalance katmanının normal FIFO ile tüketimi.

**Integration testler**

- Consumption FK/unique constraints.
- Concurrent FIFO.
- Transaction rollback.
- Gerçek AccountingSalesOrder-created movement ve mapping bağlantısı.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~FifoCostLayer"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~FifoCostLayer"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- SQL Server concurrency davranışı migration kapsamındaki sonraki doğrulamada ayrıca sınanmalıdır.

**Çözülen ve kapsam dışı kararlar**

- Maliyetsiz pozitif açılış stoğu explicit sıfır maliyetli layer kullanır; sonradan update yalnız RemainingQuantity'yi etkiler.
- Return durumunda orijinal layer'a geri ekleme veya reversal layer cancellation/return milestone'una bırakılmıştır.

**M11 içi bağımlılık**

- M07 ve M11-A stock-out mapping.

---

#### M11-C — Sales cost ve profitability

Bu bölüm bağımsız milestone değildir; revised M11 içindeki profitability çalışma paketidir.

**Kapsam**

- FIFO consumption sonuçlarından line ve invoice COGS hesaplamak.
- KDV hariç net satıştan gross profit hesaplamak.
- Profit margin hesabını sıfıra bölünmeye karşı korumak.
- Satır ve header maliyet/kâr sonuçlarını tek merkezi servisle üretmek.
- Hesaplama servisini posting orchestration'dan bağımsız doğrulamak.

**Karşılanan spesifikasyon gereksinimleri**

- COGS yalnız `CostLayerConsumption` toplamından türetilir.
- Gross profit, KDV hariç net satış eksi COGS'tur.
- Header COGS/profit satır toplamlarıyla mutabıktır.
- Profit margin sıfıra bölünmeye karşı güvenlidir.
- Spesifikasyon COGS/profitability testleri 47–50.

**Kapsam dışı**

- AccountingSalesOrder veya StockMovement oluşturmak.
- SalesInvoice posting ve status değişikliği.
- Customer receivable.
- Cancellation/refund, payment, report ve migration.

**Beklenen yeni dosyalar**

- `src/ECommerce.Application/Accounting/SalesOrders/Services/ISalesProfitabilityService.cs`
- `src/ECommerce.Application/Accounting/SalesOrders/Services/SalesProfitabilityService.cs`
- `src/ECommerce.Application/Accounting/SalesOrders/Models/SalesProfitabilityInput.cs`
- `src/ECommerce.Application/Accounting/SalesOrders/Models/SalesProfitabilityResult.cs`
- `src/ECommerce.Application/Accounting/SalesOrders/Models/SalesLineProfitabilityResult.cs`
- `tests/ECommerce.UnitTests/Accounting/SalesOrders/SalesProfitabilityServiceTests.cs`
- `tests/ECommerce.IntegrationTests/Accounting/SalesOrders/SalesProfitabilityProjectionTests.cs`

**Değişmesi gerekebilecek mevcut dosyalar**

- M11 AccountingSalesOrder/AccountingSalesOrderItem ve optional SalesInvoice dosyaları.
- M11-B CostLayerConsumption query/reader dosyaları.

**Core değişiklik ihtimali**

- Hayır.
- Servis AccountingSalesOrder, item veya StockMovement kayıtlarını keyfi olarak değiştirmez; yalnız consumption sonuçlarını uygular.

**Kabul kriterleri**

- COGS yalnız ilgili consumption kayıtlarından hesaplanır.
- Bir AccountingSalesOrderItem birden fazla CostLayer tükettiğinde toplam doğru birleşir.
- Satır ve header COGS/profit sonuçları tam mutabıktır.
- Sıfır net satışta margin güvenli ve onaylı sonuç üretir.
- Servis AccountingSalesOrder, StockMovement, CostLayer veya cari hareket oluşturamaz.

**Unit testler**

- Spesifikasyon COGS/profitability testleri 47–50.
- Tek ve çok CostLayer consumption toplamı.
- Kârlı, zararlı ve sıfır net satış.
- Header-line rounding mutabakatı.
- Aynı varyantın birden fazla invoice satırında güvenli eşlenmesi.

**Integration testler**

- AccountingSalesOrderItem→AccountingSalesOrderStockMovement→CostLayerConsumption projection zinciri.
- Persistence'tan okunan consumption toplamı ile hesaplanan COGS eşitliği.
- Hesaplama sırasında hiçbir stok/cari/status yan etkisi oluşmaması.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~SalesProfitability"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~SalesProfitability"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Aynı ProductVariant birden fazla invoice/item içinde bulunursa yanlış eşleme yapılması.

**Çözülen kararlar**

- Sıfır net satışta margin `0` döner; gross profit gerçek FIFO maliyeti nedeniyle negatif olabilir.
- Para alanları merkezi AwayFromZero politikasıyla yuvarlanır.
- Yalnız TRY ve kur 1 desteklenir; foreign-currency profitability bu milestone'da yoktur.

**M11 içi bağımlılık**

- M11 aggregate/item ve M11-B FIFO.

---

#### M11-D — Atomik AccountingSalesOrder posting ve optional SalesInvoice workflow'u

Bu bölüm bağımsız milestone değildir; revised M11 içindeki posting çalışma paketidir.

**Kapsam**

- AccountingSalesOrder posting'i tek güvenli workflow olarak tamamlamak.
- Merkezi item/invoice hesaplarını yeniden doğrulamak.
- AccountingSale StockMovement ve AccountingSalesOrderStockMovement mapping kayıtlarını oluşturmak.
- M11-B FIFO tüketimini mapped movement'lara bağlamak.
- M11-C ile item/AccountingSalesOrder/invoice COGS, gross profit ve margin üretmek.
- AccountingSalesOrder kaynaklı customer current account receivable oluşturmak.
- Optional SalesInvoice linki, PostedBy, PostedAt ve Posted status kaydetmek.
- CreateInvoice=false/true, sonradan invoice ve direct invoice entry akışlarını aynı idempotent coordinator'da birleştirmek.
- İdempotency ve tam rollback sağlamak.

**Karşılanan spesifikasyon gereksinimleri**

- Sales posting'in bütün transaction adımları.
- Exactly one AccountingSalesOrder ve optional one-to-one SalesInvoice link.
- SalesInvoice doğrudan StockMovement oluşturmaz.
- AccountingSalesOrder AccountingSale stock-out oluşturur.
- FIFO-based COGS/profit.
- Pozitif toplamda customer receivable exactly once; sıfır toplamda cari hareket yok.

**Kapsam dışı**

- Sales cancellation/refund.
- Payment allocation.
- Reports.
- Migration.

**Beklenen yeni dosyalar**

- AccountingSalesOrder post command, validator and handler.
- AccountingSalesOrder ve direct-invoice entry akışlarının kullandığı ortak Accounting sales posting coordinator.
- Optional SalesInvoice creation and direct-entry commands.
- Accounting sales posting unit tests.
- Accounting sales posting, optional invoice and rollback integration tests.

**Değişmesi gerekebilecek mevcut dosyalar**

- M11 AccountingSalesOrder ve SalesInvoice aggregate, DTO ve controller dosyaları.
- M11-A stock-out mapping dosyaları.
- M11-B FIFO service dosyaları.
- M11-C profitability service dosyaları.
- M04'te oluşturulacak current account posting service dosyaları.

**Core değişiklik ihtimali**

- Onaylı AccountingSale core değişiklikleri dışında yeni core değişiklik beklenmez.
- Existing e-ticaret Order transaction'ı bu workflow'a katılmaz.

**Kabul kriterleri**

- Draft posting aynı AccountingSalesOrder'ı Posted yapar ve yeni e-ticaret Order oluşturmaz.
- Optional SalesInvoice linki unique ve kalıcıdır.
- İkinci post ikinci movement, mapping, consumption, receivable, AccountingSalesOrder veya SalesInvoice oluşturmaz.
- AccountingSale movements yalnız AccountingSalesOrder posting workflow'undan gelir.
- COGS consumption toplamına eşittir.
- Profit ve margin satır/header seviyesinde mutabıktır.
- Herhangi bir failure AccountingSalesOrder ve invoice durumlarını Draft bırakır; partial movement/mapping/cari/consumption bırakmaz.

**Unit testler**

- Posting orchestration sırası ve idempotency.
- Pozitif toplamda customer receivable direction/tutar ve sıfır toplamda hareket oluşmaması.
- SalesInvoice'ın doğrudan stock writer bağımlılığı taşımaması.
- Stock-out mapping, FIFO ve profitability servis hata yolları.

**Integration testler**

- Revised spesifikasyon sales testleri 13–29.
- Transaction testleri 53–55.
- Insufficient stock rollback.
- Explicit sıfır OpeningBalance layer FIFO davranışı.
- Unique AccountingSalesOrderId on SalesInvoice.
- Movement/mapping/current-account/consumption adımlarından enjekte edilen her hata için tam rollback.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Stock, FIFO ve receivable effect'lerinin aynı retryable transaction'da doğru sırada uygulanması gerekir.
- Direct invoice ve later-invoice yarışları unique constraint ve status kontrolü gerektirir.

**Çözülen karar**

- Pozitif açılış stoğu maliyetsiz bırakılmaz; maliyet girilmezse sıfır cost layer oluşturulur ve satışta normal FIFO ile tüketilir.

**M11 içi bağımlılık**

- M04, M11 aggregate'ları ve M11-A–M11-C çalışma paketleri.

---

### M15 — PurchaseInvoice cancellation ve maliyet ters kayıtları

**Durum (2026-07-27):** Tamamlandı. Tüketilmemiş katmanlar geçersizleştirilir; tüketilmiş CostLayer bulunan iptal, geriye dönük maliyet politikası onaylanana kadar atomik olarak engellenir.

**Kapsam**

- Posted PurchaseInvoice için cancellation lifecycle.
- CancelledBy/CancelledAt/reason.
- Supplier debt reversal.
- CostLayer ve CostHistory için onaylanmış ters kayıt davranışı.
- Stok hareketi oluşturmadan cancellation.

**Karşılanan spesifikasyon gereksinimleri**

- Posted document hard-delete yasağı.
- Purchase cancellation reversing entries.
- Supplier current account reversal.
- CostLayer güvenli reversal/invalidation.

**Kapsam dışı**

- Sales cancellation.
- Payment refund.
- Onaylanmamış consumed-layer stratejisi.

**Beklenen yeni dosyalar**

- `src/ECommerce.Application/Accounting/PurchaseInvoices/Commands/CancelPurchaseInvoice/*`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Services/IPurchaseInvoiceCancellationService.cs`
- `src/ECommerce.Application/Accounting/PurchaseInvoices/Services/PurchaseInvoiceCancellationService.cs`
- Seçilen strateji gerektirirse `src/ECommerce.Domain/Accounting/CostLayers/CostAdjustmentEntry.cs`
- İlgili unit/integration cancellation test dosyaları.

**Değişmesi gerekebilecek mevcut dosyalar**

- PurchaseInvoice aggregate/controller.
- Current account transaction service.
- CostLayer/history Accounting dosyaları.

**Core değişiklik ihtimali**

- Hayır; Purchase cancellation StockMovement oluşturamaz.

**Kabul kriterleri**

- Yalnız Posted invoice iptal edilebilir.
- İptal ikinci kez uygulanamaz veya idempotent mevcut sonucu döndürür.
- StockMovement sayısı ve stok değişmez.
- Supplier debt tam bir ters kayıtla kapanır.
- Consumed CostLayer için yalnız önceden onaylanmış strateji uygulanır.

**Unit testler**

- Cancellation lifecycle/audit.
- Supplier reversal.
- Seçilen consumed-layer stratejisi.

**Integration testler**

- Atomic reversal.
- Failure rollback.
- No StockMovement assertion.
- Duplicate cancellation/idempotency.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~CancelPurchaseInvoice"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~PurchaseInvoiceCancellation"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Tüketilmiş layer reversal geçmiş kârlılık raporlarını etkiler.

**Çözümlenmemiş kararlar**

- Tüketilmiş layer varsa block, cost adjustment veya reversing cost entry.
- Kısmi cancellation desteği.

**Önceki milestone bağımlılığı**

- M10 ve açık cancellation stratejisi onayı.

---

### M16 — AccountingSalesOrder cancellation ve reversal

**Durum (2026-07-27):** Tamamlandı. AccountingSalesOrder stok ve alacak iptalinin sahibidir; `AccountingSaleCancellation=23`, ayrı FIFO ters kayıtları ve bağlı SalesInvoice durum eşitlemesi uygulanmıştır.

**Kapsam**

- Posted AccountingSalesOrder ve optional SalesInvoice cancellation.
- Customer receivable reversal.
- AccountingSale stock-out için ileride ayrıca onaylanacak StockMovement reversal davranışı.
- SalesInvoice'ın bağımsız stok veya receivable reversal oluşturmamasını korumak.
- Audit ve idempotency.

**Karşılanan spesifikasyon gereksinimleri**

- Accounting sales cancellation/reversal.
- Direct stock restoration yasağı.
- Customer receivable reversal.
- Posted document hard-delete yasağı.

**Kapsam dışı**

- Yeni refund provider entegrasyonu.
- Existing e-ticaret Order/Return workflow değişikliği.

**Beklenen yeni dosyalar**

- AccountingSalesOrder cancellation command/service ve optional invoice coordination dosyaları.
- Accounting sales cancellation unit ve integration testleri.

**Değişmesi gerekebilecek mevcut dosyalar**

- AccountingSalesOrder ve SalesInvoice aggregate/controller.
- AccountingSale mapping, FIFO ve current account reversal dosyaları.

**Core değişiklik ihtimali**

- Onaylı reversal tasarımı StockMovement core değişikliği gerektirirse ayrıca açık onay alınır.
- Existing Order/Return değiştirilmez.

**Kabul kriterleri**

- Yalnız Posted AccountingSalesOrder iptal edilebilir.
- Approved Accounting stock reversal existing StockMovement altyapısını kullanır.
- Customer receivable ters kaydı tek kez oluşur.
- Optional SalesInvoice ikinci reversal oluşturmaz.
- Cancellation failure partial accounting reversal bırakmaz.

**Unit testler**

- AccountingSalesOrder status ve reversal strategy.
- Receivable reversal.
- Audit/idempotency.

**Integration testler**

- Approved AccountingSale reversal davranışı.
- Mapping/consumption/source doğrulaması.
- Transaction rollback.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~CancelAccountingSalesOrder"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~AccountingSalesOrderCancellation"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- AccountingSalesOrder ile SalesInvoice status ayrışabilir.

**Çözümlenmemiş kararlar**

- AccountingSale reversal tipi ve consumed FIFO restoration politikası.
- Finansal refund zamanı.
- İade maliyetinin hangi layer'a döneceği.

**Önceki milestone bağımlılığı**

- Revised M11 ve ayrıca onaylanacak cancellation/reversal kararı.

---

### M17 — CurrentAccount API, ekstre ve vade sorguları

**Durum (2026-07-27):** Tamamlandı. Cari ekstre, müşteri alacağı, tedarikçi borcu ve ayrı gecikmiş alacak/borç sorguları salt okunur rapor yüzeyine bağlanmıştır.

**Kapsam**

- Cari hesap listesi/detayı.
- Statement query.
- Customer receivables, supplier debts ve overdue invoices.
- Currency bazlı bakiye projection.
- Source document navigation bilgileri.

**Karşılanan spesifikasyon gereksinimleri**

- Current account reporting.
- Customer receivables.
- Supplier debts.
- Overdue invoices.
- Kaynak ilişki izlenebilirliği.

**Kapsam dışı**

- Payment allocation.
- Cash/bank.
- Cached balance, ayrıca onaylanmadıkça.

**Beklenen yeni dosyalar**

- `src/ECommerce.Application/Accounting/CurrentAccounts/Queries/GetCurrentAccounts/*`
- `src/ECommerce.Application/Accounting/CurrentAccounts/Queries/GetCurrentAccountStatement/*`
- `src/ECommerce.Application/Accounting/CurrentAccounts/Queries/GetCustomerReceivables/*`
- `src/ECommerce.Application/Accounting/CurrentAccounts/Queries/GetSupplierDebts/*`
- `src/ECommerce.Application/Accounting/CurrentAccounts/Queries/GetOverdueInvoices/*`
- `src/ECommerce.API/Controllers/Accounting/CurrentAccountsController.cs`
- İlgili unit/integration query testleri.

**Değişmesi gerekebilecek mevcut dosyalar**

- M04 repository ve DTO dosyaları.

**Core değişiklik ihtimali**

- Hayır.

**Kabul kriterleri**

- Bakiyeler transaction toplamından hesaplanır.
- Currency değerleri birbirine kontrolsüz toplanmaz.
- Liste sorguları sayfalıdır ve stable ordering kullanır.
- SourceType/SourceId ile invoice'a iz sürülebilir.

**Unit testler**

- Debit-credit balance projection.
- Due date/overdue calculation.
- Currency grouping.

**Integration testler**

- Statement ordering/filtering.
- Customer/Supplier isolation.
- API authorization ve pagination.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~CurrentAccount"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~CurrentAccount"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Büyük statement sorguları projection ve index gerektirir.

**Çözümlenmemiş kararlar**

- Accounting authorization rolü.
- Bakiye cache ihtiyacı.
- Kur çevrimiyle konsolide bakiye gösterimi.

**Önceki milestone bağımlılığı**

- M04, M10 ve revised M11.

---

### M18 — CashAccount, BankAccount ve FinancialTransaction temeli

**Durum:** Payments, Collections, Cash ve Bank birleşik milestone'u kapsamında tamamlandı (2026-07-26) — Kasa/banka master kayıtları, hareketlerden türetilen bakiye ve ekstreler, onaylı manuel hareket tipleri ve atomik banka transferi uygulandı.

**Kapsam**

- CashAccount ve BankAccount master kayıtları.
- Immutable FinancialTransaction ledger.
- Cash/bank bakiyesini hareketlerden türetmek.
- Transfer in/out ve genel finansal hareket tipleri.

**Karşılanan spesifikasyon gereksinimleri**

- Cash, bank ve financial transaction yapıları.
- Bakiyenin doğrudan değiştirilmemesi.
- FinancialTransaction'ın source of truth olması.

**Kapsam dışı**

- Invoice PaymentAllocation.
- Provider payment.
- Expense payment.
- Bank integration.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/CashAndBank/CashAccount.cs`
- `src/ECommerce.Domain/Accounting/CashAndBank/BankAccount.cs`
- `src/ECommerce.Domain/Accounting/CashAndBank/FinancialTransaction.cs`
- `src/ECommerce.Domain/Accounting/CashAndBank/FinancialTransactionType.cs`
- Application repository/command/query/handler/DTO dosyaları.
- Persistence config/repository dosyaları.
- `src/ECommerce.API/Controllers/Accounting/CashAccountsController.cs`
- `src/ECommerce.API/Controllers/Accounting/BankAccountsController.cs`
- İlgili unit/integration testleri.

**Değişmesi gerekebilecek mevcut dosyalar**

- Accounting DI registration.

**Core değişiklik ihtimali**

- Hayır.

**Kabul kriterleri**

- Bakiye doğrudan set edilemez.
- Her hareket tek finansal hesaba ve source'a bağlıdır.
- Transfer çift kayıtlarının atomic/idempotent tasarımı hazırdır.
- Currency uyumu doğrulanır.

**Unit testler**

- Cash in/out ve bank transfer direction.
- Negatif/sıfır amount.
- Account activation.

**Integration testler**

- Ledger balance.
- Source unique index.
- Atomic transfer.
- API authorization.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.CashAndBank"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting.CashAndBank"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Transfer iki hareket gerektiriyorsa tek source idempotency tasarımı dikkat ister.

**Çözümlenmemiş kararlar**

- Kasa/banka hesaplarının currency modeli.
- Opening balance'ın finansal hareket türü.
- POS ve marketplace hesaplarının BankAccount mı ayrı hesap tipi mi olduğu.

**Önceki milestone bağımlılığı**

- M01.

---

### M19 — Accounting Payment, collection ve PaymentAllocation

**Durum:** Payments, Collections, Cash ve Bank birleşik milestone'u kapsamında tamamlandı (2026-07-26) — PaymentAllocation doğrudan CurrentAccountTransaction hedefleyecek şekilde revize edildi; faturasız/faturalı müşteri tahsilatı, tedarikçi ödemesi, kısmi/çoklu tahsis, idempotency ve finansal/cari etkiler tamamlandı.

**Kapsam**

- Mevcut Order Payment'tan ayrı Accounting ödeme/tahsilat belgesi.
- CustomerCollection ve SupplierPayment.
- Bir payment'ın birden fazla invoice'a, bir invoice'ın birden fazla payment'a allocation'ı.
- PaidAmount/RemainingAmount değerlerini allocation'lardan üretmek.
- CurrentAccount ve FinancialTransaction etkilerini tek transaction'da kaydetmek.

**Karşılanan spesifikasyon gereksinimleri**

- Payment ve invoice ayrımı.
- Partial/multiple payment.
- PaymentAllocation.
- Paid/Remaining amount source.
- Customer collection/supplier payment.
- Cash/bank financial effect.

**Kapsam dışı**

- Harici provider entegrasyonu.
- Existing Order Payment entity değişikliği.
- Refund provider.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/Payments/AccountingPayment.cs`
- `src/ECommerce.Domain/Accounting/Payments/PaymentAllocation.cs`
- `src/ECommerce.Domain/Accounting/Payments/AccountingPaymentType.cs`
- Application repository/command/query/service/DTO/validator dosyaları.
- Persistence configuration/repository dosyaları.
- `src/ECommerce.API/Controllers/Accounting/PaymentsController.cs`
- İlgili unit/integration testleri.

**Değişmesi gerekebilecek mevcut dosyalar**

- PurchaseInvoice/SalesInvoice paid/remaining projection dosyaları.
- CurrentAccount ve FinancialTransaction Accounting servisleri.

**Core değişiklik ihtimali**

- Hayır; mevcut `Payment` değiştirilmez.

**Kabul kriterleri**

- Allocation toplamı Payment amount'u aşamaz.
- Invoice allocation toplamı remaining amount'u aşamaz.
- Paid/Remaining arbitrary set edilemez.
- Current account ve financial transaction etkileri atomiktir.
- Aynı idempotency/source ikinci ödeme etkisi oluşturmaz.

**Unit testler**

- Partial, full ve multi-invoice allocation.
- Over-allocation.
- CustomerCollection/SupplierPayment direction.

**Integration testler**

- PaymentAllocation FK/constraints.
- Concurrent allocation.
- Paid/Remaining projection.
- CurrentAccount+FinancialTransaction rollback.
- API idempotency/auth.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.Payments"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting.Payments"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Mevcut Order Payment ile Accounting collection çift kaydı oluşabilir.
- Çoklu currency allocation kur çevrimi gerektirir.

**Çözümlenmemiş kararlar**

- Existing Order Payment başarı kaydının otomatik Accounting collection üretip üretmeyeceği.
- Cross-currency allocation.
- Unallocated payment/advance desteği.

**Önceki milestone bağımlılığı**

- M04, M17 ve M18.

---

### M20 — Genel Expense ve ExpenseCategory

**Durum (2026-07-27):** Tamamlandı. Genel giderler stok maliyetinden ayrılmış; kategori, komut, sorgu, doğrulama ve endpoint yüzeyi eklenmiştir.

**Kapsam**

- Genel işletme giderleri ve kategorileri.
- Giderin cari/financial source ilişkisi.
- ExpensePayment için FinancialTransaction entegrasyonu.
- PurchaseInvoice'a maliyet dağıtılmayan giderleri ayrı tutmak.

**Karşılanan spesifikasyon gereksinimleri**

- Expense ve ExpenseCategory.
- Genel gider ile purchase-related gider ayrımı.
- ExpensePayment finansal hareketi.

**Kapsam dışı**

- PurchaseInvoice final cost dağıtımı; M09'da ele alınmıştır.
- Bordro/sabit kıymet/amortisman.
- Harici e-fatura entegrasyonu.

**Beklenen yeni dosyalar**

- `src/ECommerce.Domain/Accounting/Expenses/Expense.cs`
- `src/ECommerce.Domain/Accounting/Expenses/ExpenseCategory.cs`
- Application command/query/repository/service/DTO/validator dosyaları.
- Persistence configuration/repository dosyaları.
- `src/ECommerce.API/Controllers/Accounting/ExpensesController.cs`
- İlgili unit/integration testleri.

**Değişmesi gerekebilecek mevcut dosyalar**

- M18 FinancialTransaction servisi.
- Gerekirse M09 PurchaseInvoiceExpense bağlantı DTO'ları.

**Core değişiklik ihtimali**

- Hayır.

**Kabul kriterleri**

- Genel gider stok maliyetini kendiliğinden değiştirmez.
- Yalnız açıkça bağlanan PurchaseInvoiceExpense maliyete dahil olur.
- Paid expense atomik FinancialTransaction oluşturur.
- Posted/paid expense normal update ile bozulamaz.

**Unit testler**

- Category/amount/currency.
- Expense lifecycle.
- Purchase-related/general distinction.

**Integration testler**

- Expense/payment transaction atomicity.
- Category FK.
- API authorization/filter.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.Expenses"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting.Expenses"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Aynı giderin hem general expense hem PurchaseInvoiceExpense olarak iki kez maliyetleştirilmesi.

**Çözümlenmemiş kararlar**

- Expense approval/posting lifecycle.
- Tax/VAT handling.
- Recurring expenses.

**Önceki milestone bağımlılığı**

- M18; Purchase link kullanılacaksa M09.

---

### M21 — Accounting raporları

**Durum (2026-07-27):** Tamamlandı. AccountingSalesOrder birincil satış kaynağı korunarak faturalı/faturasız satış, FIFO, değerleme, kârlılık, cari, ödeme, kasa/banka ve KDV raporları eklenmiştir.

**Kapsam**

- Purchase/Sales invoice list/detail sorgularını rapor ihtiyaçlarına göre tamamlamak.
- Maliyeti kesinleşmemiş ve kısmi tahsisli stok hareketleri.
- CostLayer, remaining quantity, cost history ve stock valuation.
- Invoice/AccountingSalesOrder/product profitability.
- Current account, receivable/debt/overdue.
- Payment, cash, bank ve VAT özetleri.

**Karşılanan spesifikasyon gereksinimleri**

- Spesifikasyondaki bütün reporting listesi.
- Projection, pagination ve stable ordering.

**Kapsam dışı**

- Veri ambarı/BI.
- Stream processing.
- Accounting aggregate değişikliği.
- Gerçek Warehouse yokken depo bazlı doğru fiziksel rapor.

**Beklenen yeni dosyalar**

- `src/ECommerce.Application/Accounting/Reports/Queries/GetUncostedStockMovements/*`
- `.../GetPartiallyAllocatedStockMovements/*`
- `.../GetStockValuation/*`
- `.../GetInvoiceProfitability/*`
- `.../GetOrderProfitability/*`
- `.../GetProductProfitability/*`
- `.../GetVatSummary/*`
- `.../GetPaymentReport/*`
- `.../GetCashMovements/*`
- `.../GetBankMovements/*`
- `src/ECommerce.Application/Accounting/Reports/Interfaces/IAccountingReportReader.cs`
- `src/ECommerce.Persistence/Accounting/Repositories/AccountingReportReader.cs`
- `src/ECommerce.API/Controllers/Accounting/AccountingReportsController.cs`
- İlgili query/unit/integration/performance test dosyaları.

**Değişmesi gerekebilecek mevcut dosyalar**

- Accounting repository projection'ları ve DI registration.

**Core değişiklik ihtimali**

- Hayır.
- Warehouse bazlı doğru rapor için önce ayrı core Warehouse onayı gerekir.

**Kabul kriterleri**

- Bütün listeler bounded pagination ve stable ordering kullanır.
- Read query'ler gereksiz entity graph yüklemez.
- Totals ledger/source tablolardan türetilir.
- Currency karıştırılmaz.
- PII yalnız yetkili detay sorgularında döner.

**Unit testler**

- Report projection hesapları.
- Profit/VAT/current account totals.
- Filter boundary.

**Integration testler**

- Her raporun representative veri seti.
- SQL query count/performance sınırları.
- Authorization.
- Pagination.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build --filter "FullyQualifiedName~Accounting.Reports"
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build --filter "FullyQualifiedName~Accounting.Reports"
git diff --check
git diff --name-only -- src/ECommerce.Persistence/Migrations
```

**Riskler**

- Büyük ledger tablolarında yanlış index sorgu maliyetini yükseltir.
- Warehouse report mevcut modelle tam doğru üretilemez.

**Çözümlenmemiş kararlar**

- Raporların base currency dönüşümü.
- Tarih/timezone sınırları.
- Export formatları.

**Önceki milestone bağımlılığı**

- M10, revised M11 ve M17–M20.

---

### M22 — Migration ve final SQL Server integration doğrulaması

**Kapsam**

- Yalnız bütün Accounting modeli onaylandıktan sonra migration oluşturmak.
- Model snapshot ve SQL Server şemasını doğrulamak.
- Bütün spec testlerini gerçek ilişkisel davranışla çalıştırmak.
- Concurrency, idempotency, transaction rollback ve performans doğrulaması.

**Karşılanan spesifikasyon gereksinimleri**

- Database schema, constraints ve indexes.
- Test listesi 1–55.
- SQL Server concurrency.
- Final transaction/idempotency doğrulaması.

**Kapsam dışı**

- Yeni iş kuralı eklemek.
- Core refactor.
- Production deployment veya migration apply; ayrıca onaylanmadıkça.

**Beklenen yeni dosyalar**

- `src/ECommerce.Persistence/Migrations/<timestamp>_AddAccountingModule.cs`
- İlgili designer dosyası.
- SQL Server test fixture ve final end-to-end Accounting test dosyaları.

**Değişmesi gerekebilecek mevcut dosyalar**

- `src/ECommerce.Persistence/Migrations/AppDbContextModelSnapshot.cs`
- Gerekirse test project configuration.

**Core değişiklik ihtimali**

- Muhasebe modeli için hayır; migration mevcut Persistence altyapısını kullanır.
- Önceki milestone'lardan onaylanmamış core değişiklik bu milestone'a taşınamaz.

**Kabul kriterleri**

- Tek kontrollü Accounting migration'ı beklenen tabloları, FK, index ve check constraint'leri üretir.
- Migration mevcut tablolarda istenmeyen drop/rename oluşturmaz.
- Purchase posting StockMovement eklemez.
- Accounting sales posting stock-out'u AccountingSale olarak mevcut StockMovement altyapısı üzerinden üretir.
- Onaylı Phase 3 ve revised Accounting Sales test matrisi geçer.
- Concurrent allocation/FIFO overrun üretmez.
- Down script yalnız oluşturulan Accounting şemasını güvenli geri alır; mevcut core veriye zarar vermez.

**Unit testler**

- Tüm Accounting unit suite.

**Integration testler**

- Tüm Accounting integration suite.
- SQL Server-specific concurrent allocation.
- SQL Server-specific concurrent FIFO.
- Full Purchase/Sales posting rollback.
- API auth/authorization.
- Migration up/down model doğrulaması.

**Build ve doğrulama komutları**

```powershell
dotnet build ECommerce.sln
dotnet test tests/ECommerce.UnitTests/ECommerce.UnitTests.csproj --no-build
dotnet test tests/ECommerce.IntegrationTests/ECommerce.IntegrationTests.csproj --no-build
dotnet ef migrations script --project src/ECommerce.Persistence --startup-project src/ECommerce.API
git diff --check
git status --short
git diff --stat
git diff -- src/ECommerce.Persistence/Migrations
```

Migration oluşturma ve veritabanına uygulama komutları bu milestone açıkça onaylanmadan çalıştırılmaz.

**Riskler**

- Büyük migration ve index oluşturma süresi.
- SQL Server ile SQLite test farkları.
- Production verisinin backfill gereksinimleri.
- Mevcut kullanıcı değişiklikleriyle model snapshot çakışması.

**Çözümlenmemiş kararlar**

- Migration adlandırması ve tek/çok migration politikası.
- LocalDB/Testcontainers/harici SQL Server test ortamı.
- Production apply ve rollback prosedürü.

**Önceki milestone bağımlılığı**

- M01–M10, revised M11 ve M15–M21'in onaylanmış ve tamamlanmış olması.

## 8. Gereksinim izlenebilirlik tablosu

| Spesifikasyon gereksinimi | → Uygulama milestone'u | → Beklenen entity/service | → Beklenen command veya query | → Beklenen test |
|---|---|---|---|---|
| Accounting kodu ayrı klasör/namespace altında kalmalı | M01 | Accounting Common yapıları | Yok | Namespace/dependency architecture testi |
| Product/ProductVariant/StockMovement ve e-ticaret Order/OrderItem kopyalanmamalı | M01, M05, M11 | ID tabanlı adapter/readers ve ayrı Accounting aggregate'ları | İlgili create/post komutları | Assembly/model taramasında duplicate entity bulunmaması |
| Draft/Posted/Cancelled belge durumları | M01, M05, M11, M15, M16 | InvoiceStatus ve Accounting aggregate'ları | Create/Update/Post/Cancel commands | Yaşam döngüsü ve posted immutability testleri |
| Frontend toplamlarına güvenilmemeli | M02, M05, M11 | InvoiceCalculationService | Create/Update invoice commands | Gönderilen fake totals'ın kullanılmaması |
| Ortak header toplamları satır toplamlarıyla eşleşmeli | M02 | InvoiceCalculationResult/InvoiceTotals | Calculation service | Spec testleri 40–43 |
| KDV hariç ve dahil fiyat girişi | M01, M02 | PriceEntryMode, VatRateSnapshot | Calculation service | Spec testleri 30–31 |
| Percentage/FixedPerUnit/FixedLineTotal/FixedInvoiceTotal | M01, M02 | DiscountDefinition | Calculation service | Spec testleri 32–39 |
| Invoice discount oransal ve deterministik dağıtılmalı | M02 | InvoiceCalculationService | Calculation service | Spec testleri 35–39 |
| Decimal precision ve merkezi rounding | M01, M02 | AccountingPrecision/RoundingPolicy | Calculation service | Scale ve final-line rounding testleri |
| Supplier rolündeki cari hesap mevcut olmalı | M03 | CurrentAccount | CreateCurrentAccountCommand/GetCurrentAccountsQuery | CurrentAccount persistence ve validation |
| Accounting satış müşterisi Customer/CustomerAndSupplier rolündeki CurrentAccount olmalı; User/cart bağımlılığı olmamalı | M03, M11 | CurrentAccount ve AccountingSalesOrder | CreateAccountingSalesOrderCommand | Spec testleri 15–16 ve role validation |
| Cari bakiye hareketlerden türetilmeli | M04, M17 | CurrentAccountTransaction | GetCurrentAccountStatementQuery | Debit-credit projection |
| Purchase posting supplier debt oluşturmalı | M04, M10 | CurrentAccountTransactionService | PostPurchaseInvoiceCommand | Supplier transaction exactly once |
| Accounting sales posting customer receivable oluşturmalı | M04, M11 | CurrentAccountTransactionService | PostAccountingSalesOrderCommand | Spec test 27 ve idempotency testi |
| PurchaseInvoice draft StockMovement oluşturmamalı | M05 | PurchaseInvoice | Create/UpdatePurchaseInvoiceCommand | Spec test 1 |
| CurrentAccountId+InvoiceNumber unique olmalı | M05 | PurchaseInvoice config | CreatePurchaseInvoiceCommand | Spec test 9 |
| PurchaseInvoiceLine quantity/unit/snapshot/fiyat/KDV alanları | M05 | PurchaseInvoiceLine | Create/UpdatePurchaseInvoiceCommand | Line invariant/persistence testleri |
| PurchaseInvoiceStockAllocation mapping table kullanılmalı | M06 | PurchaseInvoiceStockAllocation | SetPurchaseInvoiceAllocationsCommand | Spec testleri 3–8 |
| StockMovement kısmi ve çoklu tahsis edilebilmeli | M06 | Allocation repository/service | Set allocations/GetAvailableStockMovementsQuery | Multiple/partial allocation tests |
| Aynı stok miktarı iki kez maliyetlenmemeli | M06 | Allocation reader + unique/concurrency policy | Set/Post commands | Concurrent over-allocation testi |
| Yalnız uygun pozitif hareketler tahsis edilmeli | M06 | IAccountingStockMovementReader | GetAvailableStockMovementsQuery | Eligible type/wrong direction testi |
| Purchase allocation StockMovement'ı değiştirmemeli | M06, M10 | Accounting-side mapping | Set/PostPurchaseInvoice | Stock count/balance unchanged |
| InventoryCostLayer stok kaynağı olmamalı | M07 | InventoryCostLayer | GetCostLayersQuery | Layer create sırasında stock unchanged |
| Final VAT hariç maliyet primary valuation olmalı | M07, M09 | InventoryCostLayerFactory | PostPurchaseInvoiceCommand | Correct unit/total cost |
| ProductVariantCostHistory raporlama kaydı olmalı | M08 | ProductVariantCostHistoryService | GetCostHistoryByVariantQuery | Active history close/open |
| Purchase gideri maliyete dağıtılmalı | M09 | PurchaseInvoiceExpense/AllocationService | SetPurchaseInvoiceExpensesCommand | Dağıtım ve rounding testi |
| Purchase posting StockMovement oluşturmamalı | M10 | PurchaseInvoicePostingService | PostPurchaseInvoiceCommand | Spec test 2 ve stock count assertion |
| Purchase posting tek transaction olmalı | M10 | IUnitOfWork + posting service | PostPurchaseInvoiceCommand | Spec test 52 |
| Purchase posting idempotent olmalı | M10 | Source unique constraints | PostPurchaseInvoiceCommand | Spec testleri 10, 12 ve 55 |
| Draft AccountingSalesOrder stok veya cari etki oluşturmamalı | M11 | AccountingSalesOrder | Create/UpdateAccountingSalesOrderCommand | Spec testleri 13–14 |
| Accounting satış satırları request ProductVariantId/quantity değerlerinden gelmeli | M11 | AccountingSalesOrderItem | Create/UpdateAccountingSalesOrderCommand | Spec test 17 |
| E-ticaret Order yalnız User/cart checkout'a ait kalmalı ve Accounting tarafından oluşturulmamalı | M11 | Ayrı AccountingSalesOrder aggregate'ı | PostAccountingSalesOrderCommand | Spec testleri 20–21 ve checkout regresyonu |
| Accounting stock-out mevcut StockMovement altyapısında AccountingSale=22 olmalı | M11 | AccountingSalesOrderPostingService | PostAccountingSalesOrderCommand | Spec test 18 ve direction/reference testleri |
| StockMovement'a Accounting FK eklenmeden item/movement mapping kurulmalı | M11 | AccountingSalesOrderStockMovement | Internal stock-out operation | Mapping FK/variant/quantity integration testleri |
| Tek örtük depo kullanılmalı; Accounting sales API/modelinde WarehouseId olmamalı | M11 | AccountingSalesOrder/Item ve CostLayerConsumption | Sales commands | Yapısal bağımsızlık ve request validation testleri |
| SalesInvoice optional olmalı ve AccountingSalesOrder'a bire-bir bağlanmalı | M11 | SalesInvoice.AccountingSalesOrderId unique FK | CreateSalesInvoice/CreateAccountingSalesOrder commands | Spec testleri 22–25 |
| SalesInvoice doğrudan StockMovement veya ikinci receivable oluşturmamalı | M11 | SalesInvoice ve posting dependency boundary | Create/Post sales commands | Spec testleri 24, 26–27 |
| FIFO oldest open layer'ı deterministik tüketmeli | M11 | FifoCostLayerConsumptionService | Internal consume operation | Spec testleri 44–46 |
| FIFO gerçek AccountingSalesOrderItem ve mapped AccountingSale StockMovement'a bağlanmalı | M11 | CostLayerConsumption | Internal consume operation | AccountingSalesOrderItemId/StockMovementId mapping testi |
| Concurrent sale layer'ı aşırı tüketmemeli | M11 | CostLayer concurrency policy | Internal consume operation | Spec test 51 |
| Sales COGS consumption toplamından gelmeli | M11 | SalesProfitabilityService | CalculateSalesProfitability internal operation | Spec testleri 47–48 |
| Gross profit/margin doğru hesaplanmalı | M11 | SalesProfitabilityService | CalculateSalesProfitability internal operation | Spec testleri 49–50 |
| Accounting sales posting exactly-once ve tam rollback sağlamalı | M11 | AccountingSalesOrderPostingService | PostAccountingSalesOrderCommand | Spec testleri 19, 29 ve 53–55 |
| Harici e-fatura entegrasyonu internal SalesInvoice'dan ayrı ve sonraki kapsam olmalı | M11 | Scope/dependency boundary | Bu milestone'da command yok | Provider bağımlılığı bulunmaması testi |
| Purchase cancellation stok hareketi oluşturmamalı | M15 | PurchaseInvoiceCancellationService | CancelPurchaseInvoiceCommand | Stock count unchanged/reversal testi |
| Tüketilmiş CostLayer güvenli yönetilmeli | M15 | Onaylı adjustment/reversal policy | CancelPurchaseInvoiceCommand | Consumed-layer cancellation testi |
| Accounting sales cancellation ileride onaylı AccountingSale reversal kullanmalı | M16 | AccountingSalesOrderCancellationService | CancelAccountingSalesOrderCommand | E-ticaret Order/Return bağımsızlığı ve reversal testi |
| Accounting sales cancellation receivable ters kaydı oluşturmalı | M16 | CurrentAccountTransactionService | CancelAccountingSalesOrderCommand | Reversal exactly once |
| Current account statement/receivable/debt/overdue | M17 | Accounting query reader | Statement/Receivable/Debt queries | Projection/filter tests |
| Cash/bank balance FinancialTransaction'dan gelmeli | M18 | CashAccount/BankAccount/FinancialTransaction | Cash/Bank commands and queries | Ledger balance/atomic transfer |
| Invoice ve Payment ayrı olmalı | M19 | AccountingPayment | CreateAccountingPaymentCommand | Invoice lifecycle'dan bağımsız payment |
| Partial/multiple payment allocation | M19 | PaymentAllocation | AllocatePaymentCommand | Partial/multi-invoice tests |
| PaidAmount/RemainingAmount allocation'lardan gelmeli | M19 | Payment allocation projection | Get invoice queries | Paid/remaining consistency |
| Genel gider ve purchase gideri ayrılmalı | M09, M20 | PurchaseInvoiceExpense/Expense | Expense commands | Double-allocation prevention |
| Accounting audit aktörleri ve source ilişkileri | M05, M10, M11, M15–M20 | Explicit audit/source fields | Post/Cancel/Create commands | Actor/time/source persistence |
| API CQRS/MediatR ve ince controller kullanmalı | İlgili tüm API milestone'ları | Thin controllers/handlers | Accounting endpoints | Controller sender-delegation tests |
| Validation gereksinimleri | M01, M02, M05, M06, M11, M19 | Value objects + FluentValidation | İlgili commands | Boundary/invalid input testleri |
| Reporting listesi | M17, M21 | IAccountingReportReader | Report queries | Projection/pagination/auth tests |
| Migration ve bütün 55 test | M22 | EF model + SQL Server fixture | Migration verification | Spec test matrisi 1–55 |

## 9. Milestone onay matrisi

| Milestone | Uygulama öncesi özel karar/onay |
|---|---|
| M01 | Currency, scale ve UnitOfMeasure yönü |
| M02 | Discount eligible base ve rounding ayrıntıları |
| M03 | CurrentAccount zorunlu alanları ve benzersizlik |
| M04 | Tek hesap altında currency bazlı hareket raporlama modeli |
| M05 | Duplicate line, snapshot zamanı ve quantity modeli |
| M06 | Eligible StockMovementType listesi; integer StockQuantity |
| M07 | Tek depo veya gerçek Warehouse kararı |
| M08 | History partition ve aynı maliyet davranışı |
| M09 | Gider giriş/KDV ve allocation yöntemi |
| M10 | Eksik allocation ve idempotent response politikası |
| M11 | **Onaylandı:** ayrı AccountingSalesOrder, AccountingSale=22, mapping, optional SalesInvoice ve tek örtük depo/WarehouseId yok. **Uygulama öncesi netleşecek:** kesin maliyeti olmayan stok, duplicate variant satırı, fiyat/currency/quantity sözleşmesi ve margin politikası |
| M15 | Tüketilmiş CostLayer cancellation stratejisi |
| M16 | AccountingSale stok reversal, FIFO cost restoration ve receivable reversal stratejisi |
| M17 | Accounting authorization ve consolidated balance |
| M18 | Financial account currency ve transfer modeli |
| M19 | Existing Order Payment senkronizasyonu ve cross-currency allocation |
| M20 | Expense lifecycle ve vergi yaklaşımı |
| M21 | Warehouse report, timezone ve export kapsamı |
| M22 | Migration/test ortamı ve database apply onayı |

## 10. Çözümlenmemiş iş kararları

Revised M11 için şu mimari kararlar çözülmüş ve onaylanmıştır:

- E-ticaret Order/OrderItem yalnız User/cart checkout'a aittir; Accounting sales bunları oluşturmaz, çağırmaz veya değiştirmez.
- AccountingSalesOrder CurrentAccountId kullanır; UserId, Cart, e-ticaret Address ve shipping alanlarına dayanmaz.
- Accounting stock-out, `AccountingSale = 22` ile mevcut StockMovement altyapısından çıkar ve `AccountingSalesOrderStockMovement` ile eşlenir.
- SalesInvoice opsiyoneldir; AccountingSalesOrder'a bire-bir bağlanır ve ikinci stok/cari etki oluşturmaz.
- Bu milestone tek örtük depo kullanır; Warehouse ve WarehouseId eklenmez.
- Harici e-fatura entegrasyonu sonraki kapsamdır.

Çözülmemiş iş kararları:

1. Kesin maliyeti olmayan stok satışı engellenecek mi, provisional cost mu kullanılacak, yoksa pending cost/revaluation mı yapılacak?
2. PurchaseInvoice allocation için hangi pozitif StockMovementType değerleri uygundur?
3. OpeningBalance stoklarının başlangıç maliyeti nasıl atanacaktır?
4. PurchaseQuantity/UnitsPerUnit decimal olabilirken mevcut StockQuantity int kısıtı nasıl korunacaktır?
5. Aynı ProductVariant bir AccountingSalesOrder içinde birden fazla satır olarak bulunabilir mi?
6. Supplier için zorunlu master alanları ve benzersiz anahtarlar nelerdir?
7. AccountingSalesOrder/SalesInvoice fiyat, currency ve quantity precision sözleşmesi nedir?
8. Accounting request fiyatı mı kullanılacak, yoksa katalog fiyatından kontrollü türetme mi yapılacaktır?
9. Sıfır net satış margin sonucu, negatif margin rounding'i ve foreign-currency profitability tarihi ne olacaktır?
10. PurchaseInvoice, tahsis miktarı satır StockQuantity'den eksikken post edilebilir mi?
11. PurchaseInvoice iptalinde CostLayer tüketilmişse block, cost adjustment veya reversing entry seçeneklerinden hangisi kullanılacaktır?
12. İade maliyeti orijinal CostLayer'a mı dönecek, yeni reversal layer mı oluşturacaktır?
13. Existing Order Payment başarılı olduğunda otomatik Accounting collection kaydı oluşacak mıdır?
14. Cross-currency PaymentAllocation desteklenecek midir?
15. Accounting endpointleri yalnız Admin mi, yoksa Accountant/Finance gibi yeni roller mi kullanacaktır?
16. Invoice number kullanıcı girişi mi, sistem sequence'i mi olacaktır?
17. InvoiceDate, PostedAt ve CostDate ilişkisi nasıl olacaktır?
18. Purchase expense allocation ilk sürümde yalnız VAT-exclusive line amount proportional mı olacaktır?
19. Gerçek payment provider refund ile Accounting reversal hangi sırada yürütülecektir?

## 11. Açık onay gerektirebilecek core değişiklikleri

1. **Baseline TaxRate düzeltmesi**
   - Dosya: `src/ECommerce.Domain/Entities/TaxRate.cs`
   - İhtiyaç: Silinmiş `CalculateNetPrice` metodunun geri getirilmesi veya bütün çağrıların yeni bir core servise taşınması.
   - Etki: Düşük fakat Accounting dışı core değişiklik.

2. **AccountingSale StockMovement desteği — revised M11 için onaylandı**
   - Dosyalar: `StockMovementType.cs`, `StockMovement.cs`, `StockMovementConfiguration.cs` ve ilgili core testleri.
   - İhtiyaç: `AccountingSale = 22` negatif Out hareketi; e-ticaret Order referansı olmadan Accounting mapping ile izlenebilirlik.
   - Etki: Orta; mevcut `Sale`/checkout davranışı regresyon testleriyle korunmalıdır.

3. **Transaction composition — yalnız mevcut altyapı yetersizse ayrı onay**
   - Dosyalar: `IUnitOfWork`, `UnitOfWork` veya mevcut transaction abstraction'ı.
   - İhtiyaç: AccountingSalesOrder/AccountingSale mapping/FIFO/cari etkilerini tek commit'e katmak.
   - Etki: Orta-yüksek. Revised M11 önce mevcut transaction altyapısını kullanmalıdır; core transaction sözleşmesi değişikliği ayrıca onaylanmadan yapılamaz.

4. **Gerçek Warehouse desteği — revised M11 kapsamında yok**
   - Dosyalar: ProductVariant, StockMovement, stok repository/commands/configuration ve Order/Return stok akışları.
   - İhtiyaç: Gelecekte fiziksel çoklu depo istenmesi. Revised M11'in onaylı kararı tek örtük depo ve WarehouseId olmamasıdır.
   - Etki: Çok yüksek ve migration gerektirir.

5. **Decimal stok miktarı**
   - Dosyalar: ProductVariant, StockMovement ve bütün stok akışları.
   - İhtiyaç: Kesirli stock unit desteği.
   - Etki: Çok yüksek.

6. **Accounting sales cancellation/reversal desteği**
   - Dosyalar: StockMovement enum/reference/configuration ve AccountingSalesOrder/FIFO/current-account reversal dosyaları.
   - İhtiyaç: Gelecekte AccountingSale stok çıkışını ve AccountingSalesOrder receivable etkisini ters çevirmek.
   - Etki: Orta-yüksek.

7. **Accounting DI composition bağlantıları**
    - Dosyalar: Application/Persistence service registration veya Program.
    - İhtiyaç: Accounting servis ve repository'lerinin runtime DI kaydı.
    - Etki: Düşük; kayıtların içeriği Accounting extension dosyalarında izole tutulabilir.

8. **Accounting authorization rolü**
    - Dosyalar: UserRole, JWT role claims ve API authorization policy.
    - İhtiyaç: Admin dışında Accountant/Finance rolü istenirse.
    - Etki: Orta.

Yalnız 2 numaralı AccountingSale değişikliği revised M11 için açıkça onaylanmıştır. Diğer core değişiklikler ilgili milestone ve core değişiklik ayrı ayrı onaylanmadan uygulanamaz; e-ticaret Order/OrderItem/Cart/User/Address yapılarında değişiklik onaylanmamıştır.

## 12. Planın tamamlanma ölçütü

Accounting modülü ancak aşağıdakilerin tamamı sağlandığında tamamlanmış sayılır:

- M01–M10, tek revised M11 ve M15–M22 olmak üzere 19 bağımsız milestone ayrı ayrı onaylanmış ve kapanış raporları verilmiş olmalıdır.
- PurchaseInvoice posting'in StockMovement oluşturmadığı kanıtlanmalıdır.
- Draft AccountingSalesOrder'ın StockMovement, CostLayerConsumption veya customer receivable oluşturmadığı kanıtlanmalıdır.
- SalesInvoice'ın doğrudan StockMovement oluşturmadığı kanıtlanmalıdır.
- AccountingSalesOrder posting'in mevcut StockMovement altyapısıyla AccountingSale=22 çıkışı oluşturduğu ve e-ticaret Order oluşturmadığı kanıtlanmalıdır.
- AccountingSalesOrderStockMovement mapping'inin her stock-out'u doğru AccountingSalesOrderItem'a bağladığı kanıtlanmalıdır.
- Optional SalesInvoice'ın AccountingSalesOrder'a bire-bir bağlı olduğu ve ikinci stok/cari etki oluşturmadığı kanıtlanmalıdır.
- Her Posted AccountingSalesOrder için customer receivable etkisinin tam olarak bir kez oluştuğu kanıtlanmalıdır.
- FIFO consumption gerçek AccountingSalesOrderItem ve mapped AccountingSale StockMovement kayıtlarına bağlı olmalıdır.
- Retry'ın ikinci movement, mapping, consumption, receivable, AccountingSalesOrder veya SalesInvoice üretmediği kanıtlanmalıdır.
- Transaction/idempotency/concurrency test matrisi geçmelidir.
- Core entity duplicate'i bulunmamalıdır.
- Migration yalnız son onaylı model için oluşturulmalıdır.
- Tam unit ve integration test suite geçmelidir.
- Final diff incelemesinden sonra çalışma durdurulmalı; deployment veya database apply otomatik yapılmamalıdır.
