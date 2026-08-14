import type { Metadata } from "next";

import { getAccountAddresses, getAccountOrders, getAccountUser } from "@/modules/account/api";
import { AccountOverview } from "@/modules/account/components/account-overview";
import { withAccountSession } from "@/modules/account/session";

export const metadata: Metadata = { title: "Hesap Genel Bakış" };

// Burada genel bakış için bağımsız özel verileri paralel alarak render waterfall oluşturmuyorum.
export default async function AccountPage() {
  const [user, addresses, orders] = await withAccountSession("/account", () => Promise.all([
    getAccountUser(),
    getAccountAddresses(),
    getAccountOrders({ pageNumber: 1, pageSize: 3 }),
  ]));

  return <AccountOverview user={user} addresses={addresses} orders={orders} />;
}
