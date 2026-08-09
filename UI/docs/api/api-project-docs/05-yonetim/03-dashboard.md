# Dashboard API

## Ürün analitiği

`GET /api/dashboard/product-analytics?from=YYYY-MM-DD&to=YYYY-MM-DD` yalnızca `AdminOnly` yetkisiyle çalışır. Her iki tarih zorunludur, UTC gün sınırını kullanır ve iki uç dahil en fazla 90 günlük aralık kabul eder.

Endpoint, tüm ürünlerin seçili dönem toplamlarını ve gün bazlı toplam serisini backend'de hesaplar; hareketsiz günler sıfır sayaçlarla döner. `topProducts` en fazla beş kayıttır ve `purchaseCount`, `addToCartCount`, `clickCount` azalan sırasıyla hazırlanır. Böylece dashboard'un ürün bazlı endpointleri tek tek çağırıp toplam üretmesi gerekmez.

`GET /api/dashboard/overview` yalnızca yönetici yetkisiyle çalışır ve operasyon özetini döner.

`lowStockVariantCount`, aktif olan, stoku sıfırdan büyük ve `Dashboard:LowStockThreshold` değerinden küçük varyantların sayısıdır. Varsayılan eşik `10`dur; ortam bazında `Dashboard__LowStockThreshold` ile değiştirilebilir ve pozitif olmalıdır.

`paidRevenue` net tahsilattır: `paidAt` değeri bulunan ve durumu `Refunded` olmayan siparişlerin `grandTotal` toplamından, tamamlanmış (`Completed`) refund iade taleplerinin `refundTotal` tutarı düşülür. Böylece tam iade edilmiş siparişler gelire dahil edilmez; tamamlanmış kısmi iadeler de gelirden düşülür. Talep yalnız açılmış, onaylanmış veya teslim alınmış durumdaysa henüz düşülmez.

Dashboard okuyucusu bu altı metriği tek SQL aggregate sorgusunda alır; ilk yüklemede ayrı sayım/toplam round-trip'leri oluşturmaz.
