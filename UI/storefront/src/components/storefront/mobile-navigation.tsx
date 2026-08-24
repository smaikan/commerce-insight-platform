"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";

import type { StorefrontNavigationGroup } from "@/components/storefront/navigation-types";
import { MobileAuthLinks } from "@/modules/auth/components/header-auth-navigation";

type MobileNavigationProps = {
  siteName: string;
  groups: StorefrontNavigationGroup[];
};

// Burada mobil navigasyon durumunu küçük bir istemci bileşeninde tutup sayfa kabuğunu sunucu bileşeni olarak koruyorum.
export function MobileNavigation({ siteName, groups }: MobileNavigationProps) {
  const [isOpen, setIsOpen] = useState(false);
  const [openGroup, setOpenGroup] = useState<string | null>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;

    if (isOpen && !dialog.open) dialog.showModal();
    if (!isOpen && dialog.open) dialog.close();
  }, [isOpen]);

  useEffect(() => {
    const desktopMedia = window.matchMedia("(min-width: 64rem)");

    function closeAtDesktop(event: MediaQueryListEvent) {
      if (event.matches) setIsOpen(false);
    }

    desktopMedia.addEventListener("change", closeAtDesktop);
    return () => desktopMedia.removeEventListener("change", closeAtDesktop);
  }, []);

  function closeMenu() {
    setIsOpen(false);
  }

  function handleDialogClose() {
    setIsOpen(false);
    triggerRef.current?.focus();
  }

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        className="focus-ring inline-flex size-11 items-center justify-center text-ink lg:hidden"
        aria-expanded={isOpen}
        aria-controls="mobile-navigation-panel"
        aria-label={isOpen ? "Menüyü kapat" : "Menüyü aç"}
        onClick={() => setIsOpen((current) => !current)}
      >
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-6" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
          {isOpen ? (
            <path d="M6 6l12 12M18 6 6 18" />
          ) : (
            <path d="M4 7h16M4 12h16M4 17h16" />
          )}
        </svg>
      </button>

      <dialog
        ref={dialogRef}
        id="mobile-navigation-panel"
        className="mobile-navigation-dialog fixed inset-y-0 left-0 m-0 h-dvh max-h-none w-[min(86vw,22rem)] max-w-none overflow-y-auto border-0 bg-surface p-0 text-ink shadow-panel lg:hidden"
        aria-label="Navigasyon menüsü"
        onClose={handleDialogClose}
        onClick={(event) => {
          if (event.target === event.currentTarget) closeMenu();
        }}
      >
        <div className="flex min-h-18 items-center justify-between gap-4 border-b border-line px-4">
          {/* Burada mobil çekmece başlığını menü hiyerarşisini bastırmayacak ölçüde kompakt tutuyorum. */}
          <Link
            href="/"
            prefetch={false}
            className="focus-ring truncate text-sm font-black tracking-[0.14em] text-brand-950"
            onClick={closeMenu}
          >
            {siteName}
          </Link>
          <button type="button" className="focus-ring inline-flex size-11 shrink-0 items-center justify-center" aria-label="Menüyü kapat" onClick={closeMenu}>
            <svg aria-hidden="true" viewBox="0 0 24 24" className="size-6" fill="none" stroke="currentColor" strokeWidth="1.75" strokeLinecap="round">
              <path d="M6 6l12 12M18 6 6 18" />
            </svg>
          </button>
        </div>

        <nav className="px-4 py-4" aria-label="Mobil navigasyon">
          <Link className="mobile-nav-link" href="/" prefetch={false} onClick={closeMenu}>Ana sayfa</Link>
          <Link className="mobile-nav-link" href="/products" prefetch={false} onClick={closeMenu}>Katalog</Link>

          {groups.filter((group) => group.items.length > 0).map((group) => {
            const isGroupOpen = openGroup === group.id;
            const panelId = `mobile-navigation-${group.id}`;

            return (
              <div key={group.id} className="border-t border-line first:mt-2">
                <div className="flex min-h-12 items-center">
                  {group.href ? (
                    <Link href={group.href} prefetch={false} onClick={closeMenu} className="focus-ring flex min-h-12 min-w-0 flex-1 items-center rounded-md px-3 text-[0.9375rem] font-semibold text-ink hover:bg-surface-subtle hover:text-brand-700">
                      {group.label}
                    </Link>
                  ) : (
                    <button type="button" className="focus-ring flex min-h-12 min-w-0 flex-1 items-center rounded-md px-3 text-left text-[0.9375rem] font-semibold text-ink hover:bg-surface-subtle" aria-expanded={isGroupOpen} aria-controls={panelId} onClick={() => setOpenGroup(isGroupOpen ? null : group.id)}>
                      {group.label}
                    </button>
                  )}
                  <span className="ml-auto text-xs font-medium tabular-nums text-ink-muted">{group.items.length}</span>
                  <button type="button" className="focus-ring ml-1 inline-flex size-11 shrink-0 items-center justify-center rounded-md" aria-label={`${group.label} alt menüsünü ${isGroupOpen ? "kapat" : "aç"}`} aria-expanded={isGroupOpen} aria-controls={panelId} onClick={() => setOpenGroup(isGroupOpen ? null : group.id)}>
                    <svg aria-hidden="true" viewBox="0 0 20 20" className={`size-4 transition-transform ${isGroupOpen ? "rotate-180" : ""}`} fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round">
                      <path d="m6 8 4 4 4-4" />
                    </svg>
                  </button>
                </div>
                <ul id={panelId} hidden={!isGroupOpen} className="mb-2 border-l border-line pl-3">
                {group.items.map((item) => (
                  <li key={item.id}>
                    <Link
                      href={item.href}
                      prefetch={false}
                      onClick={closeMenu}
                      className="focus-ring flex min-h-12 items-center justify-between gap-3 rounded-md px-3 text-sm text-ink hover:bg-surface-subtle hover:text-brand-700"
                    >
                      <span className="min-w-0 truncate font-semibold">{item.label}</span>
                      <span className="shrink-0 text-xs tabular-nums text-ink-muted">{item.productCount}</span>
                    </Link>
                  </li>
                ))}
                </ul>
              </div>
            );
          })}

          <Link className="mobile-nav-link" href="/cart" prefetch={false} onClick={closeMenu}>Sepet</Link>
          {/* Burada doğrulanan müşteri auth hedeflerini mobil çekmecede ayrı ve kolay taranır bir hesap bölümünde sunuyorum. */}
          <div className="mt-2 border-t border-line pt-2">
            <p className="px-3 pt-2 pb-1 text-[0.6875rem] font-bold tracking-[0.14em] text-ink-muted uppercase">Hesap</p>
            <MobileAuthLinks onNavigate={closeMenu} />
          </div>
        </nav>
      </dialog>
    </>
  );
}
