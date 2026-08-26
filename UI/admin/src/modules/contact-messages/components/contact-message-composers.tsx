"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { addContactMessageNoteAction, replyContactMessageAction } from "@/modules/contact-messages/actions";
import { contactReplyIntentAfterEdit, createContactReplyIdempotencyKey, preserveContactDraftOnConflict } from "@/modules/contact-messages/mutation";
import type { ContactMessageActionResult, ContactMessageMutationSnapshot } from "@/modules/contact-messages/types";

const textareaClass =
  "min-h-24 w-full resize-y rounded-lg border border-border-strong bg-surface-strong px-3 py-2.5 text-xs text-foreground outline-none transition-colors hover:border-border-strong/80 focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-60";

// Burada dahili not taslağını 409 sırasında koruyup ancak açık kullanıcı kararı sonrası yeniden gönderilebilir kılıyorum.
export function InternalNoteComposer({
  messageId,
  initialSnapshot,
}: {
  messageId: string;
  initialSnapshot: ContactMessageMutationSnapshot;
}) {
  const router = useRouter();
  const [snapshot, setSnapshot] = useState(initialSnapshot);
  const [note, setNote] = useState("");
  const [result, setResult] = useState<ContactMessageActionResult>({ status: "idle" });
  const [pending, setPending] = useState(false);
  const feedbackRef = useRef<HTMLDivElement>(null);
  const inFlightRef = useRef(false);
  const fieldError = result.status === "error" ? result.fieldErrors?.note?.[0] : undefined;

  async function submit() {
    if (inFlightRef.current) return;
    inFlightRef.current = true;
    setPending(true);
    try {
      const next = await addContactMessageNoteAction({
        messageId,
        note,
        expectedConcurrencyToken: snapshot.concurrencyToken,
      });
      setResult(next);
      if (next.status === "success") {
        setSnapshot(next.snapshot);
        setNote("");
        router.refresh();
      } else queueMicrotask(() => feedbackRef.current?.focus());
    } finally {
      inFlightRef.current = false;
      setPending(false);
    }
  }

  const conflict = result.status === "conflict";

  return (
    <section aria-labelledby="internal-note-heading" aria-busy={pending}>
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border/70 pb-2.5">
        <div className="flex items-center gap-2">
          <span className="size-2 rounded-full bg-amber-500" />
          <h2 id="internal-note-heading" className="text-xs font-bold uppercase tracking-wider text-foreground">
            Dahili Not Ekle
          </h2>
        </div>
        <span className="rounded-md border border-amber-300/80 bg-amber-50 px-2 py-0.5 text-[10px] font-bold text-amber-800">
          Yalnız Yöneticiler Görür
        </span>
      </div>

      <p className="mt-2 text-xs leading-5 text-muted">
        Eklenen notlar activity akışına kaydedilir; silinemez veya düzenlenemez.
      </p>

      <div className="mt-3">
        <label htmlFor="internal-note-input" className="sr-only">
          Dahili Not Metni
        </label>
        <textarea
          id="internal-note-input"
          value={note}
          onChange={(event) => {
            setNote(event.target.value);
            if (result.status !== "conflict") setResult({ status: "idle" });
          }}
          placeholder="Yönetici arkadaşlarınız için bir not yazın..."
          maxLength={2_000}
          aria-invalid={Boolean(fieldError)}
          aria-describedby={fieldError ? "note-field-error" : "note-help"}
          disabled={pending || conflict}
          className={textareaClass}
        />
      </div>

      <div className="mt-2.5 flex items-center justify-between gap-3">
        <p id="note-help" className="font-mono text-[11px] tabular-nums text-muted">
          {note.length}/2000 karakter
        </p>
        <button
          type="button"
          onClick={submit}
          disabled={pending || conflict || !note.trim()}
          className="inline-flex min-h-9 cursor-pointer items-center justify-center rounded-lg bg-amber-600 px-4 text-xs font-semibold text-white shadow-xs transition-colors hover:bg-amber-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-500 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {pending ? "Ekleniyor…" : "Notu Kaydet"}
        </button>
      </div>

      {fieldError ? (
        <p id="note-field-error" className="mt-2 text-xs font-semibold text-danger">
          {fieldError}
        </p>
      ) : null}

      <ComposerFeedback
        ref={feedbackRef}
        result={result}
        onAcceptConflict={() => {
          if (result.status === "conflict" && result.snapshot) {
            const preserved = preserveContactDraftOnConflict(note, result.snapshot);
            setNote(preserved.draft);
            setSnapshot(preserved.snapshot);
            setResult({ status: "idle" });
            router.refresh();
          }
        }}
      />
    </section>
  );
}

// Burada alıcıyı değiştirilemez tutup aynı yanıt intentinde idempotency key'i ağ retry'ında koruyorum.
export function ReplyComposer({ messageId }: { messageId: string }) {
  const router = useRouter();
  const [body, setBody] = useState("");
  const [key, setKey] = useState(() => createContactReplyIdempotencyKey());
  const [attemptedBody, setAttemptedBody] = useState<string>();
  const [result, setResult] = useState<ContactMessageActionResult>({ status: "idle" });
  const [pending, setPending] = useState(false);
  const feedbackRef = useRef<HTMLDivElement>(null);
  const inFlightRef = useRef(false);
  const fieldError = result.status === "error" ? result.fieldErrors?.body?.[0] : undefined;

  function changeBody(nextBody: string) {
    const nextIntent = contactReplyIntentAfterEdit({ key, attemptedBody }, nextBody);
    if (nextIntent.key !== key) setKey(nextIntent.key);
    if (nextIntent.attemptedBody !== attemptedBody) setAttemptedBody(nextIntent.attemptedBody);
    setBody(nextBody);
    setResult({ status: "idle" });
  }

  async function submit() {
    if (inFlightRef.current) return;
    inFlightRef.current = true;
    const intentBody = body.trim();
    setAttemptedBody(intentBody);
    setPending(true);
    try {
      const next = await replyContactMessageAction({ messageId, body: intentBody, idempotencyKey: key });
      setResult(next);
      if (next.status === "success") {
        setBody("");
        setAttemptedBody(undefined);
        setKey(createContactReplyIdempotencyKey());
        router.refresh();
      } else queueMicrotask(() => feedbackRef.current?.focus());
    } finally {
      inFlightRef.current = false;
      setPending(false);
    }
  }

  return (
    <section aria-labelledby="reply-heading" aria-busy={pending}>
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border/70 pb-2.5">
        <div className="flex items-center gap-2">
          <span className="size-2 rounded-full bg-blue-600" />
          <h2 id="reply-heading" className="text-xs font-bold uppercase tracking-wider text-foreground">
            Müşteriye Yanıt Gönder
          </h2>
        </div>
        <span className="text-[11px] text-muted">E-posta ile iletilir</span>
      </div>

      <p className="mt-2 text-xs leading-5 text-muted">
        Yanıt metni, mesaj sahibinin kayıtlı e-posta adresine SMTP teslimat kuyruğu üzerinden ulaştırılır.
      </p>

      <div className="mt-3">
        <label htmlFor="reply-body-input" className="sr-only">
          Yanıt Metni
        </label>
        <textarea
          id="reply-body-input"
          value={body}
          onChange={(event) => changeBody(event.target.value)}
          placeholder="Müşteriye iletilecek yanıt metnini girin..."
          maxLength={5_000}
          aria-invalid={Boolean(fieldError)}
          aria-describedby={fieldError ? "reply-field-error" : "reply-help"}
          disabled={pending}
          className="min-h-28 w-full resize-y rounded-lg border border-border-strong bg-surface-strong px-3 py-2.5 text-xs text-foreground outline-none transition-colors hover:border-border-strong/80 focus:border-primary focus:ring-2 focus:ring-primary/20 disabled:cursor-not-allowed disabled:opacity-60"
        />
      </div>

      <div className="mt-2.5 flex items-center justify-between gap-3">
        <p id="reply-help" className="font-mono text-[11px] tabular-nums text-muted">
          {body.length}/5000 karakter
        </p>
        <button
          type="button"
          onClick={submit}
          disabled={pending || !body.trim()}
          className="inline-flex min-h-9 cursor-pointer items-center justify-center rounded-lg bg-primary px-4 text-xs font-semibold text-white shadow-xs transition-colors hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary disabled:cursor-not-allowed disabled:opacity-60"
        >
          {pending ? "Sıraya Alınıyor…" : "Yanıtı Gönder"}
        </button>
      </div>

      {fieldError ? (
        <p id="reply-field-error" className="mt-2 text-xs font-semibold text-danger">
          {fieldError}
        </p>
      ) : null}

      <ComposerFeedback ref={feedbackRef} result={result} />
    </section>
  );
}

// Burada composer sonucunu toast yerine kalıcı status/alert bölgesinde, trace kimliğiyle gösteriyorum.
function ComposerFeedback({
  ref,
  result,
  onAcceptConflict,
}: {
  ref: React.Ref<HTMLDivElement>;
  result: ContactMessageActionResult;
  onAcceptConflict?: () => void;
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
        {result.snapshot && onAcceptConflict ? (
          <button
            type="button"
            onClick={onAcceptConflict}
            className="mt-2 inline-flex min-h-8 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-2.5 text-xs font-semibold hover:border-primary"
          >
            Güncel kaydı kullan; taslağı koru
          </button>
        ) : null}
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
      {result.retryAfter ? <span className="mt-1 block font-normal">Retry-After: {result.retryAfter}</span> : null}
      {result.traceId ? <span className="mt-1 block font-mono text-[10px] font-normal">İz: {result.traceId}</span> : null}
    </div>
  );
}
