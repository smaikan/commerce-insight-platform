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

const selectControlClass =
  "min-h-9 w-full appearance-none rounded-lg border border-border-strong bg-surface-strong py-1.5 pl-3 pr-8 text-xs text-foreground outline-none transition-colors hover:border-border-strong/80 focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-60";

// Burada status ve assignment yapraklarını aynı authoritative concurrency snapshot üzerinde yönetiyorum.
export function ContactMessageControls({
  messageId,
  initialSnapshot,
  admins,
}: {
  messageId: string;
  initialSnapshot: ContactMessageMutationSnapshot;
  admins: readonly AssignableAdmin[];
}) {
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
      const result = await changeContactMessageStatusAction({
        messageId,
        currentStatus: snapshot.status,
        status: Number(statusTarget) as ContactMessageStatus,
        expectedConcurrencyToken: snapshot.concurrencyToken,
      });
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
      const result = await assignContactMessageAction({
        messageId,
        assignedAdminUserId: assigneeTarget || null,
        expectedConcurrencyToken: snapshot.concurrencyToken,
      });
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
    <div className="space-y-4 text-xs" aria-busy={pending}>
      {/* Durum Güncelleme */}
      <section aria-labelledby="message-status-heading">
        <label className="block">
          <span className="mb-1 block font-semibold text-muted">Durumu Değiştir</span>
          <div className="relative">
            <select
              value={statusTarget}
              onChange={(event) => {
                setStatusTarget(event.target.value);
                setStatusResult({ status: "idle" });
              }}
              className={selectControlClass}
              disabled={pending || statusConflict}
            >
              <option value="">Geçiş seçin...</option>
              {contactMessageStatusTransitions(snapshot.status).map((value) => (
                <option key={value} value={value}>
                  {contactMessageStatusLabel(value)}
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>
        </label>
        <button
          type="button"
          onClick={submitStatus}
          disabled={pending || statusTarget === "" || statusConflict}
          className="mt-2 inline-flex min-h-9 w-full cursor-pointer items-center justify-center rounded-lg bg-primary px-3 text-xs font-semibold text-white shadow-xs transition-colors hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary disabled:cursor-not-allowed disabled:opacity-60"
        >
          {pendingOperation === "status" ? "Kaydediliyor…" : "Durumu Güncelle"}
        </button>
        <MutationFeedback
          ref={statusAlertRef}
          result={statusResult}
          onAccept={() => acceptConflict(statusResult, setStatusResult)}
          admins={admins}
        />
      </section>

      {/* Yönetici Atama */}
      <section aria-labelledby="message-assignment-heading" className="border-t border-border/70 pt-4">
        <label className="block">
          <span className="mb-1 block font-semibold text-muted">Yöneticiye Ata</span>
          <div className="relative">
            <select
              value={assigneeTarget}
              onChange={(event) => {
                setAssigneeTarget(event.target.value);
                setAssignmentResult({ status: "idle" });
              }}
              className={selectControlClass}
              disabled={pending || assignmentConflict}
            >
              <option value="">Atanmamış</option>
              {admins.map((admin) => (
                <option key={admin.id} value={admin.id}>
                  {assignableAdminLabel(admin)}
                </option>
              ))}
            </select>
            <SelectChevron />
          </div>
        </label>
        <button
          type="button"
          onClick={submitAssignment}
          disabled={pending || assignmentConflict || assigneeTarget === (snapshot.assignedAdminUserId ?? "")}
          className="mt-2 inline-flex min-h-9 w-full cursor-pointer items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground transition-colors hover:border-primary hover:text-primary hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary disabled:cursor-not-allowed disabled:opacity-60"
        >
          {pendingOperation === "assignment" ? "Kaydediliyor…" : "Atamayı Kaydet"}
        </button>
        <MutationFeedback
          ref={assignmentAlertRef}
          result={assignmentResult}
          onAccept={() => acceptConflict(assignmentResult, setAssignmentResult)}
          admins={admins}
        />
      </section>
    </div>
  );
}

function SelectChevron() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 20 20"
      fill="currentColor"
      className="pointer-events-none absolute right-2.5 top-1/2 size-4 -translate-y-1/2 text-muted"
    >
      <path
        fillRule="evenodd"
        d="M5.22 8.22a.75.75 0 0 1 1.06 0L10 11.94l3.72-3.72a.75.75 0 1 1 1.06 1.06l-4.25 4.25a.75.75 0 0 1-1.06 0L5.22 9.28a.75.75 0 0 1 0-1.06Z"
        clipRule="evenodd"
      />
    </svg>
  );
}

// Burada başarı, hata ve concurrency kararını kalıcı ve klavye odağı alabilir bir bölgede sunuyorum.
function MutationFeedback({
  ref,
  result,
  onAccept,
  admins,
}: {
  ref: React.Ref<HTMLDivElement>;
  result: ContactMessageActionResult;
  onAccept: () => void;
  admins: readonly AssignableAdmin[];
}) {
  if (result.status === "idle") return null;
  if (result.status === "success")
    return (
      <div ref={ref} role="status" tabIndex={-1} className="mt-2.5 rounded-lg border border-success/25 bg-success/10 p-2.5 text-xs font-semibold text-success">
        {result.message}
      </div>
    );
  if (result.status === "conflict")
    return (
      <div
        ref={ref}
        role="alert"
        tabIndex={-1}
        className="mt-2.5 rounded-lg border border-warning/30 bg-warning/10 p-2.5 text-xs text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus"
      >
        <p className="font-semibold">{result.message}</p>
        {result.snapshot ? (
          <p className="mt-1.5 text-[11px] leading-4 text-muted">
            Güncel kayıt: {contactMessageStatusLabel(result.snapshot.status)} · {adminDisplayName(result.snapshot.assignedAdminUserId, admins)} ·{" "}
            {formatContactMessageDate(result.snapshot.updatedAt)}
          </p>
        ) : null}
        <button
          type="button"
          onClick={onAccept}
          disabled={!result.snapshot}
          className="mt-2.5 inline-flex min-h-8 w-full items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-2.5 text-xs font-semibold hover:border-primary disabled:opacity-60"
        >
          Güncel durumu kullan
        </button>
        {result.traceId ? <p className="mt-1.5 font-mono text-[10px] text-muted">İz: {result.traceId}</p> : null}
      </div>
    );
  return (
    <div
      ref={ref}
      role="alert"
      tabIndex={-1}
      className="mt-2.5 rounded-lg border border-danger/30 bg-danger/10 p-2.5 text-xs font-semibold text-danger outline-none focus-visible:ring-2 focus-visible:ring-focus"
    >
      {result.message}
      {result.traceId ? <span className="mt-1 block font-mono text-[10px] font-normal">İz: {result.traceId}</span> : null}
    </div>
  );
}
