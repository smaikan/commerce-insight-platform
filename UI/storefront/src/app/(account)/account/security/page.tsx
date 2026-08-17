import type { Metadata } from "next";

import { getAccountSessions } from "@/modules/account/api";
import { AccountSecurityView } from "@/modules/account/components/account-security-view";
import { withAccountSession } from "@/modules/account/session";

export const metadata: Metadata = { title: "Güvenlik" };

// Burada aktif oturumları kullanıcı scope'unda sunucudan okuyup etkileşimli güvenlik görünümüne yalnız güvenli özeti aktarıyorum.
export default async function AccountSecurityPage() {
  const sessions = await withAccountSession("/account/security", getAccountSessions);
  return <AccountSecurityView sessions={sessions} />;
}
