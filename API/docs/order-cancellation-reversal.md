# Sipariş iptali ve iyzico ters işlem sagası

## Kapsam

Müşteri iptali üye ve guest akışlarında ortak `OrderCancellationService` tarafından yürütülür. `Pending/Confirmed` siparişler mevcut CheckoutForm retrieve ve abandoned-token mutabakatını kullanır. `Paid/Preparing` siparişler yalnız iyzico tahsilatı doğrulanıp geri alındıktan sonra `Cancelled` olur. `Shipped` ve sonraki durumlarda provider çağrısı yapılmaz.

## Provider karar matrisi

| Yerel durum | Provider kararı | Sonuç |
| --- | --- | --- |
| `Pending/Confirmed` | Tahsilat yok veya kesin failure | Mevcut atomik stok/kupon/outbox iptali |
| `Pending/Confirmed` | Paid | Paid akışına geçilir; ardından reversal sagası uygulanabilir |
| `Paid/Preparing`, ödeme Türkiye iş tarihinde bugün | `/payment/cancel`, `paymentId` | Tam cancel |
| `Paid/Preparing`, önceki tarih veya cancel kesin reddi | Her kalem için `/payment/refund`, gerçek `paymentTransactionId + paidPrice` | Tam item-level refund |
| Provider timeout/kimliği doğrulanamayan mutasyon cevabı | Reporting ile yeniden kontrol | `ReconciliationPending`, yerel finansal/operasyonel etki yok |
| Reporting kimlik, currency veya tutar uyuşmazlığı | Otomatik işlem durur | `ManualReview`, yerel etki yok |

Çok ürünlü siparişlerde Refund V2 kullanılmaz. CF-Retrieve içindeki item transaction kimlikleri, `price` ve `paidPrice` değerleri başarılı ödeme transaction'ında `PaymentItemTransactions` tablosuna yazılır. Kalem `paidPrice` toplamı gerçek `ProviderPaidAmount` ile eşleşmeden refund operasyonu oluşturulmaz.

## Saga ve idempotency

- `OrderCancellationOperations`, sipariş ve ödemeye bağlı kalıcı intent/audit kaydıdır.
- `OrderCancellationOperationItems`, her standart refund kalemini ayrı ve deterministik conversation kimliğiyle izler.
- Aynı sipariş için `Requested/Processing/ReconciliationPending` durumlarında filtered unique index yalnız tek aktif operasyona izin verir.
- Provider çağrısı veritabanı transaction'ı dışında yapılır. Her retry öncesinde `/v2/reporting/payment/details` sorgulanır.
- Reporting sorgusu sandbox ve production ortak davranışı için yalnız URL-encode edilmiş `paymentId` ile yapılır. Üst seviye `conversationId` echo'su kimlik kanıtı sayılmaz; bazı sandbox cevaplarında `paymentId` ve `paymentTransactionId` numeric geldiğinden değerler string/numeric wire biçiminden kanonik metne çevrilir.
- Reporting kimliği; kalıcı provider `paymentId`, ilk ödeme denemesinin `paymentConversationId`, `TRY`, sipariş ara toplamı ve gerçek tahsil edilen `paidPrice` birlikte doğrulanarak bağlanır. Reporting `basketId` alanı sandbox'ta ilk CheckoutForm basket kimliğiyle kararlı eşleşmediği için finansal karar değişmezi olarak kullanılmaz.
- Yalnız adapter ön-kontrol hatasıyla `provider_response_mismatch` manual review'a düşen ve Order/Payment hâlâ tahsil edilmiş durumda olan intent, worker veya tekrarlanan iptal isteğiyle yeni operasyon oluşturmadan toplam en fazla üç reporting doğrulamasına alınabilir. Limit dolduğunda ve diğer bütün manual-review nedenlerinde operasyon terminal kalır.
- İki dakikalık lease HTTP ve worker yarışını tekilleştirir; süresi dolmuş `Processing` operasyon/item bounded worker tarafından devralınabilir.
- Provider başarısından sonra Payment, Order, stok Cancellation hareketleri, kupon release, notification/outbox ve operasyon tamamlanması tek serializable transaction ve tek `SaveChanges` ile kalıcılaşır.
- Genel `Sipariş İptal Edildi` bildirimine ek olarak `PaymentReversalCompleted` outbox kaydı aynı transaction'da oluşturulur. `payment-reversal-completed:{operationId}` anahtarı ikinci e-postayı engeller. E-posta gerçek provider tahsilat tutarını taşır; SMTP teslimatı mevcut lease/retry/dead-letter worker'ında gerçekleşir ve HTTP/provider transaction'ını bekletmez.
- Aynı gün cancel sonucunda Payment `Cancelled`; standart refund sonucunda Payment `Refunded`; iki durumda da Order `Cancelled (6)` olur.
- Sepet yeniden oluşturulmaz. Ödeme öncesi iptal/başarısızlıkta mevcut sepet korunur.

## Mutabakat worker'ı

`OrderCancellationReconciliationBackgroundService` her 30 saniyede en fazla 25 zamanı gelmiş operasyonu işler. Application komutu batch boyutunu `1..100` aralığına sınırlar. Provider sonucu belirsizken Order/Payment/stok/kupon değişmez. Polling endpointi owner kapsamındadır ve provider kimliği ya da hata payload'ı açmaz.

## Muhasebe sınırı

Mevcut Accounting modülü kendi `AccountingSalesOrder` ve Accounting `Payment` aggregate'larıyla çalışır; e-commerce `Order`/`Payment` kayıtlarına otomatik kaynak bağı yoktur. Bu nedenle cancellation sagası uydurma bir kasa, banka, cari veya fatura kaydı üretmez. Provider ters işlem audit'i cancellation operation ve item kayıtlarında kalıcıdır. Canlı muhasebe defterinde otomatik refund kaydı zorunluysa e-commerce ödeme ile Accounting belge/hesap eşlemesi ürün kararıyla ayrıca kurulmadan bu özellik muhasebe açısından go-live edilmemelidir.

## Eski ödeme kayıtları

Migration öncesi başarılı ödemelerde item transaction snapshot'ı bulunmayabilir. Aynı gün `paymentId` cancel çalışabilir; standart refund gerektiğinde API güvenli biçimde `409 payment_reversal_data_missing` döndürür ve otomatik tutar tahmini yapmaz.
