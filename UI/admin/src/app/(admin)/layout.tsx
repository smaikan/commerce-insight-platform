import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { AdminShell } from "@/modules/admin-shell/components/admin-shell";
import { getAdminStoreSettings } from "@/modules/settings/api";

export const metadata: Metadata = {
  robots: {
    index: false,
    follow: false,
  },
};

// Burada shell'i göstermeden önce backend tarafından doğrulanmış aktif Admin oturumunu zorunlu tutuyorum.
export default async function AdminLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  const session = await requireAdminPageSession("/dashboard");
  // Burada yönetim kabuğunun geçici uygulama adını gerçek StoreSettings kimliğiyle değiştiriyorum.
  const settings = await getAdminStoreSettings(session);
  return (
    <AdminShell
      user={session.user}
      store={{
        displayName: settings.displayName,
      }}
    >
      {children}
    </AdminShell>
  );
}
