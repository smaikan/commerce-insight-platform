# Ortak İstek ve Yanıt Kuralları

## Content type ve alan adları

JSON body gönderen isteklerde:

```http
Content-Type: application/json
Accept: application/json
```

JSON alan adları `camelCase`, query parametreleri ise endpoint sözleşmesinde gösterilen adlarla gönderilir. Query adları çoğunlukla `PageNumber`, `PageSize`, `Search`, `Status`, `CreatedFromUtc` biçimindedir.

## Kimlik biçimleri

| Kaynak | Dışarıdan görünen kimlik |
| --- | --- |
| Kullanıcı | `U` önekli büyük harfli Base36, örn. `U00001` |
| Ürün | `P` önekli büyük harfli Base36, örn. `P00001` |
| Sipariş | UUID/GUID; ayrıca kullanıcıya gösterilen `orderNumber` vardır |
| Varyant, adres, koleksiyon, etiket, fatura vb. | Endpoint sözleşmesine göre UUID/GUID |

Raw veritabanı `long` kullanıcı veya ürün kimliği API'ye gönderilmez.

## Sayfalama

Sayfalı endpointler ortak olarak şu yapıyı döndürür:

```json
{
  "items": [],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

Genel varsayılan `PageNumber=1`, `PageSize=20`; storefront ürün listesinde varsayılan sayfa boyutu farklı olabilir. Üst sınır endpoint bazında doğrulanır ve çoğunlukla 100'dür.

## Tarih ve saat

- Tarih-saat değerleri ISO 8601 UTC gönderilir: `2026-08-26T12:00:00Z`.
- Yalnız tarih alanları `YYYY-MM-DD` biçimindedir.
- `CreatedFromUtc` ve `CreatedToUtc` filtrelerinde sınırlar ilgili sözleşmede belirtildiği şekilde inclusive uygulanır.
- Kullanıcı arayüzü zamanı yerel saate çevirebilir; API'ye geri gönderirken UTC kullanılmalıdır.

## Enumlar

Wire sözleşmesinde enumlar sayısal gönderilir. Görsel etiketten veya enum adından yeni sayı tahmin etmeyin. Sık kullanılan değerler [Kimlikler ve enumlar](05-kimlikler-ve-enumlar.md) sayfasında listelenmiştir.

## Backend otoritesindeki alanlar

İstemci sepet ve checkout işlemlerinde şunları hesaplayıp request'e eklemez:

- Kullanıcı kimliği
- Ürün başlığı veya ürün snapshot kimliği
- Birim/net fiyat
- Vergi oranı veya vergi tutarı
- İndirim tutarı
- Kargo adı veya ücreti
- Stok
- Ara toplam veya genel toplam
- Sipariş numarası
- Sipariş/ödeme durumu

API, son katalog ve stok durumuna göre bu alanları kendisi üretir.

## Boş response

`204 No Content` başarılıdır ve JSON body içermez. İstemci bu yanıtı JSON olarak parse etmeye çalışmamalıdır.

## Cache

- Public katalog ve storefront içeriği cache'lenebilir.
- Sepet, checkout, sipariş, guest erişim, hesap ve admin yanıtları private kabul edilmeli; çoğunlukla `no-store` kullanılmalıdır.
- Başarılı katalog yönetim mutasyonları ilgili public cache etiketlerini geçersiz kılar.
- Silme veya güncelleme sonrasında kısa süreli eski veri görülürse istemci önce authoritative endpointi yeniden okumalıdır.

