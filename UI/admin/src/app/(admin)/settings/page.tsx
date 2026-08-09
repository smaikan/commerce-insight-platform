import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { SettingsOverview } from "@/modules/settings/components/settings-overview";

export const metadata: Metadata = { title: "Ayarlar" };

// Burada ayarlar merkezini yalnızca doğrulanmış yönetici oturumuna açıyorum.
export default async function SettingsPage() {
  await requireAdminPageSession("/settings");
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader title="Ayarlar" description="Mağaza operasyonlarını, vergi ve teslimat tanımlarını, kişisel hesabınızı tek merkezden yönetin." />
      <SettingsFrame activeHref="/settings"><SettingsOverview /></SettingsFrame>
    </div>
  );
}
