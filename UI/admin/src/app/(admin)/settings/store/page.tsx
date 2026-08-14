import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { getAdminStoreSettings } from "@/modules/settings/api";
import { SettingsFrame } from "@/modules/settings/components/settings-frame";
import { StoreSettingsEditor } from "@/modules/settings/store-settings/components/store-settings-editor";

export const metadata: Metadata = { title: "Mağaza ayarları" };

export default async function StoreSettingsPage() {
  const session = await requireAdminPageSession("/settings/store");
  const settings = await getAdminStoreSettings(session);

  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Mağaza ayarları"
        description="Mağaza kimliğini, iletişim görünürlüğünü, yasal bilgileri ve storefront davranışını yönetin."
      />
      <SettingsFrame activeHref="/settings/store">
        <StoreSettingsEditor initialSettings={settings} />
      </SettingsFrame>
    </div>
  );
}
