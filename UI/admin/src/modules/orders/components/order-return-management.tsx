"use client";

import { useActionState, useEffect, useRef, useState, type RefObject } from "react";
import { useRouter } from "next/navigation";
import { ConfirmDialog } from "@/lib/admin/components/confirm-dialog";
import type { AdminMutationResult } from "@/lib/admin/mutation-result";
import { manageReturnRequestAction } from "@/modules/orders/actions";
import {
  returnStatusClass,
  returnStatusLabel,
  returnTypeLabel,
} from "@/modules/orders/return-presentation";
import { formatOrderAmount, formatOrderDate } from "@/modules/orders/presentation";
import type { ReturnRequest } from "@/modules/orders/types";

type ReturnOrderItem = { id: string; quantity: number };

// Burada siparişe bağlı iade taleplerini ürün ve adetleriyle gösterip yalnız geçerli talep düzeyi aksiyonlarını sunuyorum.
export function OrderReturnManagement({
  orderId,
  orderItems,
  returns,
  unavailable = false,
}: {
  orderId: string;
  orderItems: ReturnOrderItem[];
  returns: ReturnRequest[];
  unavailable?: boolean;
}) {
  return (
    <section aria-labelledby="order-returns-title" className="overflow-hidden rounded-xl border border-border bg-surface-strong">
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border px-4 py-4 sm:px-5">
        <div>
          <h2 id="order-returns-title" className="text-base font-semibold text-foreground">İade ve değişim talepleri</h2>
          <p className="mt-1 text-sm text-muted">Talep edilen ürün, adet ve yaşam döngüsü işlemleri</p>
        </div>
        <span className="rounded-md border border-border bg-surface-subtle px-2 py-1 text-xs font-semibold text-muted">{returns.length} talep</span>
      </div>

      {unavailable ? (
        <div className="m-4 rounded-lg border border-red-200 bg-red-50 px-3 py-3 text-sm text-red-900" role="alert">
          <p className="font-semibold">İade talepleri yüklenemedi</p>
          <p className="mt-1 leading-6">Sipariş bilgileri kullanılabilir durumda; iade kararı vermeden önce sayfayı yenileyip talepleri tekrar yükleyin.</p>
        </div>
      ) : returns.length > 0 ? (
        <div className="divide-y divide-border">
          {returns.map((returnRequest) => (
            <ReturnRequestCard key={`${returnRequest.id}-${returnRequest.status}`} orderId={orderId} orderItems={orderItems} returnRequest={returnRequest} />
          ))}
        </div>
      ) : (
        <p className="px-5 py-8 text-sm leading-6 text-muted">Bu sipariş için iade veya değişim talebi bulunmuyor.</p>
      )}
    </section>
  );
}

// Burada tek iade talebinin ürün kapsamını, notlarını ve sıradaki geçerli operasyonu aynı yüzeyde tutuyorum.
function ReturnRequestCard({ orderId, orderItems, returnRequest }: { orderId: string; orderItems: ReturnOrderItem[]; returnRequest: ReturnRequest }) {
  const [state, formAction, pending] = useActionState<AdminMutationResult | null, FormData>(manageReturnRequestAction, null);
  const [pendingIntent, setPendingIntent] = useState<"approve" | "reject" | "receive" | "complete" | null>(null);
  const formRef = useRef<HTMLFormElement>(null);
  const router = useRouter();

  // Burada başarılı iade işleminden sonra sipariş durumu ve talep ayrıntısını backend'den yeniden okuyorum.
  useEffect(() => {
    if (state?.status === "success" || state?.refresh) router.refresh();
  }, [router, state]);

  return (
    <article className="p-4 sm:p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="font-semibold text-foreground">{returnRequest.returnNumber}</h3>
            <span className={`rounded-md border px-2 py-0.5 text-xs font-semibold ${returnStatusClass(returnRequest.status)}`}>{returnStatusLabel(returnRequest.status)}</span>
            <span className="text-xs font-semibold text-muted">{returnTypeLabel(returnRequest.type)}</span>
          </div>
          <p className="mt-1 text-xs text-muted">Talep tarihi: {formatOrderDate(returnRequest.createdAt)}</p>
        </div>
        <p className="text-right text-sm text-muted">İade tutarı <span className="ml-1 font-bold tabular-nums text-foreground">{formatOrderAmount(returnRequest.refundTotal)}</span></p>
      </div>

      <ul className="mt-4 divide-y divide-border rounded-lg border border-border">
        {returnRequest.items.map((item) => {
          const orderedQuantity = orderItems.find((orderItem) => orderItem.id === item.orderItemId)?.quantity;
          return (
            <li key={item.id} className="grid gap-2 px-3 py-3 sm:grid-cols-[minmax(0,1fr)_auto] sm:items-center">
              <div className="min-w-0">
                <p className="font-semibold text-foreground">{item.productTitle}</p>
                <p className="mt-1 font-mono text-xs text-muted">SKU: {item.variantSku}</p>
                {item.replacementProductVariantId ? <p className="mt-1 break-all text-xs text-muted">Yeni varyant: {item.replacementProductVariantId}</p> : null}
              </div>
              <div className="sm:text-right">
                <p className="font-bold tabular-nums text-foreground">{item.quantity} adet talep</p>
                {orderedQuantity !== undefined ? <p className="mt-1 text-xs text-muted">Siparişte {orderedQuantity} adet</p> : null}
              </div>
            </li>
          );
        })}
      </ul>

      {returnRequest.customerNote ? (
        <div className="mt-3 rounded-lg border border-border bg-surface-subtle px-3 py-3 text-sm">
          <p className="text-xs font-semibold text-muted">Müşteri notu</p>
          <p className="mt-1 whitespace-pre-wrap leading-6 text-foreground">{returnRequest.customerNote}</p>
        </div>
      ) : null}
      {returnRequest.decisionNote ? (
        <div className="mt-3 rounded-lg border border-border px-3 py-3 text-sm">
          <p className="text-xs font-semibold text-muted">Karar notu</p>
          <p className="mt-1 whitespace-pre-wrap leading-6 text-foreground">{returnRequest.decisionNote}</p>
        </div>
      ) : null}

      <ReturnRequestActions
        orderId={orderId}
        returnRequest={returnRequest}
        state={state}
        pending={pending}
        pendingIntent={pendingIntent}
        setPendingIntent={setPendingIntent}
        formRef={formRef}
        formAction={formAction}
      />
    </article>
  );
}

// Burada iade durumuna göre yalnız Requested, Approved ve Received aşamalarının sıradaki geçerli aksiyonlarını gösteriyorum.
function ReturnRequestActions({
  orderId,
  returnRequest,
  state,
  pending,
  pendingIntent,
  setPendingIntent,
  formRef,
  formAction,
}: {
  orderId: string;
  returnRequest: ReturnRequest;
  state: AdminMutationResult | null;
  pending: boolean;
  pendingIntent: "approve" | "reject" | "receive" | "complete" | null;
  setPendingIntent: (intent: "approve" | "reject" | "receive" | "complete" | null) => void;
  formRef: RefObject<HTMLFormElement | null>;
  formAction: (formData: FormData) => void;
}) {
  const hasAction = returnRequest.status === 0 || returnRequest.status === 1 || returnRequest.status === 3;
  if (!hasAction && !state) return null;

  return (
    <form ref={formRef} action={formAction} onSubmit={(event) => { if (!pendingIntent) event.preventDefault(); }} className="mt-4 border-t border-border pt-4">
      <input type="hidden" name="orderId" value={orderId} />
      <input type="hidden" name="returnRequestId" value={returnRequest.id} />
      <input type="hidden" name="intent" value={pendingIntent ?? ""} />

      {returnRequest.status === 0 ? (
        <div>
          <label className="block text-xs font-semibold text-muted">
            Karar notu <span className="font-normal">(isteğe bağlı)</span>
            <textarea name="decisionNote" maxLength={1000} rows={3} className="mt-1.5 w-full resize-y rounded-lg border border-border-strong bg-surface-strong px-3 py-2 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus" />
          </label>
          <p className="mt-2 text-xs leading-5 text-muted">Onay veya ret bu talepte listelenen tüm ürün ve adetlere birlikte uygulanır.</p>
        </div>
      ) : null}

      {state ? (
        <div className={`mt-3 rounded-lg border px-3 py-2 text-sm ${state.status === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-900" : "border-red-200 bg-red-50 text-red-900"}`} role={state.status === "error" ? "alert" : "status"}>
          <p className="font-semibold">{state.message}</p>
          {state.traceId ? <p className="mt-1 font-mono text-xs">Takip kodu: {state.traceId}</p> : null}
        </div>
      ) : null}

      <div className="mt-3 flex flex-col gap-2 sm:flex-row sm:justify-end">
        {returnRequest.status === 0 ? (
          <>
            <button type="button" onClick={() => setPendingIntent("reject")} disabled={pending} className="inline-flex min-h-10 items-center justify-center rounded-lg border border-red-300 bg-surface-strong px-4 text-sm font-semibold text-red-700 hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-60">Talebi reddet</button>
            <button type="button" onClick={() => setPendingIntent("approve")} disabled={pending} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-60">Talebi onayla</button>
          </>
        ) : null}
        {returnRequest.status === 1 ? <button type="button" onClick={() => setPendingIntent("receive")} disabled={pending} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-60">Ürünleri teslim aldım</button> : null}
        {returnRequest.status === 3 ? <button type="button" onClick={() => setPendingIntent("complete")} disabled={pending} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-60">İade sürecini tamamla</button> : null}
      </div>

      <ConfirmDialog
        open={pendingIntent !== null}
        title={returnConfirmationTitle(returnRequest, pendingIntent)}
        description={returnConfirmationDescription(returnRequest, pendingIntent)}
        confirmLabel={returnConfirmationLabel(pendingIntent)}
        confirmTone={pendingIntent === "reject" ? "danger" : "primary"}
        pendingLabel="İşleniyor…"
        pending={pending}
        onCancel={() => setPendingIntent(null)}
        onConfirm={() => {
          setPendingIntent(null);
          formRef.current?.requestSubmit();
        }}
      />
    </form>
  );
}

// Burada iade karar penceresinin başlığında talep numarası ve seçilen işlemi açıkça belirtiyorum.
function returnConfirmationTitle(returnRequest: ReturnRequest, intent: "approve" | "reject" | "receive" | "complete" | null): string {
  const action = intent === "approve" ? "onaylansın" : intent === "reject" ? "reddedilsin" : intent === "receive" ? "teslim alınsın" : "tamamlansın";
  return `${returnRequest.returnNumber} ${action} mı?`;
}

// Burada kararın talepteki bütün ürün ve adetlere uygulandığını onay penceresinde yeniden açıklıyorum.
function returnConfirmationDescription(returnRequest: ReturnRequest, intent: "approve" | "reject" | "receive" | "complete" | null): string {
  const quantity = returnRequest.items.reduce((total, item) => total + item.quantity, 0);
  const effect = intent === "reject"
    ? "Talep reddedilecek ve aktif başka talep yoksa sipariş teslim edildi durumuna dönecek."
    : intent === "receive"
      ? "Ürünlerin fiziksel olarak geldiği kaydedilecek; stok etkisini backend uygulayacak."
      : intent === "complete"
        ? "İade veya değişim süreci kapatılacak; stok etkilerini backend uygulayacak. Bu işlem ödeme sağlayıcısında para iadesi veya Payment güncellemesi oluşturmaz."
        : returnRequest.type === 0
          ? "Talep onaylanacak ve sipariş Ücret İade Edildi durumuna taşınacak. Bu iş durumu ödeme sağlayıcısında para iadesi veya Payment güncellemesi oluşturmaz."
          : "Talep onaylanacak ve sipariş İade Talebi Onaylandı durumuna taşınacak.";
  return `${returnRequest.items.length} kalemde toplam ${quantity} adet için işlem yapılacak. ${effect}`;
}

// Burada seçilen iade yaşam döngüsü işlemini kısa ve eylem odaklı onay etiketine çeviriyorum.
function returnConfirmationLabel(intent: "approve" | "reject" | "receive" | "complete" | null): string {
  return intent === "approve" ? "Talebi onayla" : intent === "reject" ? "Talebi reddet" : intent === "receive" ? "Teslim alındı olarak işaretle" : "Süreci tamamla";
}
