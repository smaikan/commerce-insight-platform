import Link from "next/link";

import { LegalPage, type LegalSection } from "@/modules/legal/components/legal-page";
import { SellerIdentity } from "@/modules/legal/components/seller-identity";
import { legalPageMetadata } from "@/modules/legal/metadata";
import { getPublicStoreSettings } from "@/modules/store-settings/api";

export const metadata = legalPageMetadata(
  "Mesafeli Satış Sözleşmesi",
  "Mesafeli satışlarda taraflar, sipariş, ödeme, teslimat, cayma ve uyuşmazlık hükümlerini inceleyin.",
  "/distance-sales-agreement",
);

// Burada mesafeli satış sayfasını mağaza ayarlarıyla beslenen Server Component olarak ilk HTML içinde sunuyorum.
export default async function DistanceSalesAgreementPage() {
  const settings = await getPublicStoreSettings().catch(() => null);

  const sections: LegalSection[] = [
    {
      id: "scope",
      title: "Amaç ve kapsam",
      content: <p>Bu metin, internet mağazası üzerinden tüketici ile satıcı arasında uzaktan iletişim araçları kullanılarak kurulacak satış ilişkisinin genel esaslarını açıklar. Somut siparişin ürünleri, miktarı, fiyatı, vergileri, kargo bedeli, teslimat adresi ve taraf bilgileri sipariş onayından önce ayrıca gösterilir.</p>,
    },
    {
      id: "parties",
      title: "Taraflar ve satıcı bilgileri",
      content: <><p>Alıcı, sipariş sırasında iletişim ve teslimat bilgilerini veren tüketicidir. Satıcıya ait bağlayıcı kimlik ve iletişim bilgileri siparişe özel ön bilgilendirme formu ile sözleşmede yer alır.</p><SellerIdentity settings={settings} /></>,
    },
    {
      id: "order",
      title: "Sözleşme konusu ve sipariş",
      content: <><p>Sözleşmenin konusu, alıcının elektronik ortamda seçtiği ürünlerin sipariş özetinde belirtilen şartlarla satışı ve teslimidir. Ürünün temel nitelikleri, varyantı, adedi ve güncel satış bedeli ürün sayfası ile sipariş özetinde gösterilir.</p><p>Sipariş butonunun ödeme yükümlülüğü doğurduğu açıkça belirtilmeden ve ön bilgiler tüketici tarafından teyit edilmeden sözleşme kurulmuş sayılmaz. Teknik hata veya stok/fiyat uyuşmazlığında tüketici bilgilendirilir; backend tarafından doğrulanmayan tutar bağlayıcı kabul edilmez.</p></>,
    },
    {
      id: "payment",
      title: "Fiyat ve ödeme",
      content: <><p>Toplam bedel; ürün fiyatı, vergi, varsa indirim ve teslimat ücretinin sipariş anındaki otoriter hesaplamasından oluşur. Kullanılabilen ödeme yöntemleri yalnız ödeme adımında gösterilir.</p><p>Ödeme hizmeti etkin değilse sipariş oluşturma işlemi kapalı tutulur ve tüketiciden tahsilat yapılmaz. Ayrıntılar için <Link href="/payment-and-delivery">Ödeme ve Teslimat</Link> sayfası incelenebilir.</p></>,
    },
    {
      id: "delivery",
      title: "Teslimat",
      content: <><p>Teslimat, alıcının bildirdiği adrese ve sipariş sırasında seçtiği aktif kargo yöntemiyle yapılır. Tahmini süre ve ücret sipariş öncesinde gösterilir. Mevzuatta daha kısa süre öngörülmediği sürece ürün siparişi en geç otuz gün içinde yerine getirilir.</p><p>İfanın imkânsızlaşması halinde tüketici kalıcı veri saklayıcısıyla bilgilendirilir ve mevzuattaki süreler içinde tahsil edilen bedeller iade edilir.</p></>,
    },
    {
      id: "withdrawal",
      title: "Cayma hakkı",
      content: <><p>Tüketici, kural olarak ürünü teslim aldığı tarihten itibaren on dört gün içinde gerekçe göstermeden ve cezai şart ödemeden cayma hakkını kullanabilir. Bildirimin süre dolmadan yazılı olarak veya kalıcı veri saklayıcısıyla satıcıya yöneltilmesi yeterlidir.</p><p>Ürünün geri gönderilmesi, iade masrafı ve geri ödeme yöntemi yürürlükteki mevzuata ve sipariş öncesi bilgilendirmeye göre uygulanır. Ayrıntılar için <Link href="/cancellation-and-refund">İptal ve İade Şartları</Link> sayfası incelenebilir.</p></>,
    },
    {
      id: "exceptions",
      title: "Cayma hakkı istisnaları",
      content: <><p>Yönetmelikte sayılan istisnalar saklıdır. Mağazanın ürün niteliğine göre özellikle aşağıdaki durumlar gündeme gelebilir:</p><ul><li>Fiyatı finansal piyasalardaki dalgalanmalara bağlı ve satıcının kontrolü dışında değişen mallar,</li><li>Tüketicinin özel istek veya kişisel ihtiyaçlarına göre hazırlanan ürünler,</li><li>Koruyucu ambalajı açıldıktan sonra sağlık veya hijyen açısından iadesi uygun olmayan ürünler.</li></ul><p>İstisna uygulanacaksa ürün ve sipariş özelinde, satın alma öncesinde açıkça belirtilir.</p></>,
    },
    {
      id: "defective-goods",
      title: "Ayıplı mal hakları",
      content: <p>Cayma hakkından bağımsız olarak 6502 sayılı Kanun kapsamındaki ayıplı mala ilişkin seçimlik haklar saklıdır. Ürünün sözleşmeye aykırı, hasarlı veya eksik teslim edildiği iddiası sipariş ve teslimat kayıtlarıyla incelenir; ayıplı mal iadesinde tüketiciye mevzuata aykırı masraf yüklenmez.</p>,
    },
    {
      id: "disputes",
      title: "Uyuşmazlık ve yürürlük",
      content: <><p>Uyuşmazlıklarda tüketicinin yerleşim yeri veya işlemin yapıldığı yerdeki Tüketici Hakem Heyeti ya da Tüketici Mahkemesi, yürürlükteki parasal sınırlar ve görev kuralları çerçevesinde yetkilidir.</p><p>Siparişe özel sözleşme, tüketicinin ön bilgileri teyit edip ödeme yükümlülüğü doğuran siparişi onaylamasıyla kurulur ve kalıcı veri saklayıcısıyla tüketiciye iletilir.</p></>,
    },
  ];

  return (
    <LegalPage
      eyebrow="Yasal metin"
      title="Mesafeli Satış Sözleşmesi"
      summary="Online siparişin kurulması, teslimi, cayma hakkı ve tarafların temel yükümlülükleri hakkında genel sözleşme çerçevesi."
      notice={<><strong>Önemli:</strong> Bu sayfa genel sözleşme çerçevesidir. Satıcı kimliği, sipariş kalemleri ve nihai tutarlar içeren bağlayıcı sözleşme sipariş onayından önce ayrıca sunulmalıdır.</>}
      sections={sections}
    />
  );
}
