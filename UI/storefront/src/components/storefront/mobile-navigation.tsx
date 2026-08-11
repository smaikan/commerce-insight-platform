"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";

type MobileNavigationProps = {
  currency: string;
  siteName: string;
};

// Burada mobil navigasyon durumunu küçük bir istemci bileşeninde tutup sayfa kabuğunu sunucu bileşeni olarak koruyorum.
export function MobileNavigation({ currency, siteName }: MobileNavigationProps) {
  const [isOpen, setIsOpen] = useState(false);
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
          <Link
            href="/"
            className="focus-ring truncate text-base font-black tracking-[0.14em] text-brand-950"
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
          <Link className="mobile-nav-link" href="/" onClick={closeMenu}>Ana sayfa</Link>
          <Link className="mobile-nav-link" href="/products" onClick={closeMenu}>Ürünler</Link>
          <Link className="mobile-nav-link" href="/cart" onClick={closeMenu}>Sepet</Link>
          <p className="mt-2 border-t border-line px-3 pt-4 pb-2 text-xs font-semibold tracking-wide text-ink-muted">
            TR · {currency}
          </p>
        </nav>
      </dialog>
    </>
  );
}
