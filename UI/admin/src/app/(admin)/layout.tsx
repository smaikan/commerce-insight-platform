import type { Metadata } from "next";
import { headers } from "next/headers";
import { ADMIN_RETURN_TO_HEADER } from "@/lib/auth/constants";
import { safeReturnTo } from "@/lib/auth/policy";
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
  // Burada Proxy'nin doğruladığı tam route ve query bilgisini 401 sonrası oturum yenileme dönüş hedefi olarak koruyorum.
  const returnTo = safeReturnTo((await headers()).get(ADMIN_RETURN_TO_HEADER));
  const session = await requireAdminPageSession(returnTo);
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
