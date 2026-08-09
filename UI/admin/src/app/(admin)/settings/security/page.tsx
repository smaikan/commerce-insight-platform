import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getAccountSessions } from "@/modules/settings/api";
import { SessionList } from "@/modules/settings/components/session-list";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";

export const metadata: Metadata = { title: "Oturumlar ve güvenlik" };

// Burada aktif oturum listesini server-side okuyup güvenlik işlemlerini dar istemci sınırlarına bırakıyorum.
export default async function SecuritySettingsPage() {
  const session = await requireAdminPageSession("/settings/security");
  const sessions = await getAccountSessions(session);
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Oturumlar ve güvenlik" description="Hesabınızın açık olduğu cihazları inceleyin ve tanımadığınız oturumları sonlandırın." /><SettingsFrame activeHref="/settings/security"><SessionList sessions={sessions} /></SettingsFrame></div>;
}
