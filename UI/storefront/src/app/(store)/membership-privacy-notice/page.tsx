import Link from "next/link";

import { LegalPage, type LegalSection } from "@/modules/legal/components/legal-page";
import { SellerIdentity } from "@/modules/legal/components/seller-identity";
import { legalConfig } from "@/modules/legal/legal-config";
import { legalPageMetadata } from "@/modules/legal/metadata";
import { getPublicStoreSettings } from "@/modules/store-settings/api";

export const metadata = legalPageMetadata(
  "Üyelik KVKK Aydınlatma Metni",
  "Müşteri hesabı oluşturulurken kişisel verilerin hangi amaçlarla, hangi hukuki sebeplerle ve kimlere aktarılabilecek şekilde işlendiğini inceleyin.",
  "/membership-privacy-notice",
);

// Burada yalnız üyelik ve müşteri hesabı veri akışına özgü KVKK aydınlatma unsurlarını açık rızadan ayrı biçimde açıklıyorum.
export default async function MembershipPrivacyNoticePage() {
  const settings = await getPublicStoreSettings().catch(() => null);
  const supportEmail = legalConfig.email || settings?.supportEmail?.trim() || "Yayın öncesinde veri sorumlusu başvuru e-postası tanımlanmalıdır.";

  const sections: LegalSection[] = [
    {
      id: "controller",
      title: "Veri sorumlusu",
      content: (
        <>
          <p>6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında üyelik sürecinin veri sorumlusu aşağıdaki işletmedir.</p>
          <SellerIdentity settings={settings} />
        </>
      ),
    },
    {
      id: "data-categories",
      title: "İşlenen kişisel veriler",
      content: (
        <ul>
          <li><strong>Kimlik ve iletişim:</strong> ad, soyad, e-posta ve kullanıcı tarafından sağlanırsa telefon numarası,</li>
          <li><strong>Müşteri işlemleri:</strong> müşteri numarası, hesap tercihleri, favoriler, kullanıcıyla ilişkilendirilen sepet, adres, sipariş, iptal ve iade kayıtları,</li>
          <li><strong>İşlem güvenliği:</strong> oturum tanımlayıcıları, giriş/çıkış ve güvenlik kayıtları, IP, cihaz ve teknik işlem bilgileri,</li>
          <li><strong>Talep ve uyuşmazlık:</strong> destek başvuruları, hesap değişiklikleri ve hak kullanımına ilişkin kayıtlar.</li>
        </ul>
      ),
    },
    {
      id: "purposes",
      title: "İşleme amaçları",
      content: (
        <ul>
          <li>Müşteri hesabının oluşturulması, doğrulanması ve yönetilmesi,</li>
          <li>Giriş, oturum, şifre yenileme ve hesap güvenliği süreçlerinin yürütülmesi,</li>
          <li>Favori, sepet, adres, sipariş ve müşteri hizmetleri özelliklerinin kullanıcı hesabıyla ilişkilendirilmesi,</li>
          <li>Dolandırıcılık, yetkisiz erişim ve kötüye kullanımın önlenmesi,</li>
          <li>Hukuki yükümlülüklerin yerine getirilmesi ve bir hakkın tesisi, kullanılması veya korunması.</li>
        </ul>
      ),
    },
    {
      id: "legal-bases",
      title: "Hukuki sebepler",
      content: (
        <>
          <p>Üyelik verileri, işleme faaliyetine göre KVKK madde 5/2 kapsamında bir sözleşmenin kurulması veya ifasıyla doğrudan ilgili olması, veri sorumlusunun hukuki yükümlülüğünü yerine getirmesi, bir hakkın tesisi/kullanılması/korunması ve temel haklara zarar vermeyen meşru menfaat şartlarına dayanılarak işlenebilir.</p>
          <p>Bu aydınlatma metninin okunduğunun işaretlenmesi açık rıza değildir. Pazarlama veya açık rıza gerektiren başka bir işlem yapılacaksa ilgili tercih ayrıca ve üyeliğin zorunlu şartı olmadan sunulur.</p>
        </>
      ),
    },
    {
      id: "collection-method",
      title: "Toplama yöntemi",
      content: (
        <p>Veriler; kayıt, giriş, hesap, güvenlik, adres ve müşteri işlemleri sırasında kullanıcının sağladığı bilgilerden; mağaza ve API sunucularındaki zorunlu oturum/güvenlik kayıtlarından elektronik ortamda otomatik veya kısmen otomatik yollarla elde edilir.</p>
      ),
    },
    {
      id: "transfers",
      title: "Aktarım ve alıcı grupları",
      content: (
        <p>Kişisel veriler, yalnız ilgili amaç için gerekli olduğu ölçüde barındırma, e-posta, güvenlik ve teknik destek hizmeti sağlayıcılarına; sipariş verilmesi halinde ödeme ve kargo/lojistik kuruluşlarına, profesyonel danışmanlara ve kanunen yetkili kamu kurumlarına aktarılabilir. Yurt dışı aktarım söz konusuysa KVKK madde 9’daki güncel şartlardan uygun olanı sağlanır.</p>
      ),
    },
    {
      id: "retention",
      title: "Saklama süresi",
      content: (
        <p>Üyelik verileri hesabın açık olduğu süre boyunca ve sonrasında ilgili vergi, ticaret, tüketici, güvenlik, sözleşme veya uyuşmazlık yükümlülüklerinin gerektirdiği sürelerle sınırlı olarak saklanır. Amaç ve hukuki sebep sona erdiğinde veriler niteliğine göre silinir, yok edilir veya anonim hâle getirilir.</p>
      ),
    },
    {
      id: "rights",
      title: "KVKK kapsamındaki haklarınız",
      content: (
        <p>KVKK madde 11 kapsamında kişisel verilerinizin işlenip işlenmediğini öğrenme, bilgi talep etme, amacına uygun kullanılıp kullanılmadığını ve aktarılan kişileri öğrenme, yanlış veya eksik verilerin düzeltilmesini isteme, şartları varsa silme/yok etme ve bu işlemlerin alıcılara bildirilmesini talep etme haklarına sahipsiniz. Ayrıca otomatik analiz sonucu aleyhinize doğan sonuca itiraz edebilir ve kanuna aykırı işleme nedeniyle zararın giderilmesini isteyebilirsiniz.</p>
      ),
    },
    {
      id: "application",
      title: "Başvuru yöntemi",
      content: (
        <>
          <p>Başvurunuzda ad-soyad, kimlik ve iletişim doğrulamasına yeterli bilgi, talebinizin konusu ve varsa destekleyici belgeler bulunmalıdır. Güvenlik için yalnız gerekli doğrulama bilgileri istenir.</p>
          <p>Başvuru kanalı: <strong>{supportEmail}</strong></p>
        </>
      ),
    },
    {
      id: "related-documents",
      title: "İlgili metinler ve resmî kaynak",
      content: (
        <>
          <p>Üyelik şartları için <Link href="/membership-agreement">Üyelik Sözleşmesi</Link>, daha geniş veri işleme ve çerez açıklamaları için <Link href="/privacy-policy">KVKK ve Gizlilik Politikası</Link> incelenebilir.</p>
          <p>Aydınlatma yükümlülüğünün kapsamı için Kişisel Verileri Koruma Kurumunun <a href="https://www.kvkk.gov.tr/Icerik/4132/aydinlatma-yukumlulugunun-yerine-getirilmesinde-uyulacak-usul-ve-esaslar-hakkinda-teblig" target="_blank" rel="noreferrer">Aydınlatma Yükümlülüğü Tebliği</a> resmî kaynağına bakılabilir.</p>
        </>
      ),
    },
  ];

  return (
    <LegalPage
      eyebrow="Üyelik ve kişisel veriler"
      title="Üyelik KVKK Aydınlatma Metni"
      summary="Hesap oluşturma ve kullanma sırasında işlenen kişisel veriler, amaçlar, hukuki sebepler, aktarım, saklama ve başvuru hakları."
      notice={<><strong>Aydınlatma notu:</strong> Bu metin bir açık rıza veya pazarlama izni değildir. Aydınlatmanın sunulması kullanıcı onayına bağlı değildir; kayıt formundaki işaretleme yalnız metnin kullanıcıya sunulduğunu ve okunduğu beyanını gösterir.</>}
      sections={sections}
      lastUpdated="14 Ağustos 2026"
    />
  );
}
