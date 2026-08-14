"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";

import { AccountIcon } from "@/modules/account/components/account-icon";
import { ACCOUNT_DESTINATIONS } from "@/modules/account/navigation";
import { logoutAction } from "@/modules/auth/actions";
import { useHeaderSession } from "@/modules/auth/components/header-session";
import { clearFavoriteStateForOwnerChange } from "@/modules/favorites/client/favorites-api";

// Burada masaüstü navbarında guest aksiyonlarını veya giriş sonrası erişilebilir Hesabım menüsünü aynı ayrılmış alanda gösteriyorum.
export function DesktopAuthNavigation() {
  const session = useHeaderSession();
  const [open, setOpen] = useState(false);
  const wrapperRef = useRef<HTMLDivElement>(null);
  const triggerRef = useRef<HTMLButtonElement>(null);

  useEffect(() => {
    if (!open) return;

    function closeOutside(event: PointerEvent) {
      if (!wrapperRef.current?.contains(event.target as Node)) setOpen(false);
    }
    function closeWithEscape(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setOpen(false);
        triggerRef.current?.focus();
      }
    }

    document.addEventListener("pointerdown", closeOutside);
    document.addEventListener("keydown", closeWithEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOutside);
      document.removeEventListener("keydown", closeWithEscape);
    };
  }, [open]);

  if (session === "loading") {
    return <div aria-hidden="true" className="hidden h-10 w-44 border border-line bg-surface-subtle lg:block" />;
  }

  if (session === "guest") {
    return (
      <nav className="hidden items-center gap-1.5 lg:flex" aria-label="Hesap işlemleri">
        <Link href="/login" prefetch={false} className="focus-ring inline-flex min-h-10 shrink-0 items-center gap-2 whitespace-nowrap border border-line bg-surface px-2.5 text-xs font-bold text-ink transition-colors hover:border-brand-600 hover:bg-surface-subtle hover:text-brand-700 xl:px-3.5">
          <UserIcon />
          Giriş yap
        </Link>
        <Link href="/register" prefetch={false} className="focus-ring inline-flex min-h-10 shrink-0 items-center gap-2 whitespace-nowrap border border-brand-950 bg-brand-950 px-2.5 text-xs font-bold text-white transition-colors hover:border-brand-700 hover:bg-brand-700 xl:px-3.5">
          <UserPlusIcon />
          Hesap oluştur
        </Link>
      </nav>
    );
  }

  return (
    <div ref={wrapperRef} className="relative hidden lg:block">
      {/* Burada hesap tetikleyicisini koyu bir çağrı butonu yerine diğer navbar aksiyonlarıyla aynı sakin yüzeyde gösteriyorum. */}
      <button
        ref={triggerRef}
        type="button"
        className={`header-action inline-flex min-h-11 cursor-pointer items-center gap-2 px-2.5 text-[0.8125rem] font-semibold whitespace-nowrap hover:bg-surface-subtle hover:text-brand-700 ${open ? "bg-surface-subtle text-brand-700" : ""}`}
        aria-expanded={open}
        aria-controls="desktop-account-menu"
        onClick={() => setOpen((current) => !current)}
      >
        <UserIcon alwaysVisible />
        Hesabım
        <ChevronIcon open={open} />
      </button>

      {open ? (
        <div id="desktop-account-menu" className="absolute top-full right-0 z-50 w-60 pt-2">
          <section className="overflow-hidden rounded-lg border border-line bg-surface shadow-panel" aria-label="Hesabım menüsü">
            {/* Burada tekrarlanan panel başlığını kaldırıp hesap hedeflerini tek bakışta taranabilen kompakt satırlarda sunuyorum. */}
            <nav className="p-1.5" aria-label="Hesabım seçenekleri">
              {ACCOUNT_DESTINATIONS.map((item) => (
                <Link key={item.href} href={item.href} prefetch={false} onClick={() => setOpen(false)} className="focus-ring flex min-h-11 items-center gap-3 px-3 text-sm font-semibold text-ink transition-colors hover:bg-surface-subtle hover:text-brand-700">
                  <span className="flex size-7 shrink-0 items-center justify-center rounded-md bg-surface-subtle text-brand-700" aria-hidden="true">
                    <AccountIcon icon={item.icon} className="size-4" />
                  </span>
                  <span>{item.label}</span>
                </Link>
              ))}
            </nav>
            <form action={logoutAction} onSubmit={clearFavoriteStateForOwnerChange} className="border-t border-line p-1.5">
              {/* Burada çıkış eylemini menü hedefleriyle aynı ölçüde, ancak ayrı bölümde sakin bir metin aksiyonu olarak tutuyorum. */}
              <button type="submit" className="focus-ring flex min-h-11 w-full items-center gap-3 px-3 text-left text-sm font-semibold text-ink-muted transition-colors hover:bg-surface-subtle hover:text-brand-700">
                <LogoutIcon />
                Çıkış yap
              </button>
            </form>
          </section>
        </div>
      ) : null}
    </div>
  );
}

// Burada mobil drawer hesap bölümünü aynı oturum durumuna göre guest bağlantıları veya gerçek hesap hedefleriyle değiştiriyorum.
export function MobileAuthLinks({ onNavigate }: { onNavigate: () => void }) {
  const session = useHeaderSession();

  if (session === "loading") {
    return <p role="status" className="min-h-12 px-3 py-3 text-sm text-ink-muted">Hesap durumu yükleniyor…</p>;
  }

  if (session === "guest") {
    return (
      <>
        <Link className="mobile-nav-link" href="/login" prefetch={false} onClick={onNavigate}>Giriş yap</Link>
        <Link className="mobile-nav-link text-brand-700" href="/register" prefetch={false} onClick={onNavigate}>Hesap oluştur</Link>
      </>
    );
  }

  return (
    <>
      {ACCOUNT_DESTINATIONS.map((item) => (
        <Link key={item.href} className="mobile-nav-link gap-3" href={item.href} prefetch={false} onClick={onNavigate}>
          <AccountIcon icon={item.icon} className="size-4.5 shrink-0 text-brand-700" />
          {item.label}
        </Link>
      ))}
      <form action={logoutAction} onSubmit={clearFavoriteStateForOwnerChange}>
        <button type="submit" className="mobile-nav-link w-full gap-3 text-left">
          <LogoutIcon />
          Çıkış yap
        </button>
      </form>
    </>
  );
}

// Burada guest giriş ve authenticated hesap tetikleyicisinde ortak kullanıcı simgesini kullanıyorum.
function UserIcon({ alwaysVisible = false }: { alwaysVisible?: boolean }) {
  return (
    <svg aria-hidden="true" viewBox="0 0 20 20" className={`${alwaysVisible ? "block" : "hidden xl:block"} size-4`} fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="10" cy="6.5" r="3" />
      <path d="M4.5 16c.8-3 2.7-4.5 5.5-4.5s4.7 1.5 5.5 4.5" />
    </svg>
  );
}

// Burada hesap oluşturma aksiyonunu ortak kullanıcı simgesinden küçük artı işaretiyle ayırıyorum.
function UserPlusIcon() {
  return (
    <svg aria-hidden="true" viewBox="0 0 20 20" className="hidden size-4 xl:block" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="8" cy="6.5" r="3" />
      <path d="M2.8 16c.7-3 2.5-4.5 5.2-4.5 1.1 0 2 .2 2.8.7M15 10.5v5M12.5 13h5" />
    </svg>
  );
}

// Burada hesabım menüsünün açık veya kapalı olduğunu düşük maliyetli yön simgesiyle gösteriyorum.
function ChevronIcon({ open }: { open: boolean }) {
  return <svg aria-hidden="true" viewBox="0 0 20 20" className={`size-3.5 transition-transform ${open ? "rotate-180" : ""}`} fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round"><path d="m6 8 4 4 4-4" /></svg>;
}

// Burada logout aksiyonunu diğer hesap hedeflerinden anlaşılır biçimde ayıran çıkış simgesini çiziyorum.
function LogoutIcon() {
  return <svg aria-hidden="true" viewBox="0 0 20 20" className="size-4.5 shrink-0" fill="none" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"><path d="M8 4H4v12h4M12 6l4 4-4 4M6 10h10" /></svg>;
}
