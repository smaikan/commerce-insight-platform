# PUT /api/store-settings/contact

- Yetki: `AdminOnly`.
- Başarı: `200 AdminStoreSettingsDto`; yeni token.
- Hatalar: `400`, `401`, `403`, `409` ProblemDetails.

Request; nullable `supportEmail` (320), `supportPhone`/`whatsappNumber` (30), `contactAddress` (1000), `workingHours` (500), mutlak HTTP/HTTPS `mapUrl` (500); altı `show...` booleanı ve zorunlu `expectedConcurrencyToken` taşır.

Görünürlük kapatıldığında değer silinmez; public GET ilgili değeri `null` döndürür. Yalnız iletişim bölümünü değiştirir.
