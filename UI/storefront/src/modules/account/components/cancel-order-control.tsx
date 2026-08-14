"use client";

import { useState, useTransition } from "react";

import { cancelOrderAction } from "@/modules/account/actions";
import { ActionFeedback } from "@/modules/account/components/action-feedback";
import { INITIAL_ACCOUNT_ACTION_STATE, type AccountActionState } from "@/modules/account/contracts";

// Burada geri alınamaz sipariş iptalini ikinci bir açık onay adımı ve kalıcı sonuç mesajıyla çalıştırıyorum.
export function CancelOrderControl({ orderId }: { orderId: string }) {
  const [confirming, setConfirming] = useState(false);
  const [state, setState] = useState<AccountActionState>(INITIAL_ACCOUNT_ACTION_STATE);
  const [pending, startTransition] = useTransition();

  return (
    <div>
      {!confirming ? (
        <button type="button" onClick={() => setConfirming(true)} className="focus-ring min-h-11 border border-danger/40 px-4 text-sm font-bold text-danger hover:bg-danger/5">Siparişi iptal et</button>
      ) : (
        <div className="border border-danger/25 bg-danger/5 p-4">
          <p className="text-sm font-bold text-ink">Siparişi iptal etmek istediğinizden emin misiniz?</p>
          <p className="mt-1 text-xs leading-5 text-ink-muted">İşlem yalnız ödeme öncesindeki uygun siparişlerde tamamlanır.</p>
          <div className="mt-3 flex flex-wrap gap-2">
            <button type="button" disabled={pending} onClick={() => startTransition(() => void cancelOrderAction(orderId).then(setState))} className="focus-ring min-h-10 bg-danger px-4 text-xs font-bold text-white disabled:cursor-wait">{pending ? "İptal ediliyor…" : "Evet, siparişi iptal et"}</button>
            <button type="button" disabled={pending} onClick={() => setConfirming(false)} className="focus-ring min-h-10 border border-line bg-surface px-4 text-xs font-bold text-ink">Vazgeç</button>
          </div>
        </div>
      )}
      <ActionFeedback state={state} />
    </div>
  );
}
