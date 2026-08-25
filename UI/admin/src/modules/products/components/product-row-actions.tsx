"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { ConfirmDialog } from "@/lib/admin/components/confirm-dialog";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { deleteProductAction, setProductListStatusAction } from "@/modules/products/actions";
import type { ProductStatus } from "@/modules/products/types";
import { getQuickProductStatus } from "@/modules/products/row-actions";

type MenuPosition = { top: number; right: number };

// Burada ürün satırındaki düzenleme, hızlı durum ve güvenli silme işlemlerini tek taşmayan menüde topluyorum.
export function ProductRowActions({ id, title, status }: { id: string; title: string; status: ProductStatus }) {
  const router = useRouter();
  const triggerRef = useRef<HTMLButtonElement>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const [open, setOpen] = useState(false);
  const [position, setPosition] = useState<MenuPosition>({ top: 0, right: 0 });
  const [confirmDelete, setConfirmDelete] = useState(false);
  const [pending, setPending] = useState(false);
  const [result, setResult] = useState<AdminMutationResult>();
  const quickStatus = getQuickProductStatus(status);

  // Burada menüyü tablo taşmasından etkilenmeyecek sabit koordinata yerleştiriyorum.
  const toggleMenu = () => {
    if (!open && triggerRef.current) {
      const rect = triggerRef.current.getBoundingClientRect();
      setPosition({ top: rect.bottom + 6, right: Math.max(8, window.innerWidth - rect.right) });
    }
    setOpen((current) => !current);
    setResult(undefined);
  };

  // Burada dış tıklama, Escape ve kaydırmada açık menüyü kapatıyorum.
  useEffect(() => {
    if (!open) return;
    const closeOutside = (event: MouseEvent) => {
      if (!menuRef.current?.contains(event.target as Node) && !triggerRef.current?.contains(event.target as Node)) setOpen(false);
    };
    const closeWithEscape = (event: KeyboardEvent) => { if (event.key === "Escape") setOpen(false); };
    const closeOnScroll = () => setOpen(false);
    document.addEventListener("mousedown", closeOutside);
    window.addEventListener("keydown", closeWithEscape);
    window.addEventListener("scroll", closeOnScroll, true);
    return () => {
      document.removeEventListener("mousedown", closeOutside);
      window.removeEventListener("keydown", closeWithEscape);
      window.removeEventListener("scroll", closeOnScroll, true);
    };
  }, [open]);

  // Burada ürün durumunu yalnız taslak ve aktif arasında değiştirip listeyi tazeliyorum.
  const changeStatus = async () => {
    if (quickStatus === null || pending) return;
    setPending(true);
    const response = await setProductListStatusAction(id, quickStatus);
    setPending(false);
    setResult(response);
    setOpen(false);
    if (response.status === "success") router.refresh();
  };

  // Burada onaylanan ürün silme sonucunu yönlendirme veya yerinde hata mesajıyla tamamlıyorum.
  const deleteProduct = async () => {
    if (pending) return;
    setPending(true);
    const response = await deleteProductAction(id);
    setPending(false);
    setResult(response);
    if (response.status === "success") {
      setConfirmDelete(false);
      router.push(response.redirectHref || "/products");
    }
  };

  return (
    <div className="relative inline-flex">
      <button ref={triggerRef} type="button" onClick={toggleMenu} aria-haspopup="menu" aria-expanded={open} aria-label={`${title} için işlemler`} className="inline-flex size-8 cursor-pointer items-center justify-center rounded-lg border border-transparent text-muted transition-colors hover:border-border hover:bg-surface-strong hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">
        <svg viewBox="0 0 20 20" aria-hidden="true" className="size-5 fill-current"><circle cx="4" cy="10" r="1.5" /><circle cx="10" cy="10" r="1.5" /><circle cx="16" cy="10" r="1.5" /></svg>
      </button>
      {open ? (
        <div ref={menuRef} role="menu" style={{ top: position.top, right: position.right }} className="fixed z-[70] w-52 overflow-hidden rounded-lg border border-border bg-surface-strong p-1.5 text-left shadow-xl">
          <Link href={`/products/${encodeURIComponent(id)}`} role="menuitem" className="flex min-h-9 cursor-pointer items-center rounded-md px-3 text-sm font-semibold text-foreground transition-colors hover:bg-surface-subtle">Ürünü düzenle</Link>
          {quickStatus !== null ? <button type="button" role="menuitem" disabled={pending} onClick={changeStatus} className="flex min-h-9 w-full cursor-pointer items-center rounded-md px-3 text-sm font-semibold text-foreground transition-colors hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60">{quickStatus === 1 ? "Ürünü aktifleştir" : "Taslak olarak kaydet"}</button> : null}
          <div className="my-1 border-t border-border" />
          <button type="button" role="menuitem" onClick={() => { setOpen(false); setConfirmDelete(true); }} className="flex min-h-9 w-full cursor-pointer items-center rounded-md px-3 text-sm font-semibold text-danger transition-colors hover:bg-danger/10">Ürünü sil</button>
        </div>
      ) : null}
      {result?.status === "error" && !confirmDelete ? <div role="alert" className="fixed bottom-5 right-5 z-[90] max-w-sm rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900 shadow-xl"><p className="font-semibold">{result.message}</p>{result.traceId ? <p className="mt-1 text-xs">Takip kodu: {result.traceId}</p> : null}<button type="button" onClick={() => setResult(undefined)} className="mt-2 text-xs font-bold underline">Kapat</button></div> : null}
      <ConfirmDialog open={confirmDelete} title="Ürün silinsin mi?" description={`“${title}” mağazadan kaldırılacak ve arşive taşınacak. Sipariş, stok ve satış geçmişi korunacak.`} confirmLabel="Ürünü sil" pending={pending} error={result?.status === "error" ? result.message : undefined} onCancel={() => { if (!pending) { setConfirmDelete(false); setResult(undefined); } }} onConfirm={deleteProduct} />
    </div>
  );
}
