# POST /api/product-types

- Görev alanı: **Katalog → Kategoriler**.

- İşlev: Yeni ürün türü/kategori oluşturur.
- Yetki: **Admin**.
- Başarı: `201 ProductTypeDto`.
- Hatalar: `400`, `401`, `403`, aynı ad için `409`; ortak `ProblemDetails`.

## Request

| Alan | Zorunlu | Kural |
| --- | --- | --- |
| `name` | Evet | Boş olamaz, en çok 150. |
| `description` | Hayır | Nullable, en çok 1000. |
| `isActive` | Hayır | Varsayılan `true`. |
| `imageUrl` | Hayır | Nullable, en çok 500; API yalnız URL değerini saklar. |

```json
{
  "name": "Ayakkabı",
  "description": "Ayakkabı ürünleri",
  "isActive": true,
  "imageUrl": "https://cdn.example.com/categories/shoes.webp"
}
```

Boş veya whitespace `imageUrl`, veritabanında `null` olarak normalize edilir. Görsel yükleme bu endpointin sorumluluğu değildir.
