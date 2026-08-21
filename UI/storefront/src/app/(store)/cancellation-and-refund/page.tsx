import Link from "next/link";

import { LegalPage, type LegalSection } from "@/modules/legal/components/legal-page";
import { legalPageMetadata } from "@/modules/legal/metadata";

export const metadata = legalPageMetadata(
  "İptal ve İade Şartları",
  "Sipariş iptali, 14 günlük cayma hakkı, iade gönderimi, geri ödeme ve cayma hakkı istisnalarını inceleyin.",
  "/cancellation-and-refund",
);

// Burada API sipariş durumlarıyla kanuni cayma hakkını birbirine karıştırmadan ayrı süreçler olarak açıklıyorum.
const sections: LegalSection[] = [
  {
    id: "cancellation",
    title: "Sipariş iptali",
    content: <><p>Ödeme öncesi veya sipariş henüz hazırlanma sürecine geçmeden iptal seçeneği sipariş durumuna göre sunulabilir. Mevcut sistemde müşteri iptali yalnız sipariş kargoya verilmeden önce ve uzlaştırma bekleyen ödeme bulunmadığında gerçekleştirilebilir.</p><p>Ödenmiş, hazırlanan veya kargoya verilmiş siparişlerde doğrudan iptal yerine teslimat ve kanuni iade süreci uygulanabilir. İptal sonucu stok, kupon ve rezervasyon durumu mağaza sistemi tarafından yeniden hesaplanır.</p></>,
  },
  {
    id: "withdrawal-period",
    title: "14 günlük cayma hakkı",
    content: <><p>Tüketici, kural olarak ürünü kendisinin veya belirlediği üçüncü kişinin teslim aldığı günden itibaren on dört gün içinde herhangi bir gerekçe göstermeden cayma hakkını kullanabilir. Tek siparişte ayrı günlerde teslim edilen ürünlerde süre, son ürünün teslimiyle başlar.</p><p>Cayma bildiriminin süre dolmadan satıcıya yazılı olarak veya kalıcı veri saklayıcısıyla yöneltilmesi yeterlidir. Bildirimin ulaştığına dair kayıt saklanmalıdır.</p></>,
  },
  {
    id: "request",
    title: "Talep nasıl oluşturulur?",
    content: <><ol><li>Sipariş erişim alanından ilgili teslim edilmiş sipariş ve ürün seçilir.</li><li>İade veya uygun olduğunda değişim türü, adet ve açıklama bildirilir.</li><li>Talebin alındığını gösteren kayıt ve iade yönlendirmesi takip edilir.</li></ol><p>Hesapsız siparişlerde yalnız güvenli sipariş erişim bağlantısı veya doğrulanmış guest oturumu kullanılır. Sipariş numarası ve e-posta adresi tek başına erişim yetkisi sağlamaz.</p></>,
  },
  {
    id: "return-shipment",
    title: "Ürünün geri gönderilmesi",
    content: <><p>Satıcı ürünü kendisinin alacağını bildirmedikçe tüketici, cayma bildiriminden itibaren yönetmelikteki süre içinde ürünü belirtilen adrese veya yetkilendirilmiş kişiye gönderir. Güncel düzenlemede bu süre on gündür.</p><p>Ürün mümkünse orijinal ambalajı, aksesuarları ve siparişle gönderilen parçalarıyla; taşıma sırasında zarar görmeyecek biçimde paketlenmelidir. Cayma süresinde ürünü işleyişine ve kullanım talimatına uygun incelemekten doğan makul değişiklikler ayrıca değerlendirilir.</p></>,
  },
  {
    id: "shipping-cost",
    title: "İade kargo masrafı",
    content: <><p>İade taşıyıcısı, masrafın kime ait olduğu ve varsa mahsup edilebilecek tutar sipariş öncesi bilgilendirmede açıkça gösterilir ve yürürlükteki mevzuata göre uygulanır.</p><p>Ürün ayıplı, hasarlı, eksik veya yanlış gönderilmişse tüketiciye iade masrafı yüklenmez. Belirtilen taşıyıcının tüketicinin bulunduğu yerde hizmet vermemesi halinde mevzuattaki ek yükümlülükler uygulanır.</p></>,
  },
  {
    id: "refund",
    title: "Geri ödeme",
    content: <><p>Cayma bildiriminin satıcıya ulaştığı tarihten itibaren tahsil edilen bedeller, mevzuatın öngördüğü süre içinde ve tüketicinin satın alırken kullandığı ödeme aracına uygun biçimde iade edilir. Kural olarak bu süre on dört gündür.</p><p>Bankanın veya ödeme kuruluşunun tutarı hesaba yansıtma süresi mağazanın iade işlemini tamamladığı tarihten farklı olabilir. İade sonucu kalıcı bir kayıtla tüketiciye bildirilir.</p></>,
  },
  {
    id: "exceptions",
    title: "İade ve cayma istisnaları",
    content: <><p>Kanuni istisnalar ürün özelinde ve satın alma öncesinde açıkça belirtilir. Mağaza bakımından özellikle şunlar önemlidir:</p><ul><li>Fiyatı satıcının kontrolü dışındaki finansal piyasa dalgalanmalarına bağlı mallar,</li><li>Kişiye özel ölçü, yazı, seçim veya talimatla hazırlanan ürünler,</li><li>Koruyucu ambalajı açıldıktan sonra sağlık ve hijyen açısından iadesi uygun olmayan ürünler.</li></ul><p>Bir ürünün bu gruplardan birine girmesi otomatik varsayılmaz; ürün niteliği ve ön bilgilendirme birlikte değerlendirilir.</p></>,
  },
  {
    id: "defects",
    title: "Ayıplı veya yanlış ürün",
    content: <><p>Ayıplı mala ilişkin onarım, değişim, bedel indirimi veya sözleşmeden dönme gibi seçimlik haklar cayma hakkından ayrıdır. Yanlış ürün, eksik parça, taşıma hasarı veya açıklamaya aykırılık halinde fotoğraf, paket etiketi ve varsa kargo tutanağı incelemeyi hızlandırabilir.</p><p>Bu belgelerin bulunmaması kanuni hakları kendiliğinden ortadan kaldırmaz.</p></>,
  },
  {
    id: "official-source",
    title: "Resmî bilgi kaynağı",
    content: <p>Genel tüketici bilgisi için T.C. Ticaret Bakanlığı’nın <a href="https://tuketici.ticaret.gov.tr/yayinlar/tuketici-bilgi-rehberi/mesafeli-sozlesmeler-hakkinda-bilgilendirme" target="_blank" rel="noreferrer">Mesafeli Sözleşmeler Hakkında Bilgilendirme</a> sayfası incelenebilir. Siparişin genel sözleşme çerçevesi için <Link href="/distance-sales-agreement">Mesafeli Satış Sözleşmesi</Link> sayfasına bakılabilir.</p>,
  },
];

// Burada iptal ve iade politikasını müşterinin süreç sırasına uygun statik sayfa olarak sunuyorum.
export default function CancellationAndRefundPage() {
  return <LegalPage eyebrow="Müşteri bilgilendirmesi" title="İptal ve İade Şartları" summary="Sipariş iptali, cayma bildirimi, ürünün geri gönderilmesi ve bedel iadesi için uygulanacak temel süreç." sections={sections} />;
}
