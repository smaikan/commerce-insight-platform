import Link from "next/link";
import { settingsGroups } from "@/modules/settings/catalog";

// Burada uygulanmış ve planlanmış ayarları aynı profesyonel bilgi mimarisinde görünür tutuyorum.
export function SettingsOverview() {
  return (
    <div className="grid gap-4 xl:grid-cols-2">
      {settingsGroups.map((group) => (
        <section key={group.title} className="overflow-hidden rounded-xl border border-border bg-surface">
          <header className="border-b border-border bg-surface-subtle/60 px-4 py-3.5 sm:px-5">
            <h2 className="text-base font-semibold text-foreground">{group.title}</h2>
            <p className="mt-1 text-sm leading-5 text-muted">{group.description}</p>
          </header>
          <div className="divide-y divide-border">
            {group.options.map((option) =>
              option.href ? (
                <Link key={option.title} href={option.href} className="group flex min-h-20 items-center gap-3 px-4 py-3 hover:bg-surface-subtle/70 sm:px-5">
                  <span className="flex size-9 shrink-0 items-center justify-center rounded-lg border border-primary/20 bg-primary/5 text-primary" aria-hidden="true">
                    <ArrowIcon />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="block text-sm font-semibold text-foreground group-hover:text-primary-hover">{option.title}</span>
                    <span className="mt-0.5 block text-xs leading-5 text-muted">{option.description}</span>
                  </span>
                  <span className="text-lg text-muted" aria-hidden="true">›</span>
                </Link>
              ) : (
                <div key={option.title} className="flex min-h-20 items-center gap-3 px-4 py-3 sm:px-5">
                  <span className="flex size-9 shrink-0 items-center justify-center rounded-lg border border-border bg-surface-subtle text-muted" aria-hidden="true">
                    <ClockIcon />
                  </span>
                  <span className="min-w-0 flex-1">
                    <span className="flex flex-wrap items-center gap-2">
                      <span className="text-sm font-semibold text-foreground">{option.title}</span>
                      <span className="rounded-md border border-border-strong bg-surface-subtle px-1.5 py-0.5 text-[11px] font-semibold text-muted">Geliştirme aşamasında</span>
                    </span>
                    <span className="mt-0.5 block text-xs leading-5 text-muted">{option.description}</span>
                  </span>
                </div>
              ),
            )}
          </div>
        </section>
      ))}
    </div>
  );
}

// Burada kullanılabilir ayar satırını küçük ve tutarlı bir yön oku ile destekliyorum.
function ArrowIcon() {
  return <svg viewBox="0 0 20 20" className="size-4 fill-none stroke-current" strokeWidth="1.8"><path d="M4 10h12M11 5l5 5-5 5" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}

// Burada geliştirme durumunu yalnızca renge dayanmayan bir saat simgesiyle destekliyorum.
function ClockIcon() {
  return <svg viewBox="0 0 20 20" className="size-4 fill-none stroke-current" strokeWidth="1.7"><circle cx="10" cy="10" r="7" /><path d="M10 6v4l2.5 1.5" strokeLinecap="round" strokeLinejoin="round" /></svg>;
}
