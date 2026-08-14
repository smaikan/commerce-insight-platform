import { siteConfig } from "@/lib/site-config";
import { legalConfig } from "@/modules/legal/legal-config";

// Burada yalnız yapılandırılmış işletme bilgilerini gösterip eksik yasal kimlik alanlarının doldurulması gerektiğini açıkça belirtiyorum.
export function SellerIdentity() {
  return (
    <dl>
      <dt>Mağaza adı</dt><dd>{siteConfig.name}</dd>
      <dt>Yasal unvan</dt><dd>{legalConfig.businessName || "Siparişe özel ön bilgilendirme formunda gösterilecektir."}</dd>
      <dt>Adres</dt><dd>{legalConfig.address || "Siparişe özel ön bilgilendirme formunda gösterilecektir."}</dd>
      <dt>E-posta</dt><dd>{legalConfig.email || "Yayın öncesinde yasal iletişim adresi tanımlanmalıdır."}</dd>
      <dt>Telefon</dt><dd>{legalConfig.phone || "Yayın öncesinde yasal iletişim numarası tanımlanmalıdır."}</dd>
      <dt>MERSİS / VKN</dt>
      <dd>{legalConfig.mersisNumber || legalConfig.taxNumber || "Yayın öncesinde işletme kimliği tanımlanmalıdır."}</dd>
      {legalConfig.taxOffice ? <><dt>Vergi dairesi</dt><dd>{legalConfig.taxOffice}</dd></> : null}
    </dl>
  );
}
