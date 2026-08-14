import type { Metadata } from "next";

import { getAccountAddresses } from "@/modules/account/api";
import { AddressesView } from "@/modules/account/components/addresses-view";
import { withAccountSession } from "@/modules/account/session";

export const metadata: Metadata = { title: "Adreslerim" };

// Burada adres sayfasını yalnız oturumdaki müşterinin cache dışı kayıtlarıyla oluşturuyorum.
export default async function AccountAddressesPage() {
  const addresses = await withAccountSession("/account/addresses", getAccountAddresses);
  return <AddressesView addresses={addresses} />;
}
