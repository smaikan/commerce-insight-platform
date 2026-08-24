"use client";

import { useCallback, useEffect, useRef, useState } from "react";

import {
  cancellationConfirmationMessage,
  cancellationOperationMessage,
  cancellationPollingDelay,
  cancellationProblemMessage,
  isCancellationPending,
  type CustomerOrder,
  type OrderCancellationAccessMode,
  type OrderCancellationOperation,
} from "@/modules/orders/cancellation";
import {
  cancelCustomerOrder,
  loadCustomerOrderCancellation,
  loadOrderAfterCancellation,
  OrderCancellationClientError,
} from "@/modules/orders/client/cancellation-api";

const MAX_AUTOMATIC_POLLING_MS = 5 * 60_000;

type CancellationViewState =
  | { kind: "idle" }
  | { kind: "confirming" }
  | { kind: "submitting" }
  | { kind: "pending"; operation: OrderCancellationOperation; elapsedMs: number }
  | { kind: "attention"; message: string; traceId?: string }
  | { kind: "completed" };

type CustomerOrderCancellationControlProps = {
  orderId: string;
  orderStatus: number;
  accessMode: OrderCancellationAccessMode;
  label?: string;
  appearance?: "checkout" | "account";
  onOrderUpdated?: (order: CustomerOrder) => void | Promise<void>;
};

// Burada member ve guest sipariş yüzeylerini aynı idempotent cancellation ve polling durum makinesinde birleştiriyorum.
export function CustomerOrderCancellationControl({
  orderId,
  orderStatus,
  accessMode,
  label = "Siparişi iptal et",
  appearance = "checkout",
  onOrderUpdated,
}: CustomerOrderCancellationControlProps) {
  const [state, setState] = useState<CancellationViewState>({ kind: "idle" });
  const requestSequenceRef = useRef(0);
  const mutationInFlightRef = useRef(false);
  const feedbackRef = useRef<HTMLDivElement>(null);
  const onOrderUpdatedRef = useRef(onOrderUpdated);

  // Burada async polling tamamlandığında en güncel parent callback'ini render sırasında ref değiştirmeden koruyorum.
  useEffect(() => {
    onOrderUpdatedRef.current = onOrderUpdated;
  }, [onOrderUpdated]);

  // Burada tamamlanan operasyonu yalnız güncel OrderDto gerçekten Cancelled olduğunda kullanıcıya başarı olarak açıklıyorum.
  const finishCancellation = useCallback(async (knownOrder?: CustomerOrder) => {
    const sequence = ++requestSequenceRef.current;
    try {
      const order = knownOrder ?? await loadOrderAfterCancellation(orderId, accessMode);
      if (sequence !== requestSequenceRef.current) return;
      if (order.status !== 6) {
        setState({ kind: "attention", message: "İptal işlemi tamamlandı olarak bildirildi ancak güncel sipariş durumu doğrulanamadı. Lütfen sayfayı yenileyin." });
        return;
      }

      setState({ kind: "completed" });
      await onOrderUpdatedRef.current?.(order);
    } catch (error) {
      if (sequence !== requestSequenceRef.current) return;
      setState(attentionState(error));
    }
  }, [accessMode, orderId]);

  // Burada sayfa yenilendiğinde daha önce başlatılmış operasyonu bulup kaldığı polling veya inceleme durumundan sürdürüyorum.
  useEffect(() => {
    const controller = new AbortController();
    const sequence = ++requestSequenceRef.current;
    void loadCustomerOrderCancellation(orderId, accessMode, controller.signal)
      .then((operation) => {
        if (controller.signal.aborted || sequence !== requestSequenceRef.current) return;
        if (isCancellationPending(operation.status)) {
          setState({ kind: "pending", operation, elapsedMs: 0 });
        } else if (operation.status === 3) {
          void finishCancellation();
        } else {
          setState({ kind: "attention", message: cancellationOperationMessage(operation) });
        }
      })
      .catch((error) => {
        if (controller.signal.aborted || sequence !== requestSequenceRef.current) return;
        if (error instanceof OrderCancellationClientError && error.problem.status === 404) return;
        setState(attentionState(error));
      });

    return () => controller.abort();
  }, [accessMode, finishCancellation, orderId]);

  // Burada pending operasyonu nextAttemptAt yönlendirmesiyle, görünür sekmede ve sınırlı süre boyunca sorguluyorum.
  useEffect(() => {
    if (state.kind !== "pending") return;
    if (state.elapsedMs >= MAX_AUTOMATIC_POLLING_MS) {
      const terminalTimer = window.setTimeout(() => setState({
        kind: "attention",
        message: "İptal kontrolü arka planda devam ediyor. Siparişiniz henüz değiştirilmedi; daha sonra sayfayı yenileyerek güncel durumu kontrol edin.",
      }), 0);
      return () => window.clearTimeout(terminalTimer);
    }

    const controller = new AbortController();
    const delay = cancellationPollingDelay(state.operation.nextAttemptAt ?? null);
    const timer = window.setTimeout(() => {
      if (document.visibilityState === "hidden") {
        setState({ ...state, operation: { ...state.operation, nextAttemptAt: null }, elapsedMs: state.elapsedMs + delay });
        return;
      }

      void loadCustomerOrderCancellation(orderId, accessMode, controller.signal)
        .then((operation) => {
          if (controller.signal.aborted) return;
          if (isCancellationPending(operation.status)) {
            setState({ kind: "pending", operation, elapsedMs: state.elapsedMs + delay });
          } else if (operation.status === 3) {
            void finishCancellation();
          } else {
            setState({ kind: "attention", message: cancellationOperationMessage(operation) });
          }
        })
        .catch((error) => {
          if (controller.signal.aborted) return;
          if (error instanceof OrderCancellationClientError && error.problem.status === 503) {
            setState({ ...state, operation: { ...state.operation, nextAttemptAt: null }, elapsedMs: state.elapsedMs + delay });
            return;
          }
          setState(attentionState(error));
        });
    }, delay);

    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [accessMode, finishCancellation, orderId, state]);

  // Burada onay, bekleme ve hata geçişlerinde klavye odağını kalıcı geri bildirim bölgesine taşıyorum.
  useEffect(() => {
    if (state.kind !== "idle") feedbackRef.current?.focus();
  }, [state.kind]);

  // Burada tek müşteri niyetini bir kez POST edip 200 ve 202 sonuçlarını birbirine karıştırmadan işliyorum.
  async function submitCancellation() {
    if (mutationInFlightRef.current || state.kind === "submitting" || state.kind === "pending") return;
    mutationInFlightRef.current = true;
    setState({ kind: "submitting" });
    try {
      const result = await cancelCustomerOrder(orderId, accessMode);
      if (result.kind === "completed") {
        await finishCancellation(result.order);
        return;
      }
      setState({ kind: "pending", operation: result.operation, elapsedMs: 0 });
    } catch (error) {
      await refreshOrderAfterConflict(error);
    } finally {
      mutationInFlightRef.current = false;
    }
  }

  // Burada 409 sonrasında kör retry yerine authoritative siparişi okuyup dış yüzeyi de güncelliyorum.
  async function refreshOrderAfterConflict(error: unknown) {
    if (!(error instanceof OrderCancellationClientError) || error.problem.status !== 409) {
      setState(attentionState(error));
      return;
    }

    try {
      const order = await loadOrderAfterCancellation(orderId, accessMode);
      if (order.status === 6) {
        await finishCancellation(order);
        return;
      }
      await onOrderUpdatedRef.current?.(order);
    } catch {
      // Burada asıl ProblemDetails mesajını koruyup ikincil refresh hatasıyla finansal sonucu değiştirmiyorum.
    }
    setState({ kind: "attention", message: cancellationProblemMessage(error.problem), traceId: error.problem.traceId });
  }

  // Burada kullanıcıya yeni finansal mutation göndermeden yalnız mevcut operasyonu tekrar sorgulama olanağı veriyorum.
  async function checkOperationNow() {
    setState({ kind: "submitting" });
    try {
      const operation = await loadCustomerOrderCancellation(orderId, accessMode);
      if (isCancellationPending(operation.status)) {
        setState({ kind: "pending", operation, elapsedMs: 0 });
      } else if (operation.status === 3) {
        await finishCancellation();
      } else {
        setState({ kind: "attention", message: cancellationOperationMessage(operation) });
      }
    } catch (error) {
      setState(attentionState(error));
    }
  }

  const isAccount = appearance === "account";
  const buttonClassName = isAccount
    ? "focus-ring min-h-11 w-full cursor-pointer border border-danger/40 px-4 text-sm font-bold text-danger transition-colors hover:bg-danger/5 disabled:cursor-wait disabled:text-ink-muted"
    : "focus-ring inline-flex min-h-12 w-full cursor-pointer items-center justify-center rounded-lg border border-danger/40 bg-surface px-5 text-sm font-bold text-danger transition-colors hover:bg-danger/5 disabled:cursor-wait disabled:border-line disabled:text-ink-muted";
  const panelClassName = isAccount
    ? "border border-danger/25 bg-danger/5 p-4"
    : "rounded-lg border border-danger/25 bg-danger/5 p-4";

  if (state.kind === "idle") {
    return <button type="button" onClick={() => setState({ kind: "confirming" })} className={buttonClassName}>{label}</button>;
  }

  if (state.kind === "confirming") {
    return (
      <div ref={feedbackRef} tabIndex={-1} className={panelClassName} aria-labelledby={`cancel-order-${orderId}-title`}>
        <p id={`cancel-order-${orderId}-title`} className="text-sm font-bold text-ink">Siparişi iptal etmek istediğinizden emin misiniz?</p>
        <p className="mt-2 text-xs leading-5 text-ink-muted">{cancellationConfirmationMessage(orderStatus)}</p>
        <div className="mt-4 flex flex-wrap gap-2">
          <button type="button" onClick={() => void submitCancellation()} className="focus-ring min-h-10 cursor-pointer rounded-lg bg-danger px-4 text-xs font-bold text-white transition-colors hover:bg-danger/90">Evet, siparişi iptal et</button>
          <button type="button" onClick={() => setState({ kind: "idle" })} className="focus-ring min-h-10 cursor-pointer rounded-lg border border-line bg-surface px-4 text-xs font-bold text-ink transition-colors hover:bg-surface-subtle">Vazgeç</button>
        </div>
      </div>
    );
  }

  if (state.kind === "submitting") {
    return (
      <div ref={feedbackRef} tabIndex={-1} className="rounded-lg border border-line bg-surface-subtle p-4" role="status" aria-live="polite" aria-busy="true">
        <p className="text-sm font-bold text-ink">İptal durumu kontrol ediliyor…</p>
        <p className="mt-1 text-xs leading-5 text-ink-muted">Bu sırada ikinci bir iptal veya iade isteği gönderilmeyecek.</p>
      </div>
    );
  }

  if (state.kind === "pending") {
    return (
      <div ref={feedbackRef} tabIndex={-1} className="rounded-lg border border-brand-700/25 bg-surface-subtle p-4" role="status" aria-live="polite" aria-busy="true">
        <p className="text-sm font-bold text-ink">İptal işlemi doğrulanıyor</p>
        <p className="mt-1 text-xs leading-5 text-ink-muted">{cancellationOperationMessage(state.operation)}</p>
      </div>
    );
  }

  if (state.kind === "completed") {
    return <div ref={feedbackRef} tabIndex={-1} className="rounded-lg border border-success/25 bg-success/5 p-4 text-sm font-bold text-success" role="status">Siparişiniz iptal edildi.</div>;
  }

  return (
    <div ref={feedbackRef} tabIndex={-1} className={panelClassName} role="alert">
      <p className="text-sm font-bold text-danger">İptal işlemi tamamlanmadı</p>
      <p className="mt-2 text-xs leading-5 text-ink-muted">{state.message}</p>
      {state.traceId ? <p className="mt-2 break-all text-[11px] text-ink-muted">Takip kodu: {state.traceId}</p> : null}
      <button type="button" onClick={() => void checkOperationNow()} className="focus-ring mt-4 min-h-10 cursor-pointer rounded-lg border border-line bg-surface px-4 text-xs font-bold text-ink transition-colors hover:bg-surface-subtle">Durumu yeniden kontrol et</button>
    </div>
  );
}

// Burada bilinmeyen client hatasını güvenli ve kalıcı bir dikkat durumuna dönüştürüyorum.
function attentionState(error: unknown): CancellationViewState {
  if (error instanceof OrderCancellationClientError) {
    return { kind: "attention", message: cancellationProblemMessage(error.problem), traceId: error.problem.traceId };
  }
  return { kind: "attention", message: "İptal durumu kontrol edilemedi. Siparişiniz değiştirilmedi; daha sonra yeniden kontrol edin." };
}
