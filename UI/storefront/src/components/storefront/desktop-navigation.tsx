"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useRef, useState } from "react";

import type { StorefrontNavigationGroup } from "@/components/storefront/navigation-types";

// Burada masaüstü navigasyonunda hover, click ve klavye odağını aynı erişilebilir disclosure davranışında birleştiriyorum.
export function DesktopNavigation({ groups }: { groups: StorefrontNavigationGroup[] }) {
  const pathname = usePathname();
  const [openGroup, setOpenGroup] = useState<string | null>(null);
  const triggerRefs = useRef<Record<string, HTMLElement | null>>({});

  function closeGroup(groupId: string, restoreFocus = false) {
    setOpenGroup((current) => current === groupId ? null : current);
    if (restoreFocus) triggerRefs.current[groupId]?.focus();
  }

  return (
    <nav className="hidden min-w-0 items-center justify-start gap-1 text-sm font-semibold text-ink-muted lg:flex" aria-label="Ana navigasyon">
      <PrimaryNavigationLink href="/" label="Ana sayfa" current={pathname === "/"} />
      <PrimaryNavigationLink href="/products" label="Katalog" current={pathname === "/products" || pathname.startsWith("/products/")} />

      {groups.filter((group) => group.items.length > 0).map((group) => {
        const isOpen = openGroup === group.id;
        const panelId = `desktop-navigation-${group.id}`;

        return (
          <div
            key={group.id}
            className="relative"
            onMouseEnter={() => setOpenGroup(group.id)}
            onMouseLeave={() => closeGroup(group.id)}
            onFocusCapture={() => setOpenGroup(group.id)}
            onBlurCapture={(event) => {
              if (!event.currentTarget.contains(event.relatedTarget as Node | null)) closeGroup(group.id);
            }}
            onKeyDown={(event) => {
              if (event.key === "Escape") {
                event.preventDefault();
                closeGroup(group.id, true);
              }
            }}
          >
            {group.href ? (
              <div className={`flex min-h-11 items-center transition-colors ${isOpen ? "text-brand-700" : "hover:text-brand-700"}`}>
                <Link
                  ref={(element) => { triggerRefs.current[group.id] = element; }}
                  href={group.href}
                  prefetch={false}
                  aria-current={pathname === group.href ? "page" : undefined}
                  className="focus-ring inline-flex min-h-11 items-center py-2 pr-1 pl-3"
                  onClick={() => closeGroup(group.id)}
                >
                  {group.label}
                </Link>
                <button
                  type="button"
                  className="focus-ring inline-flex size-9 items-center justify-center"
                  aria-label={`${group.label} alt menüsünü ${isOpen ? "kapat" : "aç"}`}
                  aria-expanded={isOpen}
                  aria-controls={panelId}
                  onClick={() => setOpenGroup((current) => current === group.id ? null : group.id)}
                >
                  <ChevronIcon open={isOpen} />
                </button>
              </div>
            ) : (
              <button
                ref={(element) => { triggerRefs.current[group.id] = element; }}
                type="button"
                className={`focus-ring inline-flex min-h-11 items-center gap-1.5 px-3 transition-colors ${
                  isOpen ? "text-brand-700" : "hover:text-brand-700"
                }`}
                aria-expanded={isOpen}
                aria-controls={panelId}
                onClick={() => setOpenGroup((current) => current === group.id ? null : group.id)}
              >
                {group.label}
                <ChevronIcon open={isOpen} />
              </button>
            )}

            {isOpen ? (
              <div id={panelId} className="absolute top-full left-0 z-50 w-[min(32rem,calc(100vw-2rem))] pt-3">
                <section className="overflow-hidden rounded-xl border border-line bg-surface shadow-panel" aria-label={`${group.label} bağlantıları`}>
                  <div className="flex items-center justify-between gap-4 border-b border-line bg-surface-subtle px-5 py-3">
                    <h2 className="text-sm font-bold text-ink">{group.label}</h2>
                    {group.href ? (
                      <Link href={group.href} prefetch={false} onClick={() => closeGroup(group.id)} className="focus-ring text-xs font-bold text-brand-700 hover:text-brand-950">
                        Tümünü gör <span aria-hidden="true">→</span>
                      </Link>
                    ) : (
                      <span className="text-xs text-ink-muted">{group.items.length} başlık</span>
                    )}
                  </div>
                  <ul className="grid max-h-[min(26rem,65vh)] grid-cols-2 gap-1 overflow-y-auto p-3">
                    {group.items.map((item) => (
                      <li key={item.id}>
                        <Link
                          href={item.href}
                          prefetch={false}
                          aria-current={pathname === item.href ? "page" : undefined}
                          onClick={() => closeGroup(group.id)}
                          className={`focus-ring flex min-h-14 items-center justify-between gap-4 rounded-lg px-3 py-2.5 transition-colors ${
                            pathname === item.href
                              ? "bg-surface-subtle text-brand-700"
                              : "text-ink hover:bg-surface-subtle hover:text-brand-700"
                          }`}
                        >
                          <span className="min-w-0 truncate font-semibold">{item.label}</span>
                          <span className="shrink-0 text-xs tabular-nums text-ink-muted">{item.productCount} ürün</span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                </section>
              </div>
            ) : null}
          </div>
        );
      })}
    </nav>
  );
}

// Burada doğrudan hedefleri açılır menü tetikleyicileriyle aynı görsel ve odak düzeninde tutuyorum.
function PrimaryNavigationLink({ href, label, current }: { href: string; label: string; current: boolean }) {
  return (
    <Link
      href={href}
      prefetch={false}
      aria-current={current ? "page" : undefined}
      className={`focus-ring inline-flex min-h-11 items-center px-3 transition-colors ${
        current ? "text-brand-700" : "hover:text-brand-700"
      }`}
    >
      {label}
    </Link>
  );
}

// Burada açılma durumunu metin dışı, yardımcı teknolojiden gizli ve düşük maliyetli bir SVG ile gösteriyorum.
function ChevronIcon({ open }: { open: boolean }) {
  return (
    <svg aria-hidden="true" viewBox="0 0 20 20" className={`size-4 transition-transform ${open ? "rotate-180" : ""}`} fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round">
      <path d="m6 8 4 4 4-4" />
    </svg>
  );
}
