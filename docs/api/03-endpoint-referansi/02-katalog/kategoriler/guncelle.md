# PUT /api/product-types/{id}

- Görev alanı: **Katalog → Kategoriler**.

- İşlev: Ürün türünün ad, açıklama ve özel görselini günceller.
- Yetki: **Admin**.
- Başarı: `200 ProductTypeDto`.
- Hatalar: `400`, `401`, `403`, `404`, aynı ad için `409`; ortak `ProblemDetails`.

## Request

| Alan | Zorunlu | Kural |
| --- | --- | --- |
| `name` | Evet | Boş olamaz, en çok 150. |
| `description` | Hayır | Nullable, en çok 1000. |
| `imageUrl` | Hayır | Nullable, en çok 500. `null` veya boş değer özel görseli kaldırır. |

```json
{
  "name": "Ayakkabı",
  "description": "Güncel açıklama",
  "imageUrl": "https://cdn.example.com/categories/shoes-v2.webp"
}
```

Başarılı güncelleme `products` output-cache etiketini temizler; public kategori vitrini sonraki cache üretiminde yeni özel görseli veya fallback'i kullanır.
