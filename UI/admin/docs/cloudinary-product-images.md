# Cloudinary ürün görselleri

Admin ürün formu görselleri tarayıcıdan doğrudan Cloudinary'ye yükler. ASP.NET API dosya taşımak yerine Cloudinary'nin güvenli URL'sini, sırasını ve ana görsel seçimini saklar.

## Cloudinary ayarı

1. Cloudinary'de unsigned bir upload preset oluşturun.
2. İzin verilen biçimleri JPG, PNG ve WebP ile sınırlayın.
3. Maksimum dosya boyutunu 8 MB yapın.
4. Preset içinde sabit bir klasör tanımlamayın. Admin isteği klasörü `products/{productPublicId}` olarak gönderir ve dönen `public_id` bu klasörle doğrulanır.
5. Gerçek hesap adını ve preset adını `UI/admin/.env.local` içine ekleyin:

```dotenv
NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME=your-cloud-name
NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET=your-unsigned-upload-preset
```

`API secret`, signed-upload secret veya başka bir gizli değer `NEXT_PUBLIC_*` değişkenlerine yazılmaz. Public ortam değişkenleri değiştiğinde Next.js uygulamasını yeniden derleyin/başlatın.

## Kayıt davranışı

- Bir üründe mevcut ve yeni görseller toplamı en fazla 10'dur.
- Dosyalar kaydetme sırasında en fazla üç eşzamanlı istekle yüklenir.
- Yeni üründe ilk seçilen görsel varsayılan ana görseldir; kullanıcı kaydetmeden önce başka bir görsel seçebilir.
- API'ye yalnız seçilen görsel için `isMain: true` gönderilir. Diğer görselleri `false` yapma ve tek ana görsel garantisi backend'e aittir.
- Kısmi yükleme hatasında başarılı Cloudinary yanıtları korunur; tekrar deneme yalnız eksik dosya veya API adımını çalıştırır.
- Bu sürüm Cloudinary'den fiziksel silme ve manuel sıralama içermez.
