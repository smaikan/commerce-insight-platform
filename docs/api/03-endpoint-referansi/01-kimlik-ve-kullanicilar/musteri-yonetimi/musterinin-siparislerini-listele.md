# GET /api/users/{id}/orders

- Görev alanı: **Kimlik ve kullanıcılar → Müşteri yönetimi**.

Yöneticinin seçili müşteriye ait e-ticaret siparişlerini sayfalı olarak okumasını sağlar.

## Yetki ve parametre

**Admin.** `id`, `U...` biçiminde public User ID'dir. OpenAPI'de görünen `UserId` query alanı istemci tarafından gönderilmez; controller değeri path kimliğinden güvenli biçimde atar.

```http
GET /api/users/U00001/orders?PageNumber=1&PageSize=20&Status=5&CreatedFromUtc=2026-08-01T00:00:00Z&CreatedToUtc=2026-08-31T23:59:59Z
Authorization: Bearer <admin-access-token>
```

## Query parametreleri

| Parametre | Tip | Zorunlu |
| --- | --- | --- |
| `PageNumber` | integer | Hayır |
| `PageSize` | integer | Hayır |
| `Status` | integer `OrderStatus` | Hayır |
| `CreatedFromUtc` | ISO 8601 UTC | Hayır |
| `CreatedToUtc` | ISO 8601 UTC | Hayır |

## Başarılı response — 200 OK

```json
{
  "items": [
    {
      "id": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
      "orderNumber": "ORD-20260826-A1B2",
      "status": 5,
      "grandTotal": 1349.8,
      "itemCount": 1,
      "createdAt": "2026-08-20T09:15:00Z",
      "paidAt": "2026-08-20T09:18:00Z",
      "customerName": "Deniz Yılmaz"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Hatalar

- `400`: public User ID veya filtreler geçersiz
- `401`: token yok/geçersiz
- `403`: Admin rolü yok
- `404`: kullanıcı bulunamadı

