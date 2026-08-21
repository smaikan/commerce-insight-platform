"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { assignContactMessageAction, changeContactMessageStatusAction } from "@/modules/contact-messages/actions";
import {
  adminDisplayName,
  assignableAdminLabel,
  contactMessageStatusLabel,
  contactMessageStatusTransitions,
  formatContactMessageDate,
} from "@/modules/contact-messages/presentation";
import type { AssignableAdmin, ContactMessageActionResult, ContactMessageMutationSnapshot, ContactMessageStatus } from "@/modules/contact-messages/types";

const controlClass = "min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 disabled:cursor-not-allowed disabled:opacity-60";

// Burada status ve assignment yapraklarını aynı authoritative concurrency snapshot üzerinde yönetiyorum.
export function ContactMessageControls({ messageId, initialSnapshot, admins }: { messageId: string; initialSnapshot: ContactMessageMutationSnapshot; admins: readonly AssignableAdmin[] }) {
  const router = useRouter();
  const [snapshot, setSnapshot] = useState(initialSnapshot);
  const [statusTarget, setStatusTarget] = useState<string>("");
  const [assigneeTarget, setAssigneeTarget] = useState(initialSnapshot.assignedAdminUserId ?? "");
  const [statusResult, setStatusResult] = useState<ContactMessageActionResult>({ status: "idle" });
  const [assignmentResult, setAssignmentResult] = useState<ContactMessageActionResult>({ status: "idle" });
  const [pendingOperation, setPendingOperation] = useState<"status" | "assignment" | null>(null);
  const statusAlertRef = useRef<HTMLDivElement>(null);
  const assignmentAlertRef = useRef<HTMLDivElement>(null);
  const inFlightRef = useRef(false);

  async function submitStatus() {
    if (statusTarget === "" || inFlightRef.current) return;
    inFlightRef.current = true;
    setPendingOperation("status");
    try {
      const result = await changeContactMessageStatusAction({ messageId, currentStatus: snapshot.status, status: Number(statusTarget) as ContactMessageStatus, expectedConcurrencyToken: snapshot.concurrencyToken });
      setStatusResult(result);
      if (result.status === "success") {
        setSnapshot(result.snapshot);
        setStatusTarget("");
        router.refresh();
      } else if (result.status === "conflict" || result.status === "error") {
        queueMicrotask(() => statusAlertRef.current?.focus());
      }
    } finally {
      inFlightRef.current = false;
      setPendingOperation(null);
    }
  }

  async function submitAssignment() {
    if (inFlightRef.current) return;
    inFlightRef.current = true;
    setPendingOperation("assignment");
    try {
      const result = await assignContactMessageAction({ messageId, assignedAdminUserId: assigneeTarget || null, expectedConcurrencyToken: snapshot.concurrencyToken });
      setAssignmentResult(result);
      if (result.status === "success") {
        setSnapshot(result.snapshot);
        router.refresh();
      } else if (result.status === "conflict" || result.status === "error") {
        queueMicrotask(() => assignmentAlertRef.current?.focus());
      }
    } finally {
      inFlightRef.current = false;
      setPendingOperation(null);
    }
  }

  function acceptConflict(result: ContactMessageActionResult, setResult: (result: ContactMessageActionResult) => void) {
    if (result.status !== "conflict" || !result.snapshot) return;
    setSnapshot(result.snapshot);
    setAssigneeTarget(result.snapshot.assignedAdminUserId ?? "");
    setStatusTarget("");
    setResult({ status: "idle" });
    router.refresh();
  }

  const statusConflict = statusResult.status === "conflict";
  const assignmentConflict = assignmentResult.status === "conflict";
  const pending = pendingOperation !== null;
  return (
    <div className="space-y-5" aria-busy={pending}>
      <section aria-labelledby="message-status-heading">
        <h2 id="message-status-heading" className="text-xs font-bold uppercase tracking-[0.08em] text-muted">Durum</h2>
        <p className="mt-2 text-sm font-semibold text-foreground">{contactMessageStatusLabel(snapshot.status)}</p>
        <label className="mt-3 block">
          <span className="mb-1.5 block text-xs font-semibold text-muted">Yeni durum</span>
          <select value={statusTarget} onChange={(event) => { setStatusTarget(event.target.value); setStatusResult({ status: "idle" }); }} className={controlClass} disabled={pending || statusConflict}>
            <option value="">Geçiş seçin</option>
            {contactMessageStatusTransitions(snapshot.status).map((value) => <option key={value} value={value}>{contactMessageStatusLabel(value)}</option>)}
          </select>
        </label>
        <button type="button" onClick={submitStatus} disabled={pending || statusTarget === "" || statusConflict} className="mt-2 min-h-10 w-full rounded-lg bg-primary px-3 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pendingOperation === "status" ? "Kaydediliyor…" : "Durumu güncelle"}</button>
        <MutationFeedback ref={statusAlertRef} result={statusResult} onAccept={() => acceptConflict(statusResult, setStatusResult)} admins={admins} />
      </section>

      <section aria-labelledby="message-assignment-heading" className="border-t border-border pt-5">
        <h2 id="message-assignment-heading" className="text-xs font-bold uppercase tracking-[0.08em] text-muted">Atama</h2>
        <p className="mt-2 truncate text-sm text-foreground">Şu an: {adminDisplayName(snapshot.assignedAdminUserId, admins)}</p>
        <label className="mt-3 block">
          <span className="mb-1.5 block text-xs font-semibold text-muted">Yönetici</span>
          <select value={assigneeTarget} onChange={(event) => { setAssigneeTarget(event.target.value); setAssignmentResult({ status: "idle" }); }} className={controlClass} disabled={pending || assignmentConflict}>
            <option value="">Atanmamış</option>
            {admins.map((admin) => <option key={admin.id} value={admin.id}>{assignableAdminLabel(admin)}</option>)}
          </select>
        </label>
        <button type="button" onClick={submitAssignment} disabled={pending || assignmentConflict || assigneeTarget === (snapshot.assignedAdminUserId ?? "")} className="mt-2 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:border-primary hover:text-primary disabled:cursor-not-allowed disabled:opacity-60">{pendingOperation === "assignment" ? "Kaydediliyor…" : "Atamayı kaydet"}</button>
        <MutationFeedback ref={assignmentAlertRef} result={assignmentResult} onAccept={() => acceptConflict(assignmentResult, setAssignmentResult)} admins={admins} />
      </section>
    </div>
  );
}

// Burada başarı, hata ve concurrency kararını kalıcı ve klavye odağı alabilir bir bölgede sunuyorum.
function MutationFeedback({ ref, result, onAccept, admins }: { ref: React.Ref<HTMLDivElement>; result: ContactMessageActionResult; onAccept: () => void; admins: readonly AssignableAdmin[] }) {
  if (result.status === "idle") return null;
  if (result.status === "success") return <div ref={ref} role="status" tabIndex={-1} className="mt-3 rounded-lg border border-success/25 bg-success/10 p-3 text-sm font-semibold text-success">{result.message}</div>;
  if (result.status === "conflict") return (
    <div ref={ref} role="alert" tabIndex={-1} className="mt-3 rounded-lg border border-warning/30 bg-warning/10 p-3 text-sm text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus">
      <p className="font-semibold">{result.message}</p>
      {result.snapshot ? <p className="mt-2 text-xs leading-5 text-muted">Güncel kayıt: {contactMessageStatusLabel(result.snapshot.status)} · {adminDisplayName(result.snapshot.assignedAdminUserId, admins)} · {formatContactMessageDate(result.snapshot.updatedAt)}</p> : null}
      <button type="button" onClick={onAccept} disabled={!result.snapshot} className="mt-3 min-h-10 rounded-lg border border-border-strong bg-surface-strong px-3 font-semibold hover:border-primary disabled:opacity-60">Güncel durumu kullan</button>
      {result.traceId ? <p className="mt-2 font-mono text-[11px] text-muted">İz: {result.traceId}</p> : null}
    </div>
  );
  return <div ref={ref} role="alert" tabIndex={-1} className="mt-3 rounded-lg border border-danger/30 bg-danger/10 p-3 text-sm font-semibold text-danger outline-none focus-visible:ring-2 focus-visible:ring-focus">{result.message}{result.traceId ? <span className="mt-1 block font-mono text-[11px] font-normal">İz: {result.traceId}</span> : null}</div>;
}
