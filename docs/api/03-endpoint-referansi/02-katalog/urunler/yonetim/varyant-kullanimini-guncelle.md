# PATCH /api/products/{id}/has-variants

- Görev alanı: **Katalog → Ürünler → Yönetim**.
- İşlev: Ürünün varyantlı veya varyantsız sunum tercihini değiştirir.
- Yetki: **Admin**.

## Request body

```json
{
  "hasVariants": false
}
```

`hasVariants: false`, tek varyantlı ürünü ana/tek ürün gibi listeler. Üründe birden fazla varyant varsa `false` kabul edilmez ve API 400 döner. Değer kalıcıdır; ürün response’larında aynı değer döner.

## Başarılı response

`200 OK` ve güncel `ProductDto` döner.
