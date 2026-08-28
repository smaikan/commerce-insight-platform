# GET /api/dashboard/work-queue-summary

- Görev alanı: **Operasyon → Dashboard**.
- Yetki: **Admin**.
- Başarılı cevap: `200 AdminWorkQueueSummaryDto`
- Cache: Yönetici verisi olduğu için ortak cache'e alınmaz.

Bu operasyon admin navigasyonunda işlem bekleyen sipariş ve yeni iletişim mesajı rozetlerini üretir. Sayaçlar kişiye özel “okundu” bilgisi değil, ortak operasyon kuyruğudur.

## Parametreler

Bu operasyon query veya request body almaz.

## Başarılı response

```json
{
  "ordersAwaitingProcessingCount": 4,
  "newContactMessageCount": 2,
  "generatedAtUtc": "2026-08-27T10:00:00Z"
}
```

## Sayaç kuralları

- `ordersAwaitingProcessingCount`, durumu `Pending`, `Confirmed` veya `Paid` olan siparişleri sayar.
- `Preparing` ve sonraki sipariş durumları bu kuyrukta görünmez.
- `newContactMessageCount`, yalnız `New` durumundaki iletişim mesajlarını sayar.
- Sayaçlar mevcut durum kolonları ve indeksleri üzerinden tek aggregate SQL gidişinde hesaplanır; ayrı bir boolean alan tutulmaz.
- `generatedAtUtc`, özetin üretildiği UTC anıdır.

## Hatalar

- `401`: Geçerli oturum yok.
- `403`: Kullanıcının Admin yetkisi yok.
- `500`/ağ/timeout: Admin arayüzü son başarılı sayaçları korur ve görünür sekmede yeniden dener.
