import { siteConfig } from "@/lib/site-config";
import type { AuthUser } from "@/lib/auth/contracts";
import { logoutAction } from "@/modules/auth/actions";
import { AdminSidebar } from "@/modules/admin-shell/components/admin-sidebar";
import { MobileNavigation } from "@/modules/admin-shell/components/mobile-navigation";

// Burada doğrulanmış yönetici kimliğini yalnız gerekli profil özeti ve server-side logout eylemiyle shell'e bağlıyorum.
export function AdminShell({ children, user }: Readonly<{ children: React.ReactNode; user: AuthUser }>) {
  return (
    <div className="min-h-dvh bg-page">
      <a
        href="#main-content"
        className="fixed left-3 top-3 z-50 -translate-y-20 rounded-lg bg-surface-strong px-4 py-2 text-sm font-semibold text-primary transition-transform focus:translate-y-0"
      >
        Ana içeriğe geç
      </a>

      <AdminSidebar siteName={siteConfig.name} />

      <div className="min-h-dvh lg:pl-72">
        <header className="sticky top-0 z-10 flex h-16 items-center gap-3 border-b border-sidebar-border bg-sidebar-elevated px-4 text-sidebar-foreground sm:px-6 lg:px-8">
          <MobileNavigation siteName={siteConfig.name} />
          <div className="min-w-0">
            <p className="truncate text-sm font-semibold text-sidebar-foreground">Yönetim Paneli</p>
            <p className="hidden truncate text-xs text-sidebar-muted sm:block">{user.firstName} {user.lastName}</p>
          </div>
          <form action={logoutAction} className="ml-auto">
            <button
              type="submit"
              className="inline-flex min-h-10 items-center justify-center rounded-lg border border-sidebar-border bg-sidebar px-3 text-sm font-semibold text-sidebar-foreground transition-colors hover:bg-sidebar-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
            >
              Çıkış yap
            </button>
          </form>
        </header>

        <main id="main-content" className="min-h-[calc(100dvh-4rem)] px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
          {children}
        </main>
      </div>
    </div>
  );
}
