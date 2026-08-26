# İade ve Değişim

İade/değişim talepleri sipariş iptalinden farklıdır. Kargoya verilmiş veya müşteriye ulaşmış ürünler için return akışı kullanılır.

## Erişim

- Üye müşteri: Bearer token ve sipariş sahipliği
- Misafir müşteri: guest order session ve sahiplik
- Yönetici: Bearer token + `AdminOnly`

## Talep oluşturma

Üye endpointi:

```http
POST /api/returns
Authorization: Bearer <access-token>
```

Misafir endpointi:

```http
POST /api/guest-orders/{orderId}/returns
```

Örnek body:

```json
{
  "orderId": "3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26",
  "type": 0,
  "reason": "Beden uygun olmadı.",
  "items": [
    {
      "orderItemId": "fbe6c35a-2f0c-45d7-b849-cf9809080e89",
      "quantity": 1
    }
  ]
}
```

`type=0` iade, `type=1` değişimdir. Tam alanlar ilgili endpoint sözleşmesinden kontrol edilmelidir.

## Yaşam döngüsü

Güncel operasyon sırası:

```text
Requested → Received → Approved
                     ↘ Rejected
```

Yönetici ürün fiziksel olarak teslim alınmadan karar vermemelidir. `Completed` eski kayıt uyumluluğu için korunur. Geçersiz geçiş `409 return_status_transition_invalid` döndürür.

## Stok ve finans etkisi

Stok yalnız iş akışının belirlediği aşamada ve tek sefer artırılır. İade onayı, ödeme iadesi ve stok hareketi aynı kavram değildir; endpoint response'u tamamlandı demeden istemci bunlardan birini gerçekleşmiş varsaymamalıdır.

## Concurrency

Admin karar mutasyonları güncel concurrency token ister. `409 concurrency_conflict` durumunda iade detayı yeniden okunmalı ve karar körlemesine tekrarlanmamalıdır.

## Ayrıntılı referans

[İade endpointleri](../03-endpoint-referansi/03-satis-ve-siparis/iadeler/README.md)

