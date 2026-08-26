# POST /api/addresses

- Görev alanı: **Kimlik ve kullanıcılar → Adreslerim**.
- İşlev: oluşturur.
- Operation ID: `POST-/api/addresses`
- Yetki: **User**.
- Content-Type: request body varsa `application/json` gönderin.
- Hata: 400 validation/domain, 401 authentication, 403 policy, 404 kaynak, 409 conflict/concurrency. Ortak gövde `ProblemDetails`tir.

## Parametreler

Path, query veya header parametresi yoktur.

## Request body

Aşağıdaki örnek alan adlarını camelCase ile gönderin.

| Alan | Tip | Zorunlu |
| --- | --- | --- |
| `type` | integer (int32) | Evet |
| `title` | string | Evet |
| `firstName` | string | Evet |
| `lastName` | string | Evet |
| `phoneNumber` | string | Evet |
| `city` | string | Evet |
| district | string | Evet |
| neighborhood | string | Hayır |

| 
eighborhood | string | Evet |

| `fullAddress` | string | Evet |
| `postalCode` | string | Hayır |
| `isDefault` | boolean | Evet |

```json
{
  "type": 0,
  "title": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string",
  "city": "string",
  "district": "string",
  "neighborhood": "string",
  "fullAddress": "string",
  "postalCode": "string",
  "isDefault": true
}
```

## Başarılı response (200)

```json
{
  "id": "00000000-0000-0000-0000-000000000001",
  "type": 0,
  "title": "string",
  "firstName": "string",
  "lastName": "string",
  "phoneNumber": "string",
  "city": "string",
  "district": "string",
  "neighborhood": "string",
  "fullAddress": "string",
  "postalCode": "string",
  "isDefault": true,
  "createdAt": "2026-07-29T12:00:00Z",
  "updatedAt": "2026-07-29T12:00:00Z"
}
```




