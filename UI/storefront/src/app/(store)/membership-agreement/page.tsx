import Link from "next/link";

import { siteConfig } from "@/lib/site-config";
import { LegalPage, type LegalSection } from "@/modules/legal/components/legal-page";
import { SellerIdentity } from "@/modules/legal/components/seller-identity";
import { legalPageMetadata } from "@/modules/legal/metadata";

export const metadata = legalPageMetadata(
  "Üyelik Sözleşmesi",
  "Müşteri hesabının oluşturulması, kullanılması, güvenliği, kapatılması ve tarafların temel yükümlülükleri hakkında bilgi alın.",
  "/membership-agreement",
);

// Burada üyelik hesabının kurulması ve kullanılması için sipariş sözleşmesinden ayrı, sade ve kullanıcı odaklı hükümleri topluyorum.
const sections: LegalSection[] = [
  {
    id: "parties-and-scope",
    title: "Taraflar ve sözleşmenin kapsamı",
    content: <><p>Bu Üyelik Sözleşmesi, {siteConfig.name} mağazasında müşteri hesabı oluşturan kişi ile aşağıda bilgileri bulunan hizmet sağlayıcı arasındaki üyelik ilişkisinin esaslarını düzenler. Üyelik; ürün inceleme, favori ve sepet yönetimi, adres kaydetme, siparişleri takip etme ve hesap güvenliği araçlarından yararlanmayı sağlar.</p><SellerIdentity /></>,
  },
  {
    id: "establishment",
    title: "Üyeliğin kurulması",
    content: <><p>Kullanıcı, kayıt formundaki zorunlu bilgileri doğru ve güncel biçimde sağlayıp Üyelik Sözleşmesi’ni kabul ederek hesap oluşturma talebinde bulunur. Üyelik, kayıt işleminin sistem tarafından başarıyla tamamlanmasıyla kurulur.</p><p>Hesap açmak ücretli değildir ve kullanıcıyı ürün satın almaya zorlamaz. Bir sipariş verilmesi halinde ürün, fiyat, teslimat, ödeme ve cayma koşulları ayrıca sunulan sipariş bilgileri ile Mesafeli Satış Sözleşmesi’ne tabidir.</p></>,
  },
  {
    id: "account-information",
    title: "Hesap bilgilerinin doğruluğu",
    content: <p>Kullanıcı; kendi adına, erişebildiği bir e-posta adresiyle hesap oluşturduğunu ve sağladığı bilgilerin doğru olduğunu kabul eder. Bilgiler değiştiğinde hesap alanından güncellenmelidir. Başkasına ait kimlik veya iletişim bilgilerinin izinsiz kullanılması yasaktır.</p>,
  },
  {
    id: "security",
    title: "Şifre ve hesap güvenliği",
    content: <><p>Kullanıcı şifresini gizli tutmak, kolay tahmin edilemeyen bir şifre seçmek ve hesabının izinsiz kullanıldığını düşündüğünde mağazaya gecikmeden bildirmekle sorumludur. Oturum ve güvenlik kontrolleri, hesabın ve diğer kullanıcıların korunması amacıyla uygulanabilir.</p><p>Mağaza hiçbir zaman kullanıcıdan şifresini e-posta veya telefonla paylaşmasını istemez.</p></>,
  },
  {
    id: "service-use",
    title: "Hizmetin kullanımı",
    content: <><p>Kullanıcı hesabı hukuka, dürüstlük kurallarına ve hizmetin amacına uygun kullanılmalıdır. Sistemin güvenliğini bozma, yetkisiz erişim deneme, yanıltıcı bilgi sağlama, otomatik veya aşırı isteklerle hizmeti aksatma ve diğer kullanıcıların haklarını ihlal etme girişimleri yasaktır.</p><p>Favoriler, sepet ve benzeri kolaylıklar satın alma garantisi oluşturmaz; fiyat, stok ve satışa uygunluk sipariş sırasında API tarafından yeniden doğrulanır.</p></>,
  },
  {
    id: "orders-and-payments",
    title: "Sipariş ve ödeme ilişkisi",
    content: <p>Üyelik Sözleşmesi tek başına satış ilişkisi kurmaz. Her sipariş; kullanıcıya gösterilen ürün, varyant, adet, fiyat, vergi, kargo, teslimat ve ödeme bilgileri üzerinden ayrıca değerlendirilir. Siparişe özgü ön bilgilendirme ve mesafeli satış hükümleri üyelik hükümlerinden bağımsız olarak uygulanır.</p>,
  },
  {
    id: "communications",
    title: "Bildirimler ve ticari iletişim",
    content: <><p>Hesabın güvenliği, kayıt işlemi, sipariş, ödeme, teslimat, iptal ve iade gibi hizmetin yürütülmesi için gerekli bildirimler kullanıcıya gönderilebilir.</p><p>Reklam, kampanya veya pazarlama amaçlı ticari elektronik ileti izni üyeliğin zorunlu şartı değildir. Böyle bir tercih sunulursa ayrı, açık ve geri alınabilir bir seçimle yönetilir.</p></>,
  },
  {
    id: "personal-data",
    title: "Kişisel veriler",
    content: <p>Üyelik sırasında işlenen kişisel veriler, bu sözleşmeden ayrı olan <Link href="/membership-privacy-notice">Üyelik KVKK Aydınlatma Metni</Link> kapsamında açıklanır. Genel veri işleme, çerez ve başvuru bilgileri için <Link href="/privacy-policy">KVKK ve Gizlilik Politikası</Link> da incelenebilir. Bu sözleşmenin kabulü pazarlama izni veya açık rıza beyanı anlamına gelmez.</p>,
  },
  {
    id: "suspension-and-termination",
    title: "Üyeliğin sınırlandırılması ve sona ermesi",
    content: <><p>Kullanıcı hesabını mevcut hesap kapatma araçları üzerinden kapatmayı talep edebilir. Hesabın kapatılması, tamamlanmış siparişlere, devam eden iade veya uyuşmazlıklara ve kanuni saklama yükümlülüklerine ilişkin kayıtları kendiliğinden ortadan kaldırmaz.</p><p>Güvenlik riski, hukuka aykırı kullanım veya sözleşmenin esaslı ihlali halinde hesap geçici olarak sınırlandırılabilir ya da kapatılabilir. Mümkün ve hukuken uygun olduğu ölçüde kullanıcı sebep ve başvuru yolu hakkında bilgilendirilir.</p></>,
  },
  {
    id: "changes-and-disputes",
    title: "Değişiklikler ve uyuşmazlıklar",
    content: <><p>Hizmet veya mevzuat değişiklikleri nedeniyle sözleşme güncellenebilir. Kullanıcının haklarını önemli ölçüde etkileyen değişiklikler yürürlüğe girmeden önce uygun bir kanaldan bildirilir; gerekli olduğu durumlarda yeniden kabul alınır.</p><p>Bu sözleşme Türkiye Cumhuriyeti hukukuna tabidir. Tüketicinin emredici mevzuattan doğan hakları ile yetkili Tüketici Hakem Heyeti ve Tüketici Mahkemesine başvuru hakları saklıdır.</p></>,
  },
];

// Burada üyelik hükümlerini etkileşimsiz Server Component olarak ilk HTML içinde erişilebilir biçimde sunuyorum.
export default function MembershipAgreementPage() {
  return (
    <LegalPage
      eyebrow="Hesap ve üyelik"
      title="Üyelik Sözleşmesi"
      summary="Müşteri hesabının oluşturulması, güvenli kullanımı, hizmet kapsamı, sona ermesi ve tarafların temel yükümlülükleri."
      notice={<><strong>Yayın notu:</strong> İşletmenin yasal unvan, adres ve iletişim bilgileri canlı ortama geçmeden önce yapılandırılmalı; metin işletmenin gerçek süreçleriyle birlikte hukuk uzmanı tarafından son kez kontrol edilmelidir.</>}
      sections={sections}
      lastUpdated="14 Ağustos 2026"
    />
  );
}
