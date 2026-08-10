# Cloudinary admin medya ayarları

Admin paneli ürün, marka, koleksiyon ve banner dosyalarını tarayıcıdan doğrudan Cloudinary'ye yükler. API dosyanın kendisini değil, doğrulanan HTTPS URL'sini saklar.

## Ortam değişkenleri

Gerçek değerleri yalnız `UI/admin/.env.local` içinde tutun:

```dotenv
NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME=your-cloud-name
NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET=your-unsigned-image-preset
NEXT_PUBLIC_CLOUDINARY_BANNER_UPLOAD_PRESET=your-unsigned-banner-preset
```

Bu değerler tarayıcıya açıktır. Cloudinary API secret veya signed-upload secret değerlerini `NEXT_PUBLIC_*` değişkenlerine eklemeyin.

## Preset kuralları

Görsel preset'i ürün, marka ve koleksiyonlarda kullanılır:

- Unsigned upload açık olmalı.
- JPG, PNG ve WebP kabul edilmeli.
- Maksimum dosya boyutu 8 MB olmalı.
- İstekten gelen klasörü ezmemeli.

Banner preset'i yalnız vitrin medyasında kullanılır:

- Unsigned upload açık olmalı.
- JPG, PNG, WebP, MP4 ve WebM kabul edilmeli.
- Görseller en fazla 8 MB, videolar en fazla 25 MB olmalı.
- İstekten gelen `banners/main` ve `banners/alternate` klasörlerini ezmemeli.

Preset veya ortam değişkeni değiştiğinde Next.js geliştirme sunucusunu yeniden başlatın.

## Saklama davranışı

- Ürünler `products/{productId}`, markalar `brands/{brandId}`, koleksiyonlar `collections/{collectionId}` altında yüklenir.
- Bannerlar ana ve alt banner klasörlerinde saklanır.
- Marka ve koleksiyon oluşturulurken kayıt önce API'de oluşturulur; medya dönen kimlikle yüklenip ikinci adımda kayda bağlanır.
- API yalnız URL sakladığı için bir görseli formdan kaldırmak Cloudinary dosyasını fiziksel olarak silmez. Otomatik silme için ileride backend tarafında imzalı silme ve `public_id` sözleşmesi gerekir.
