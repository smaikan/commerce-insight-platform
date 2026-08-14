import { siteConfig } from "@/lib/site-config";
import { LegalPage, type LegalSection } from "@/modules/legal/components/legal-page";
import { SellerIdentity } from "@/modules/legal/components/seller-identity";
import { legalConfig } from "@/modules/legal/legal-config";
import { legalPageMetadata } from "@/modules/legal/metadata";

export const metadata = legalPageMetadata(
  "KVKK ve Gizlilik Politikası",
  "Kişisel verilerin hangi amaçlarla işlendiği, hukuki sebepler, aktarım, saklama, çerezler ve KVKK kapsamındaki haklar hakkında bilgi alın.",
  "/privacy-policy",
);

// Burada doğrulanabilen Storefront veri akışlarını politika düzeyinde açıklayıp faaliyet bazlı aydınlatmanın ayrıca yapılması gerektiğini koruyorum.
const sections: LegalSection[] = [
  {
    id: "controller",
    title: "Veri sorumlusu",
    content: <><p>{siteConfig.name}, online mağaza markasıdır. 6698 sayılı Kişisel Verilerin Korunması Kanunu bakımından veri sorumlusu, aşağıdaki yasal işletme kimliğiyle siparişe ve ilgili işleme faaliyetine özel aydınlatma metinlerinde açıklanır.</p><SellerIdentity /></>,
  },
  {
    id: "data-categories",
    title: "İşlenen veri kategorileri",
    content: <><p>Kullanılan özelliğe göre aşağıdaki veriler işlenebilir:</p><ul><li><strong>Kimlik ve iletişim:</strong> ad, soyad, e-posta ve telefon,</li><li><strong>Teslimat ve fatura:</strong> adres başlığı, alıcı bilgisi, il, ilçe, açık adres ve posta kodu,</li><li><strong>Müşteri işlemleri:</strong> sepet, sipariş, ürün, adet, kupon, kargo, iptal ve iade kayıtları,</li><li><strong>İşlem güvenliği:</strong> oturum/guest tanımlayıcıları, güvenlik doğrulaması, IP, zaman ve teknik kayıtlar,</li><li><strong>Finansal işlem:</strong> ödeme entegrasyonu etkinleştirildiğinde ödeme durumu ve sağlayıcı referansı; tam kart verisinin mağaza uygulamasında saklandığı varsayılmaz.</li></ul></>,
  },
  {
    id: "purposes",
    title: "İşleme amaçları",
    content: <ul><li>Sepetin çalışması ve siparişin oluşturulması,</li><li>Ödeme, faturalama, kargo, teslimat ve müşteri iletişiminin yürütülmesi,</li><li>İptal, iade, değişim, talep ve uyuşmazlıkların yönetilmesi,</li><li>Hesap, oturum, dolandırıcılık ve hizmet güvenliğinin korunması,</li><li>Yasal saklama, yetkili kurum talepleri ve hakların tesisi/kullanılması,</li><li>Ayrı ve geçerli tercih bulunması halinde pazarlama iletişimi veya zorunlu olmayan teknolojilerin kullanılması.</li></ul>,
  },
  {
    id: "legal-bases",
    title: "Hukuki sebepler",
    content: <><p>Veri işleme faaliyetine göre KVKK madde 5 kapsamındaki sözleşmenin kurulması veya ifası için zorunluluk, veri sorumlusunun hukuki yükümlülüğü, bir hakkın tesisi/kullanılması/korunması ve temel haklara zarar vermeyen meşru menfaat şartlarına dayanılabilir.</p><p>Açık rıza gereken pazarlama veya zorunlu olmayan teknoloji faaliyetleri, sözleşmenin zorunlu şartı gibi sunulmaz; aydınlatma ile açık rıza birbirinden ayrı yürütülür. Faaliyete özgü hukuki sebep, veri elde edilirken gösterilen aydınlatma metninde ayrıca belirtilmelidir.</p></>,
  },
  {
    id: "collection-method",
    title: "Toplama yöntemi",
    content: <p>Veriler; checkout, hesap, sepet, sipariş ve iade formlarında kullanıcı tarafından; çerez ve benzeri zorunlu teknik mekanizmalardan; mağaza sunucusu, güvenlik servisi, kargo ve ödeme sağlayıcılarının işlem cevaplarından elektronik ortamda elde edilebilir.</p>,
  },
  {
    id: "transfers",
    title: "Aktarım ve hizmet sağlayıcılar",
    content: <><p>Veriler yalnız gerekli olduğu ölçüde kargo/lojistik şirketlerine, ödeme kuruluşuna, barındırma ve güvenlik hizmeti sağlayıcılarına, profesyonel danışmanlara ve kanunen yetkili kamu kurumlarına amaçla sınırlı aktarılabilir.</p><p>Yurt dışına veri aktarımı varsa KVKK madde 9’daki güncel aktarım şartlarından uygun olanı sağlanmadan aktarım yapılmaz. Alıcı grubu, aktarım amacı ve kullanılan güvence faaliyete özgü aydınlatmada somutlaştırılmalıdır.</p></>,
  },
  {
    id: "retention",
    title: "Saklama ve imha",
    content: <p>Kişisel veriler işleme amacı ve ilgili vergi, ticaret, tüketici, ödeme, sözleşme veya uyuşmazlık mevzuatının gerektirdiği süre boyunca saklanır. Süre sona erdiğinde ve başka hukuki sebep kalmadığında veri; niteliğine göre silinir, yok edilir veya anonim hâle getirilir. Kesin süreler işletmenin veri envanteri ve faaliyete özgü aydınlatma metninde belirlenmelidir.</p>,
  },
  {
    id: "cookies",
    title: "Çerezler ve yerel teknolojiler",
    content: <><p>Sepetin ve güvenli guest sipariş erişiminin çalışması, CSRF koruması, oturum devamlılığı ve güvenlik kontrolü için zorunlu çerezler kullanılabilir. Bu çerezler hizmetin talep edilen işlevini sağlar ve pazarlama amacı taşımaz.</p><p>Analitik, kişiselleştirme veya reklam gibi zorunlu olmayan teknolojiler eklenirse kullanım amacı, sağlayıcısı, süresi ve tercih kontrolü ayrı çerez bilgilendirmesinde gösterilmelidir.</p></>,
  },
  {
    id: "rights",
    title: "KVKK kapsamındaki haklarınız",
    content: <><p>KVKK madde 11 çerçevesinde kişisel verinizin işlenip işlenmediğini öğrenme, işlenmişse bilgi talep etme, amacını ve uygun kullanılıp kullanılmadığını öğrenme, aktarılan kişileri bilme, düzeltme, şartları varsa silme/yok etme ve bu işlemlerin alıcılara bildirilmesini isteme haklarına sahipsiniz.</p><p>Ayrıca münhasıran otomatik sistemlerle analiz sonucu aleyhe bir sonuca itiraz edebilir ve kanuna aykırı işleme nedeniyle zararın giderilmesini talep edebilirsiniz.</p></>,
  },
  {
    id: "application",
    title: "Başvuru yöntemi",
    content: <><p>Başvuruda ad-soyad, kimlik/iletişim doğrulamasına yeterli bilgi, talebin konusu ve varsa destekleyici belgeler bulunmalıdır. Güvenlik amacıyla yalnız gerekli doğrulama istenir.</p><p>Başvuru kanalı: <strong>{legalConfig.email || "Yayın öncesinde veri sorumlusu başvuru e-postası tanımlanmalıdır."}</strong></p><p>Başvurular yasal süre içinde yanıtlanır. Veri sorumlusuna yapılan başvurunun sonuçsuz kalması veya cevabın yetersiz bulunması halinde, kanuni şart ve süreler içinde Kişisel Verileri Koruma Kuruluna şikâyet hakkı saklıdır.</p></>,
  },
  {
    id: "official-sources",
    title: "Resmî kaynaklar ve güncellemeler",
    content: <><p>KVKK’nın <a href="https://www.kvkk.gov.tr/Icerik/2033/Aydinlatma-Yukumlulugu-" target="_blank" rel="noreferrer">Aydınlatma Yükümlülüğü</a> açıklaması ve <a href="https://www.kvkk.gov.tr/Icerik/6765/AYDINLATMA-YUKUMLULUGUNUN-YERINE-GETIRILMESI-HAKKINDA-KAMUOYU-DUYURUSU" target="_blank" rel="noreferrer">aydınlatma yükümlülüğü kamuoyu duyurusu</a> incelenebilir.</p><p>Bu genel gizlilik politikası, kişisel verinin elde edildiği anda sunulması gereken faaliyete özgü KVKK aydınlatma metninin veya gerektiğinde ayrı açık rıza beyanının yerine geçmez. Süreç, sağlayıcı veya mevzuat değiştiğinde politika güncellenir.</p></>,
  },
];

// Burada KVKK ve gizlilik politikasını kullanıcı onayı istemeyen, bilgilendirici Server Component sayfası olarak sunuyorum.
export default function PrivacyPolicyPage() {
  return (
    <LegalPage
      eyebrow="Gizlilik"
      title="KVKK ve Gizlilik Politikası"
      summary="Kişisel verilerin hangi süreçlerde, hangi amaç ve hukuki sebeplerle işlendiğini; aktarım, saklama ve başvuru haklarını açıklayan genel politika."
      notice={<><strong>Şeffaflık notu:</strong> Bu genel politika, checkout veya üyelik gibi veri toplama anlarında sunulması gereken faaliyete özgü KVKK aydınlatma metinlerinin yerine geçmez.</>}
      sections={sections}
    />
  );
}
