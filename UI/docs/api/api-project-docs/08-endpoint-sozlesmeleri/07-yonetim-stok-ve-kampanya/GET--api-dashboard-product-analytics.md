# GET /api/dashboard/product-analytics

- İşlev alanı: **07 Yönetim, stok ve kampanya**
- Yetki: **AdminOnly**
- Başarılı cevap: `200 DashboardProductAnalyticsDto`

## Parametreler

| Ad | Konum | Zorunlu | Şema |
| --- | --- | --- | --- |
| `from` | query | Evet | string (date, `YYYY-MM-DD`) |
| `to` | query | Evet | string (date, `YYYY-MM-DD`) |

`from` değeri `to` değerinden sonra olamaz. İki uç dahil istenen aralık en fazla 90 gündür. Gün sınırı UTC'dir; endpoint saat dilimi dönüşümü yapmaz.

## Başarılı response

```json
{
  "from": "2026-08-01",
  "to": "2026-08-07",
  "clickCount": 12400,
  "addToCartCount": 890,
  "purchaseCount": 164,
  "favoriteCount": 205,
  "ratingCount": 31,
  "reviewCount": 12,
  "dailyMetrics": [
    {
      "date": "2026-08-01",
      "clickCount": 1800,
      "addToCartCount": 124,
      "purchaseCount": 21,
      "favoriteCount": 34,
      "ratingCount": 4,
      "reviewCount": 2
    }
  ],
  "topProducts": [
    {
      "productId": "P00001",
      "title": "Örnek ürün",
      "mainSku": "SKU-001",
      "clickCount": 620,
      "addToCartCount": 71,
      "purchaseCount": 18
    }
  ],
  "generatedAtUtc": "2026-08-07T12:00:00Z"
}
```

Toplamlar ve `dailyMetrics` tüm ürünlerin günlük metriklerinden backend tarafından hesaplanır. `dailyMetrics` hareketsiz UTC günlerini de sıfır sayaçlarla içerir. `topProducts` en fazla beş üründür; `purchaseCount`, `addToCartCount`, `clickCount` azalan sırasını kullanır.

## Hatalar

- `400`: Eksik/geçersiz tarih, ters tarih aralığı veya 90 günü aşan aralık.
- `401`: Geçerli oturum yok.
- `403`: Kullanıcının Admin yetkisi yok.
