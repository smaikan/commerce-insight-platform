"use client";

import { useEffect, useRef } from "react";

type ConfirmDialogProps = {
  open: boolean;
  title: string;
  description: string;
  confirmLabel: string;
  pending?: boolean;
  error?: string;
  onCancel: () => void;
  onConfirm: () => void;
};

// Burada geri alınamayan işlemi odak yönetimi ve Escape desteği olan erişilebilir pencerede onaylatıyorum.
export function ConfirmDialog({ open, title, description, confirmLabel, pending = false, error, onCancel, onConfirm }: ConfirmDialogProps) {
  const cancelRef = useRef<HTMLButtonElement>(null);
  const dialogRef = useRef<HTMLElement>(null);
  const previousFocusRef = useRef<HTMLElement | null>(null);
  const pendingRef = useRef(pending);
  const onCancelRef = useRef(onCancel);

  // Burada klavye dinleyicisinin en güncel bekleme ve iptal davranışını yeniden kurulmadan okumasını sağlıyorum.
  useEffect(() => {
    pendingRef.current = pending;
    onCancelRef.current = onCancel;
  }, [onCancel, pending]);

  // Burada pencere açıldığında odağı güvenli iptal seçeneğine taşıyor ve Escape ile kapatıyorum.
  useEffect(() => {
    if (!open) return;
    previousFocusRef.current = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    cancelRef.current?.focus();
    const closeWithEscape = (event: KeyboardEvent) => {
      if (event.key === "Escape" && !pendingRef.current) onCancelRef.current();
      if (event.key !== "Tab") return;
      const focusable = Array.from(dialogRef.current?.querySelectorAll<HTMLElement>("button:not(:disabled), [href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), [tabindex]:not([tabindex='-1'])") || []);
      if (focusable.length === 0) return;
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    };
    window.addEventListener("keydown", closeWithEscape);
    return () => {
      window.removeEventListener("keydown", closeWithEscape);
      document.body.style.overflow = previousOverflow;
      previousFocusRef.current?.focus();
    };
  }, [open]);

  if (!open) return null;
  return (
    <div className="fixed inset-0 z-[80] flex items-center justify-center bg-slate-950/45 p-4" role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !pending) onCancel(); }}>
      <section ref={dialogRef} role="alertdialog" aria-modal="true" aria-labelledby="confirm-title" aria-describedby="confirm-description" className="w-full max-w-md rounded-xl border border-border bg-surface-strong p-5 shadow-2xl">
        <h2 id="confirm-title" className="text-base font-bold text-foreground">{title}</h2>
        <p id="confirm-description" className="mt-2 text-sm leading-6 text-muted">{description}</p>
        {error ? <p className="mt-3 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm font-semibold text-red-900" role="alert">{error}</p> : null}
        <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <button ref={cancelRef} type="button" disabled={pending} onClick={onCancel} className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle disabled:opacity-60">Vazgeç</button>
          <button type="button" disabled={pending} onClick={onConfirm} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-danger px-4 text-sm font-semibold text-white hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Siliniyor…" : confirmLabel}</button>
        </div>
      </section>
    </div>
  );
}
