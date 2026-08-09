# Cari Hesaplar

Cari hesap, müşteri ve tedarikçi için tek master kayıttır. Ayrı Supplier veya CurrentAccountAddress API'si yoktur; adres alanları doğrudan cari hesaptadır.

## Endpointler

| İşlem | Endpoint |
| --- | --- |
| Oluştur | `POST /api/accounting/current-accounts` |
| Güncelle | `PUT /api/accounting/current-accounts/{id}` |
| Detay | `GET /api/accounting/current-accounts/{id}` |
| Liste | `GET /api/accounting/current-accounts?pageNumber=1&pageSize=20` |
| Cari ekstre raporu | `GET /api/accounting/reports/current-accounts/{id}/statement` |

## Oluşturma body'si

```json
{
  "code": "CUS-0001",
  "type": 1,
  "name": "Örnek Müşteri A.Ş.",
  "tradeName": "Örnek Müşteri",
  "taxNumber": "1234567890",
  "taxOffice": "Kadıköy",
  "phoneNumber": "+905551112233",
  "email": "muhasebe@ornek.com",
  "country": "TR",
  "city": "İstanbul",
  "district": "Kadıköy",
  "neighborhood": "Osmanağa",
  "addressLine": "Rıhtım Cad. No:1",
  "postalCode": "34710",
  "userId": null
}
```

`type`: müşteri için `1`, tedarikçi için `2`, iki role de sahip hesap için `3`.

`userId` yalnız mevcut e-ticaret kullanıcısına opsiyonel bağlantıdır. AccountingSalesOrder oluşturmak için gerekli değildir.

## Güncelleme body'si

```json
{
  "account": {
    "code": "CUS-0001",
    "type": 1,
    "name": "Örnek Müşteri A.Ş.",
    "city": "İstanbul"
  },
  "isActive": true
}
```

Pasif cari hesaplar yeni satış/alış kullanımında geçersizdir. Mevcut tarihsel belgeler korunur.

## Örnek response

```json
{
  "id": "5ffbcac9-87cb-4b38-9b28-fb47a2d86645",
  "code": "CUS-0001",
  "type": 1,
  "name": "Örnek Müşteri A.Ş.",
  "taxNumber": "1234567890",
  "city": "İstanbul",
  "isActive": true,
  "userId": null
}
```

Frontend notu: müşteri seçicisi `Customer (1)` veya `CustomerAndSupplier (3)` hesaplarını; tedarikçi seçicisi `Supplier (2)` veya `CustomerAndSupplier (3)` hesaplarını göstermelidir.
