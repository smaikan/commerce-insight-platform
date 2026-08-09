import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getAccount } from "@/modules/settings/api";
import { AccountSettings } from "@/modules/settings/components/account-settings";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";

export const metadata: Metadata = { title: "Hesabım" };

// Burada güncel hesap bilgisini server-side okuyup profil ve güvenlik formlarına iletiyorum.
export default async function AccountSettingsPage() {
  const session = await requireAdminPageSession("/settings/account");
  const user = await getAccount(session);
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Hesabım" description="Kişisel profil bilgilerinizi ve yönetici hesabınızın giriş bilgilerini güncelleyin." /><SettingsFrame activeHref="/settings/account"><AccountSettings user={user} /></SettingsFrame></div>;
}
