# Storefront

[API dokümantasyonuna dön](../../../../README.md) · [Tam endpoint referansına dön](../../../README.md)

Bu bölüm **11 operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.

## Görev alanları

| Alan | Operasyon |
| --- | ---: |
| [Filtre seçenekleri](./filtre-secenekleri/README.md) | 3 |

## İşlemler

| Görev | Metot ve endpoint | Yetki |
| --- | --- | --- |
| [Markaya gore listele](./markaya-gore-listele.md) | **GET** `/api/products/by-brand/{brandId}` | Public |
| [Koleksiyona gore listele](./koleksiyona-gore-listele.md) | **GET** `/api/products/by-collection/{collectionId}` | Public |
| [Etikete gore listele](./etikete-gore-listele.md) | **GET** `/api/products/by-tag/{tagId}` | Public |
| [Kategoriye gore listele](./kategoriye-gore-listele.md) | **GET** `/api/products/by-type/{typeId}` | Public |
| [Url ile ürün getir](./url-ile-urun-getir.md) | **GET** `/api/products/by-url/{url}` | Public |
| [Yayınlanan ürünleri listele](./yayinlanan-urunleri-listele.md) | **GET** `/api/products/published` | Public |
| [Arama önerilerini getir](./arama-onerilerini-getir.md) | **GET** `/api/products/published/search-suggestions` | Public |
| [SEO indeksini getir](./seo-indeksini-getir.md) | **GET** `/api/products/seo-index` | Public |
