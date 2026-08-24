import { siteConfig } from "@/lib/site-config";
import { legalConfig } from "@/modules/legal/legal-config";
import type { PublicStoreSettings } from "@/modules/store-settings/types";

type SellerIdentityProps = {
  settings?: PublicStoreSettings | null;
};

// Burada mağaza ayarlarından gelen güncel bilgileri ve tanımlı yasal işletme kimliğini gösteriyorum.
export function SellerIdentity({ settings }: SellerIdentityProps = {}) {
  const storeName = settings?.displayName?.trim() || siteConfig.name;
  const legalName = legalConfig.businessName || "Siparişe özel ön bilgilendirme formunda gösterilecektir.";
  const address = legalConfig.address || "Siparişe özel ön bilgilendirme formunda gösterilecektir.";
  const email = legalConfig.email || settings?.supportEmail?.trim() || "Yayın öncesinde yasal iletişim adresi tanımlanmalıdır.";
  const phone = legalConfig.phone || settings?.supportPhone?.trim() || "Yayın öncesinde yasal iletişim numarası tanımlanmalıdır.";
  const mersisOrTax = legalConfig.mersisNumber || legalConfig.taxNumber || null;

  return (
    <dl>
      <dt>Mağaza adı</dt><dd>{storeName}</dd>
      <dt>Yasal unvan</dt><dd>{legalName}</dd>
      <dt>Adres</dt><dd>{address}</dd>
      <dt>E-posta</dt><dd>{email}</dd>
      <dt>Telefon</dt><dd>{phone}</dd>
      {mersisOrTax ? <><dt>MERSİS / VKN</dt><dd>{mersisOrTax}</dd></> : null}
      {legalConfig.taxOffice ? <><dt>Vergi dairesi</dt><dd>{legalConfig.taxOffice}</dd></> : null}
    </dl>
  );
}
