import { LegalPage, type LegalSection } from "@/modules/legal/components/legal-page";
import { legalPageMetadata } from "@/modules/legal/metadata";

export const metadata = legalPageMetadata(
  "Ödeme ve Teslimat",
  "Online mağazada ödeme güvenliği, sipariş tutarı, kargo seçimi, teslimat süresi ve teslim kontrolü hakkında bilgi alın.",
  "/payment-and-delivery",
);

// Burada ödeme ve teslimat bilgisini mevcut checkout sözleşmesiyle çelişmeden kullanıcı odaklı bölümlere ayırıyorum.
const sections: LegalSection[] = [
  {
    id: "payment-methods",
    title: "Ödeme yöntemleri",
    content: <><p>O anda kullanılabilen ödeme yöntemleri ve varsa yönteme özgü koşullar siparişin ödeme adımında gösterilir. Sitede görünmeyen bir ödeme yöntemi kullanılabilir kabul edilmez.</p><p>Ödeme entegrasyonunun etkin olmadığı dönemlerde online sipariş oluşturma kapalıdır. Kullanıcıdan kart veya tahsilat bilgisi alınmaz ve başarılı ödeme izlenimi verilmez.</p></>,
  },
  {
    id: "pricing",
    title: "Tutarın hesaplanması",
    content: <><p>Ürün bedeli, vergi, indirim, kupon ve kargo ücreti sipariş oluşturulmadan hemen önce sunucu tarafından yeniden doğrulanır. Sepette görülen fiyat değişmişse tüketicinin güncel durumu görmesi ve yeniden onaylaması gerekir.</p><p>Sipariş özeti; ürün adedi, birim fiyat, indirim, vergi, teslimat bedeli ve genel toplamı ayrı ve anlaşılır biçimde gösterir.</p></>,
  },
  {
    id: "security",
    title: "Ödeme güvenliği",
    content: <><p>Ödeme hizmeti etkinleştirildiğinde tahsilat, yalnız yapılandırılmış ödeme kuruluşu üzerinden ve siparişe ait doğrulanmış tutarla başlatılır. Kart bilgilerinin mağaza uygulamasında saklandığı varsayılmaz; ödeme sağlayıcısının adı ve koşulları ödeme adımında açıklanır.</p><p>Başarılı, bekleyen veya başarısız ödeme durumu yalnız yetkili servis cevabına göre gösterilir. Ağ kesintisinde ikinci bir tahsilat başlatmak yerine mevcut ödeme durumu kontrol edilir.</p></>,
  },
  {
    id: "shipping-selection",
    title: "Kargo seçimi ve ücret",
    content: <p>Aktif kargo yöntemleri, adları ve ücretleri teslimat adımında gösterilir. Seçilen yöntemin geçerliliği ve ücreti sipariş oluşturulurken yeniden doğrulanır. Kampanya veya ücretsiz kargo koşulları varsa uygulanmadan önce sipariş özetinde görünür.</p>,
  },
  {
    id: "delivery-time",
    title: "Hazırlık ve teslimat süresi",
    content: <><p>Tahmini hazırlık ve teslimat süresi ürün, teslimat bölgesi ve kargo yöntemine göre sipariş öncesinde belirtilir. Mevzuatta daha kısa süre taahhüt edilmedikçe ürün en geç 3 iş günü içinde kargoya verilir. Teslimatın 3 iş gününden uzun süreceği durumlarda tüketici bilgilendirilir ve onay alınır.</p><p>Stok veya teslimatın sonradan imkânsızlaşması halinde tüketici gecikmeden bilgilendirilir; varsa tahsil edilen bedeller mevzuattaki süre ve yönteme uygun olarak iade edilir.</p></>,
  },
  {
    id: "address",
    title: "Teslimat adresi ve alıcı",
    content: <><p>Tüketici; alıcı adı, telefon, il, ilçe ve açık adres bilgilerinin doğru ve eksiksiz olmasından sorumludur. Sipariş tamamlandıktan sonra adres değişikliği ancak sevkiyat durumu ve kargo sağlayıcısının imkânları elveriyorsa yapılabilir.</p><p>Yanlış veya eksik adres nedeniyle teslim edilemeyen gönderilerde tüketici bilgilendirilir; yeniden gönderim koşulları somut masraf ve mevzuat çerçevesinde belirlenir.</p></>,
  },
  {
    id: "delivery-check",
    title: "Teslim sırasında kontrol",
    content: <><p>Paketin belirgin biçimde hasarlı, açılmış veya eksik görünmesi halinde mümkünse kargo görevlisiyle tutanak tutulması ve durumun gecikmeden mağazaya bildirilmesi önerilir.</p><p>Tutanak bulunmaması tüketicinin kanuni ayıplı mal haklarını kendiliğinden ortadan kaldırmaz. Ürün, ambalaj ve gönderi etiketi inceleme tamamlanana kadar korunmalıdır.</p></>,
  },
];

// Burada ödeme ve teslimat sayfasını doğrudan crawl edilebilir statik içerik olarak sunuyorum.
export default function PaymentAndDeliveryPage() {
  return <LegalPage eyebrow="Müşteri bilgilendirmesi" title="Ödeme ve Teslimat" summary="Sipariş tutarının doğrulanmasından ürünün teslim alınmasına kadar ödeme ve kargo sürecinin temel kuralları." sections={sections} />;
}
