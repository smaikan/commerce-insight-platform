"use client";

import { useEffect, useRef } from "react";
import { AdminNavigation } from "@/modules/admin-shell/components/admin-navigation";

export function MobileNavigation({ siteName }: { siteName: string }) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) {
      return;
    }

    const restoreFocus = () => triggerRef.current?.focus();
    dialog.addEventListener("close", restoreFocus);
    return () => dialog.removeEventListener("close", restoreFocus);
  }, []);

  const openNavigation = () => dialogRef.current?.showModal();
  const closeNavigation = () => dialogRef.current?.close();

  return (
    <>
      <button
        ref={triggerRef}
        type="button"
        onClick={openNavigation}
        className="inline-flex size-11 items-center justify-center rounded-lg border border-border-strong bg-surface-strong text-foreground hover:bg-surface-subtle lg:hidden"
        aria-label="Ana menüyü aç"
      >
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 fill-none stroke-current stroke-2">
          <path d="M4 7h16M4 12h16M4 17h16" strokeLinecap="round" />
        </svg>
      </button>

      <dialog
        ref={dialogRef}
        aria-label="Ana menü"
        className="m-0 h-dvh max-h-none w-[min(88vw,20rem)] max-w-none border-0 bg-sidebar p-0 text-sidebar-foreground shadow-xl backdrop:bg-foreground/45 open:flex open:flex-col"
      >
        <div className="flex h-16 shrink-0 items-center justify-between border-b border-sidebar-border px-4">
          <div className="min-w-0">
            {/* Burada mobil menüde de masaüstüyle aynı mağaza adı kimliğini koruyorum. */}
            <span className="block truncate text-base font-semibold tracking-tight">{siteName}</span>
            <span className="mt-0.5 block text-xs text-sidebar-muted">Yönetim Paneli</span>
          </div>
          <button
            type="button"
            onClick={closeNavigation}
            className="inline-flex size-11 items-center justify-center rounded-lg text-sidebar-muted hover:bg-sidebar-hover hover:text-sidebar-foreground"
            aria-label="Ana menüyü kapat"
          >
            <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 fill-none stroke-current stroke-2">
              <path d="m6 6 12 12M18 6 6 18" strokeLinecap="round" />
            </svg>
          </button>
        </div>
        <div className="flex-1 overflow-y-auto py-3">
          <AdminNavigation onNavigate={closeNavigation} />
        </div>
      </dialog>
    </>
  );
}
