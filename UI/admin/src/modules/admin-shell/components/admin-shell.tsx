import type { AuthUser } from "@/lib/auth/contracts";
import { logoutAction } from "@/modules/auth/actions";
import { AdminSidebar } from "@/modules/admin-shell/components/admin-sidebar";
import { MobileNavigation } from "@/modules/admin-shell/components/mobile-navigation";
import type { AdminWorkQueueSummaryData } from "@/modules/dashboard/types";

// Burada doğrulanmış yönetici kimliğini yalnız gerekli profil özeti ve server-side logout eylemiyle shell'e bağlıyorum.
type AdminStoreBrand = {
  displayName: string;
};

// Burada kabuğun marka kimliğini API'den gelen gerçek mağaza adıyla taşıyorum.
export function AdminShell({
  children,
  user,
  store,
  initialWorkQueueSummary,
  initialWorkQueueUnavailable,
}: Readonly<{
  children: React.ReactNode;
  user: AuthUser;
  store: AdminStoreBrand;
  initialWorkQueueSummary: AdminWorkQueueSummaryData | null;
  initialWorkQueueUnavailable: boolean;
}>) {
  return (
    <div className="min-h-dvh bg-page">
      <a
        href="#main-content"
        className="fixed left-3 top-3 z-50 -translate-y-20 rounded-lg bg-surface-strong px-4 py-2 text-sm font-semibold text-primary transition-transform focus:translate-y-0"
      >
        Ana içeriğe geç
      </a>

      <AdminSidebar
        siteName={store.displayName}
        initialWorkQueueSummary={initialWorkQueueSummary}
        initialWorkQueueUnavailable={initialWorkQueueUnavailable}
      />

      <div className="min-h-dvh lg:pl-64">
        <header className="sticky top-0 z-10 flex h-14 items-center gap-3 border-b border-border bg-surface-strong px-4 text-foreground sm:px-5 lg:px-6">
          <MobileNavigation
            siteName={store.displayName}
            initialWorkQueueSummary={initialWorkQueueSummary}
            initialWorkQueueUnavailable={initialWorkQueueUnavailable}
          />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-foreground">{user.firstName} {user.lastName}</p>
            <p className="hidden truncate text-xs text-muted sm:block">Yönetici hesabı</p>
          </div>
          <form action={logoutAction} className="ml-auto">
            <button
              type="submit"
              className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground transition-colors hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
            >
              Çıkış yap
            </button>
          </form>
        </header>

        <main id="main-content" className="min-h-[calc(100dvh-3.5rem)] px-4 py-5 sm:px-5 lg:px-6 lg:py-6">
          {children}
        </main>
      </div>
    </div>
  );
}
