# GET /api/guest-orders/{id}/returns

- Görev alanı: **Satış ve sipariş → İade ve değişim → Misafir işlemleri**.

- Yetki: **Guest session**.

Guest session'ın yetkili olduğu siparişin iade/değişim taleplerini sayfalar.

- Authorization: guest session cookie; JWT gerekmez.
- Cookie: `ecommerce_guest_orders`.
- Cevap: `no-store`; cookie değişmez.

| Alan | Konum | Required | Nullable | Kural |
| --- | --- | --- | --- | --- |
| id | route | Evet | Hayır | Order GUID |
| pageNumber | query | Hayır | Hayır | Varsayılan 1 |
| pageSize | query | Hayır | Hayır | Varsayılan 20, en fazla 100 |

```http
GET /api/guest-orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/returns?pageNumber=1&pageSize=20
Cookie: ecommerce_guest_orders=<session>
```

```json
{
  "items": [
    {
      "id": "8dfd2b27-8083-469d-9d08-af232bc207ea",
      "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
      "returnNumber": "RET-ABC",
      "type": 1,
      "status": 1
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1
}
```

Hatalar: `400` sayfalama/GUID; `401 invalid_guest_access`; `404 not_found` çapraz-order dahil; `403/409/428/429/503` normal GET'te beklenmez; `500` kontrollü retry. UI session biterse access-link akışına döner. Cookie ve iade PII'si loglanmaz.
