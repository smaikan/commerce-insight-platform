import Link from "next/link";
import { availableSettingsOptions } from "@/modules/settings/catalog";

// Burada ayar alt sayfalarında sabit bağlam ve kompakt bölüm navigasyonu sağlıyorum.
export function SettingsFrame({ activeHref, children }: { activeHref?: string; children: React.ReactNode }) {
  return (
    <div className="grid gap-4 lg:grid-cols-[220px_minmax(0,1fr)] lg:items-start">
      <aside className="rounded-xl border border-border bg-surface p-2 lg:sticky lg:top-4" aria-label="Ayar bölümleri">
        <Link
          href="/settings"
          aria-current={activeHref === "/settings" ? "page" : undefined}
          className={`flex min-h-10 items-center rounded-lg px-3 text-sm font-semibold ${activeHref === "/settings" ? "bg-primary-soft text-primary-hover" : "text-foreground hover:bg-surface-subtle"}`}
        >
          Tüm ayarlar
        </Link>
        <div className="my-2 border-t border-border" />
        <nav className="space-y-1">
          {availableSettingsOptions.map((option) => (
            <Link
              key={option.href}
              href={option.href}
              aria-current={activeHref === option.href ? "page" : undefined}
              className={`flex min-h-10 items-center rounded-lg px-3 text-sm font-medium ${activeHref === option.href ? "bg-primary-soft text-primary-hover" : "text-muted hover:bg-surface-subtle hover:text-foreground"}`}
            >
              {option.title}
            </Link>
          ))}
        </nav>
      </aside>
      <div className="min-w-0">{children}</div>
    </div>
  );
}
