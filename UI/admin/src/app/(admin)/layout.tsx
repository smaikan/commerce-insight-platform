import type { Metadata } from "next";
import { headers } from "next/headers";
import { ADMIN_RETURN_TO_HEADER } from "@/lib/auth/constants";
import type { AdminSession } from "@/lib/auth/contracts";
import { safeReturnTo } from "@/lib/auth/policy";
import { requireAdminPageSession } from "@/lib/auth/session";
import { AdminShell } from "@/modules/admin-shell/components/admin-shell";
import { getAdminWorkQueueSummary } from "@/modules/dashboard/api";
import type { AdminWorkQueueSummaryData } from "@/modules/dashboard/types";
import { getAdminStoreSettings } from "@/modules/settings/api";

export const metadata: Metadata = {
  robots: {
    index: false,
    follow: false,
  },
};

type InitialWorkQueueState = {
  summary: AdminWorkQueueSummaryData | null;
  unavailable: boolean;
};

// Burada kritik olmayan sayaç hatasında admin kabuğunu açık tutup görünür bir yeniden deneme durumu taşıyorum.
async function getInitialWorkQueueState(session: AdminSession): Promise<InitialWorkQueueState> {
  try {
    return { summary: await getAdminWorkQueueSummary(session), unavailable: false };
  } catch {
    return { summary: null, unavailable: true };
  }
}

// Burada shell'i göstermeden önce backend tarafından doğrulanmış aktif Admin oturumunu zorunlu tutuyorum.
export default async function AdminLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  // Burada Proxy'nin doğruladığı tam route ve query bilgisini 401 sonrası oturum yenileme dönüş hedefi olarak koruyorum.
  const returnTo = safeReturnTo((await headers()).get(ADMIN_RETURN_TO_HEADER));
  const session = await requireAdminPageSession(returnTo);
  // Burada mağaza kimliğiyle kritik olmayan iş kuyruğu sayaçlarını aynı anda yüklüyorum.
  const [settings, workQueue] = await Promise.all([
    getAdminStoreSettings(session),
    getInitialWorkQueueState(session),
  ]);
  return (
    <AdminShell
      user={session.user}
      initialWorkQueueSummary={workQueue.summary}
      initialWorkQueueUnavailable={workQueue.unavailable}
      store={{
        displayName: settings.displayName,
      }}
    >
      {children}
    </AdminShell>
  );
}
