# GET /api/dashboard/overview

- İşlev alanı: **07 Yönetim, stok ve kampanya**
- Yetki: **Admin** (`AdminOnly`)
- Başarılı cevap: `200 DashboardOverviewDto`
- Cache: Yönetici verisi olduğu için ortak cache'e alınmaz.

## Parametreler

Bu operasyon query veya request body almaz.

## Başarılı response

```json
{
  "totalOrderCount": 186,
  "pendingOrderCount": 8,
  "paidOrderCount": 139,
  "paidRevenue": 245670.5,
  "activeProductCount": 74,
  "lowStockVariantCount": 6,
  "generatedAtUtc": "2026-08-05T19:30:00Z"
}
```

## Metrik anlamları

- `paidRevenue`, tamamlanmış refund iadeleri düşülmüş net tahsilattır; dönem filtresi olmadığı için tüm zamanlar toplamıdır.
- `lowStockVariantCount`, aktif, stok miktarı sıfırdan büyük ve yapılandırılmış düşük stok eşiğinin altında kalan varyant sayısıdır.
- `generatedAtUtc`, özetin üretildiği UTC anıdır.

## Hatalar

- `401`: Geçerli oturum yok.
- `403`: Kullanıcının Admin yetkisi yok.
- `500`/ağ/timeout: Frontend mevcut dashboard durumunu koruyup güvenli yeniden deneme sunar.
