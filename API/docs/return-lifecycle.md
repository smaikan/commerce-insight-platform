# İade yaşam döngüsü

## Yeni kayıt akışı

Yeni `ReturnRequest` kayıtlarının karar sırası fiziksel teslimden sonradır:

```text
Requested (0) -> Received (3) -> Approved (1)
                             -> Rejected (2)
```

- `POST /api/returns/{id}/receive`, yalnız `Requested` kaydı `Received` yapar ve `ReceivedAt` alanını UTC yazar. Sipariş `ReturnRequested (8)` kalır; stok hareketi, ödeme/refund işlemi ve kupon işlemi oluşmaz.
- `POST /api/returns/{id}/approve`, yalnız karar bekleyen `Received` kaydı onaylar. `Refund` için sipariş `Refunded (7)` olur ve iade kalemleri birer `SaleReturn` stok girişi üretir. `Exchange` için sipariş `ReturnApproved (9)` olur; iade stok girişi ile replacement stok çıkışı aynı serializable transaction'da uygulanır.
- `POST /api/returns/{id}/reject`, yalnız karar bekleyen `Received` kaydı reddeder. Stok hareketi oluşmaz; sipariş durumu diğer aktif taleplerden yeniden türetilir.

Onay akışı yalnız iş durumu ve stok ledger'ını değiştirir. Ödeme sağlayıcısına otomatik para iadesi çağrısı yapılmaz ve `Payment` kaydı değiştirilmez.

## Sipariş durumu türetme

Öncelik sırası şöyledir:

1. En az bir onay sonucu bulunan `Refund` talebi: `Refunded (7)`.
2. En az bir onay sonucu bulunan `Exchange` talebi: `ReturnApproved (9)`.
3. En az bir `Requested` veya karar bekleyen yeni `Received` talebi: `ReturnRequested (8)`.
4. Yalnız reddedilmiş talepler: `Delivered (5)`.

Bir talebin reddi diğer aktif taleplerin sonucunu silmez. Sipariş durum bildirimi yalnız türetilen durum gerçekten değiştiğinde kuyruğa alınır. Return durum bildirimi `returnId + status` anahtarıyla tekilleştirilir.

## Stok ve transaction güvenliği

- Refund stok girişi ve exchange giriş/çıkışları karar onayıyla aynı transaction'dadır.
- `StockMovement` tekillik indeksi, aynı `ReturnRequest + ProductVariant + Type` için yinelenen hareketi engeller.
- Tekrarlanan veya yarışan karar çağrıları ikinci bir stok/outbox etkisi üretmez. Geçersiz durum geçişi `409 return_status_transition_invalid`, gerçek EF concurrency yarışı `409 concurrency_conflict` döndürür.
- Exchange replacement doğrulaması veya stok işlemi başarısızsa talep, sipariş ve tüm stok etkileri rollback olur.

## Eski kayıt uyumluluğu

Enum değerleri ve `POST /api/returns/{id}/complete` kaldırılmamıştır. Deployment öncesi akışta `ApprovedAt` fiziksel teslimden önce yazıldığı için eski kayıtlar bounded bir uyumluluk dalıyla ayırt edilir:

- Eski `Approved` kayıt `receive` ile `Received` olabilir.
- Eski `Received` kayıt `complete` ile `Completed` olabilir.
- Eski onaylı `Received/Completed` kayıt sipariş durumunu geriye düşürmez.
- Yeni kayıt `Requested -> Approved` veya yeni `Approved -> Received -> Completed` yoluna giremez; yeni kayıtlarda `complete` kullanılmaz.

Şema ve enum değişmediği için bu değişiklik için migration yoktur. Uygulama öncesi veri incelemesinde mevcut `Received/Completed` kayıtların tamamında `ApprovedAt` bulunduğu doğrulanmıştır.
