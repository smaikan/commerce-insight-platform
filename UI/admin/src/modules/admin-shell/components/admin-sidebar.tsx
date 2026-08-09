import { AdminNavigation } from "@/modules/admin-shell/components/admin-navigation";

export function AdminSidebar({ siteName }: { siteName: string }) {
  return (
    <aside className="fixed inset-y-0 left-0 z-20 hidden w-64 border-r border-sidebar-border bg-sidebar text-sidebar-foreground lg:flex lg:flex-col">
      <div className="flex h-14 shrink-0 items-center border-b border-sidebar-border px-4">
        <div className="min-w-0">
          <span className="block truncate text-base font-semibold tracking-tight text-sidebar-foreground">
            {siteName}
          </span>
          <span className="mt-0.5 block text-xs font-medium text-sidebar-muted">Yönetim Paneli</span>
        </div>
      </div>
      <div className="flex-1 overflow-y-auto py-3">
        <AdminNavigation />
      </div>
    </aside>
  );
}
