"use client";

import { useEffect, useRef } from "react";

import { IyzicoPaymentControl } from "@/modules/checkout/components/iyzico-payment-control";
import { OrderCancellationControl } from "@/modules/checkout/components/order-cancellation-control";
import { SandboxPaymentNotice } from "@/modules/checkout/components/sandbox-payment-notice";

// Burada iyzico sayfasından ödemeden dönen müşteriyi ikinci sipariş oluşturmadan mevcut ödeme kararıyla buluşturuyorum.
export function ActivePaymentRecoveryDialog({
  orderId,
  orderNumber,
  orderStatus,
  accessMode,
  sandboxCardNumber,
  onCancelled,
}: {
  orderId: string;
  orderNumber: string;
  orderStatus: number;
  accessMode: "member" | "guest";
  sandboxCardNumber: string | null;
  onCancelled: () => void;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  // Burada modalı gerçek top-layer dialog olarak açıp arka plandaki checkout kontrollerini etkileşime kapatıyorum.
  useEffect(() => {
    const dialog = dialogRef.current;
    if (!dialog) return;
    if (!dialog.open) dialog.showModal();

    return () => {
      if (dialog.open) dialog.close();
    };
  }, []);

  return (
    <dialog
      ref={dialogRef}
      className="payment-recovery-dialog fixed inset-0 m-auto max-h-[calc(100dvh-2rem)] w-[calc(100%-2rem)] max-w-lg overflow-y-auto rounded-2xl border border-line bg-surface p-0 text-ink shadow-panel"
      aria-labelledby="payment-recovery-title"
      aria-describedby="payment-recovery-description payment-recovery-safety"
      onCancel={(event) => event.preventDefault()}
    >
      <div className="border-b border-line px-5 py-5 sm:px-7 sm:py-6">
        <span className="inline-flex size-10 items-center justify-center rounded-full bg-surface-subtle text-brand-700" aria-hidden="true">
          <svg viewBox="0 0 24 24" className="size-5" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <path d="M12 7v5l3 2" />
            <circle cx="12" cy="12" r="9" />
          </svg>
        </span>
        <p className="mt-4 text-xs font-bold tracking-[0.14em] text-brand-700 uppercase">Bekleyen ödeme</p>
        <h1 id="payment-recovery-title" className="mt-2 text-2xl font-semibold tracking-[-0.03em] text-ink sm:text-3xl">
          Ödemeniz henüz tamamlanmadı
        </h1>
        <p id="payment-recovery-description" className="mt-3 text-sm leading-6 text-ink-muted">
          Yeni bir sipariş oluşturmadan önce mevcut iyzico ödeme oturumuna devam edin veya siparişi iptal edin.
        </p>
      </div>

      <div className="space-y-5 px-5 py-5 sm:px-7 sm:py-6">
        <div className="rounded-xl border border-line bg-surface-subtle px-4 py-3">
          <p className="text-xs font-semibold text-ink-muted">Bekleyen sipariş</p>
          <p className="mt-1 break-all text-sm font-bold text-brand-950">{orderNumber}</p>
          <p className="mt-2 text-xs leading-5 text-ink-muted">Bu siparişteki ürünler ödeme sonucu kesinleşene kadar sizin için rezerve edilir.</p>
        </div>

        {sandboxCardNumber ? <SandboxPaymentNotice cardNumber={sandboxCardNumber} /> : null}

        <div className="grid gap-3 sm:grid-cols-2">
          <IyzicoPaymentControl orderId={orderId} />
          <OrderCancellationControl
            orderId={orderId}
            orderStatus={orderStatus}
            accessMode={accessMode}
            label="Siparişi iptal et"
            onCancelled={onCancelled}
          />
        </div>

        <p id="payment-recovery-safety" className="text-xs leading-5 text-ink-muted">
          İptal öncesinde iyzico son kez kontrol edilir. Tahsilat yoksa sipariş hemen iptal edilir ve ayrılan stok yeniden kullanılabilir olur.
        </p>
      </div>
    </dialog>
  );
}
