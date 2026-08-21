"use client";

import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { ConfirmDialog } from "@/lib/admin/components/confirm-dialog";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { updateOrderStatusAction } from "@/modules/orders/actions";
import { orderStatusTransitions } from "@/modules/orders/lifecycle";
import type { Order } from "@/modules/orders/types";

const initialState: AdminMutationResult | null = null;
type OrderStatusControlOrder = Pick<Order, "id" | "orderNumber" | "status" | "shippingCarrier" | "trackingNumber" | "trackingUrl">;

// Burada normal sipariş geçişlerini iade akışından ayırıp kargo ve iptal kararlarını kontrollü formda sunuyorum.
export function OrderStatusControl({ order }: { order: OrderStatusControlOrder }) {
  const transitions = orderStatusTransitions(order.status);
  const [state, formAction, pending] = useActionState(updateOrderStatusAction, initialState);
  const [targetStatus, setTargetStatus] = useState<number>(transitions[0]?.value ?? order.status);
  const [showCancelConfirmation, setShowCancelConfirmation] = useState(false);
  const formRef = useRef<HTMLFormElement>(null);
  const router = useRouter();
  const isShipping = targetStatus === 4;

  // Burada başarılı durum değişikliğinden sonra aynı detayın ve sunucu verisinin güncel halini yüklüyorum.
  useEffect(() => {
    if (state?.status === "success" || state?.refresh) router.refresh();
  }, [router, state]);

  if (transitions.length === 0) {
    const message = order.status === 7
      ? "Ücret iade edildi durumu iade akışı tarafından belirlenir. Bu iş durumu, ödeme sağlayıcısında para iadesi veya Payment güncellemesi yapıldığı anlamına gelmez."
      : order.status === 8 || order.status === 9
        ? "Bu siparişin durumu aşağıdaki iade taleplerinden yönetilir. Genel durum alanı iade akışını değiştiremez."
        : "Bu durum için genel sipariş akışında başka bir geçiş yok.";
    return (
      <p className="rounded-lg border border-border bg-surface-subtle px-3 py-3 text-sm leading-6 text-muted">
        {message}
      </p>
    );
  }

  // Burada iptal hedefinde formu göndermeden önce işlemin stok ve kupon etkisini kullanıcıya açıkça onaylatıyorum.
  function handleSubmit(event: React.FormEvent<HTMLFormElement>) {
    if (targetStatus === 6 && !showCancelConfirmation) {
      event.preventDefault();
      setShowCancelConfirmation(true);
    }
  }

  return (
    <form ref={formRef} action={formAction} onSubmit={handleSubmit} className="space-y-3">
      <input type="hidden" name="orderId" value={order.id} />
      <label className="block text-xs font-semibold text-muted">
        Yeni durum
        <select
          name="status"
          value={targetStatus}
          onChange={(event) => setTargetStatus(Number(event.target.value))}
          className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
        >
          {transitions.map((transition) => (
            <option key={`${transition.value}-${transition.label}`} value={transition.value}>{transition.label}</option>
          ))}
        </select>
      </label>
      <p className="text-xs leading-5 text-muted">
        {transitions.find((transition) => transition.value === targetStatus)?.description}
      </p>

      {isShipping ? (
        <fieldset className="space-y-3 border-t border-border pt-3">
          <legend className="text-sm font-semibold text-foreground">Kargo takibi</legend>
          <label className="block text-xs font-semibold text-muted">
            Taşıyıcı
            <input name="shippingCarrier" required maxLength={150} defaultValue={order.shippingCarrier ?? ""} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus" />
          </label>
          <label className="block text-xs font-semibold text-muted">
            Takip numarası
            <input name="trackingNumber" required maxLength={100} defaultValue={order.trackingNumber ?? ""} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus" />
          </label>
          <label className="block text-xs font-semibold text-muted">
            Takip bağlantısı <span className="font-normal">(isteğe bağlı)</span>
            <input name="trackingUrl" type="url" maxLength={500} defaultValue={order.trackingUrl ?? ""} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus" />
          </label>
        </fieldset>
      ) : null}

      {state ? (
        <div className={`rounded-lg border px-3 py-2 text-sm ${state.status === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-900" : "border-red-200 bg-red-50 text-red-900"}`} role={state.status === "error" ? "alert" : "status"}>
          <p className="font-semibold">{state.message}</p>
          {state.traceId ? <p className="mt-1 font-mono text-xs">Takip kodu: {state.traceId}</p> : null}
        </div>
      ) : null}

      <button type="submit" disabled={pending} className="inline-flex min-h-10 w-full items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-60">
        {pending ? "Güncelleniyor…" : "Sipariş durumunu güncelle"}
      </button>

      <ConfirmDialog
        open={showCancelConfirmation}
        title={`Sipariş ${order.orderNumber} iptal edilsin mi?`}
        description="Sipariş iptal edilir; stok ve kupon etkileri backend tarafından geri alınır. Bu işlem kendi başına ödeme iadesi oluşturmaz ve durum genel akıştan geri çevrilemez."
        confirmLabel="Siparişi iptal et"
        pending={pending}
        onCancel={() => setShowCancelConfirmation(false)}
        onConfirm={() => {
          setShowCancelConfirmation(false);
          formRef.current?.requestSubmit();
        }}
      />
    </form>
  );
}
