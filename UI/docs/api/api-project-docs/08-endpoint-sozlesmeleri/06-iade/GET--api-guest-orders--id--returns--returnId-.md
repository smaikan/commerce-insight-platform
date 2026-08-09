# GET /api/guest-orders/{id}/returns/{returnId}

Guest session'ın yalnız grant verilen siparişine ait iade talebi detayını döndürür.

| Alan | Konum | Required | Nullable |
| --- | --- | --- | --- |
| id | route | Evet | Hayır |
| returnId | route | Evet | Hayır |

```http
GET /api/guest-orders/3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26/returns/8dfd2b27-8083-469d-9d08-af232bc207ea
Cookie: ecommerce_guest_orders=<session>
```

Başarı `200` ve kalemleriyle `ReturnRequestDto`; cookie değişmez, cevap `no-store` olmalıdır.

Hatalar: `400` GUID; `401 invalid_guest_access`; `404 not_found` iade yok, başka siparişe ait veya grant yok; `403/409/428/429/503` beklenmez; `500` kontrollü retry. UI 404'te kaynak varlığını sızdırmayan genel mesaj gösterir. Cookie ve iade içeriği loglanmaz.
