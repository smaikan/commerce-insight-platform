# POST /api/product-types/bulk

- Görev alanı: **Katalog → Kategoriler**.

- İşlev: En fazla 500 ürün türünü tek işlemde oluşturur.
- Yetki: **Admin**.
- Başarı: `201 ProductTypeDto[]`.
- Hatalar: `400`, `401`, `403`, yinelenen/mevcut ad için `409`; ortak `ProblemDetails`.

## Request

```json
{
  "productTypes": [
    {
      "name": "Ayakkabı",
      "description": "Ayakkabı ürünleri",
      "isActive": true,
      "imageUrl": "https://cdn.example.com/categories/shoes.webp"
    }
  ]
}
```

Her `imageUrl` nullable ve en çok 500 karakterdir. Başarılı response içindeki her `ProductTypeDto`, kalıcı özel `imageUrl` alanını taşır.
