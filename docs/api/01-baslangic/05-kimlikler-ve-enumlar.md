# Kimlikler ve Temel Enumlar

Bu sayfa en sık kullanılan sayısal enumları hızlı başvuru amacıyla toplar. Tam ve güncel şema için [OpenAPI JSON](../openapi.json) kullanılmalıdır.

## Kullanıcı

### UserRole

| Değer | Ad |
| ---: | --- |
| `1` | Customer |
| `2` | Admin |

### UserStatus

| Değer | Ad |
| ---: | --- |
| `1` | Active |
| `2` | Passive |
| `3` | Deleted |

## Sipariş

### OrderStatus

| Değer | Ad |
| ---: | --- |
| `0` | Pending |
| `1` | Confirmed |
| `2` | Paid |
| `3` | Preparing |
| `4` | Shipped |
| `5` | Delivered |
| `6` | Cancelled |
| `7` | Refunded |
| `8` | ReturnRequested |
| `9` | ReturnApproved |

## İade

### ReturnType

| Değer | Ad |
| ---: | --- |
| `0` | Refund |
| `1` | Exchange |

### ReturnRequestStatus

| Değer | Ad |
| ---: | --- |
| `0` | Requested |
| `1` | Approved |
| `2` | Rejected |
| `3` | Received |
| `4` | Completed |

Yeni operasyonel akış `Requested → Received → Approved veya Rejected` sırasını kullanır. `Completed` eski kayıt uyumluluğu için korunur; geçişler istemci tarafından tahmin edilmemelidir.

## Kimlik notları

- User ID örneği: `U00001`
- Product ID örneği: `P00001`
- UUID örneği: `3b1f64b5-d3be-4c71-a3c4-9699fd0d0d26`
- `orderNumber` görüntüleme ve arama içindir; sipariş route kimliği UUID'dir.
- Endpoint `id` alanının hangi kimlik tipini istediği ilgili sözleşmede açıkça belirtilir.

