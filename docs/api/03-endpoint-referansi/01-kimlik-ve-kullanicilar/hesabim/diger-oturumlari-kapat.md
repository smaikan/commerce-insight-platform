# DELETE /api/users/me/sessions

- Görev alanı: **Kimlik ve kullanıcılar → Hesabım**.
- İşlev: Diger oturumları kapatır.
- Operation ID: `DELETE-/api/users/me/sessions`
- Yetki: **User**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Bu operasyon JSON request body almaz. Gerekli tüm değerleri yukarıdaki path, query veya header parametreleriyle gönderin.

## Başarılı response (200)

Response body yoktur.

