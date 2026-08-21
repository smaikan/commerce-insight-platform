"use client";

import { useRef, useState } from "react";
import { useRouter } from "next/navigation";
import { addContactMessageNoteAction, replyContactMessageAction } from "@/modules/contact-messages/actions";
import { contactReplyIntentAfterEdit, createContactReplyIdempotencyKey, preserveContactDraftOnConflict } from "@/modules/contact-messages/mutation";
import type { ContactMessageActionResult, ContactMessageMutationSnapshot } from "@/modules/contact-messages/types";

const textareaClass = "min-h-32 w-full resize-y rounded-lg border border-border-strong bg-surface-strong px-3 py-2 text-sm leading-6 text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-focus/30 disabled:cursor-not-allowed disabled:opacity-60";

// Burada dahili not taslağını 409 sırasında koruyup ancak açık kullanıcı kararı sonrası yeniden gönderilebilir kılıyorum.
export function InternalNoteComposer({ messageId, initialSnapshot }: { messageId: string; initialSnapshot: ContactMessageMutationSnapshot }) {
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
      const next = await addContactMessageNoteAction({ messageId, note, expectedConcurrencyToken: snapshot.concurrencyToken });
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
    <section aria-labelledby="internal-note-heading" className="border-t border-border pt-6" aria-busy={pending}>
      <div className="flex flex-wrap items-baseline justify-between gap-2"><h2 id="internal-note-heading" className="text-base font-semibold text-foreground">Dahili not ekle</h2><span className="rounded-md bg-amber-50 px-2 py-1 text-xs font-bold text-amber-800">Yalnız yöneticiler görür</span></div>
      <p className="mt-2 text-sm leading-6 text-muted">Notlar activity akışına eklenir; düzenlenemez veya silinemez.</p>
      <label className="mt-4 block"><span className="mb-1.5 block text-sm font-semibold text-foreground">Not</span><textarea value={note} onChange={(event) => { setNote(event.target.value); if (result.status !== "conflict") setResult({ status: "idle" }); }} maxLength={2_000} aria-invalid={Boolean(fieldError)} aria-describedby={fieldError ? "note-field-error" : "note-help"} disabled={pending || conflict} className={textareaClass} /></label>
      <div className="mt-2 flex items-center justify-between gap-3"><p id="note-help" className="text-xs text-muted">{note.length}/2000 karakter</p><button type="button" onClick={submit} disabled={pending || conflict || !note.trim()} className="min-h-10 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Ekleniyor…" : "Notu activity'ye ekle"}</button></div>
      {fieldError ? <p id="note-field-error" className="mt-2 text-sm font-semibold text-danger">{fieldError}</p> : null}
      <ComposerFeedback ref={feedbackRef} result={result} onAcceptConflict={() => { if (result.status === "conflict" && result.snapshot) { const preserved = preserveContactDraftOnConflict(note, result.snapshot); setNote(preserved.draft); setSnapshot(preserved.snapshot); setResult({ status: "idle" }); router.refresh(); } }} />
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
    <section aria-labelledby="reply-heading" className="border-t border-border pt-6" aria-busy={pending}>
      <h2 id="reply-heading" className="text-base font-semibold text-foreground">Müşteriye yanıt</h2>
      <p className="mt-2 text-sm leading-6 text-muted">Alıcı, mesaj kaydındaki e-posta adresidir ve buradan değiştirilemez. Kabul edilen yanıt önce gönderim sırasına alınır.</p>
      <label className="mt-4 block"><span className="mb-1.5 block text-sm font-semibold text-foreground">Yanıt metni</span><textarea value={body} onChange={(event) => changeBody(event.target.value)} maxLength={5_000} aria-invalid={Boolean(fieldError)} aria-describedby={fieldError ? "reply-field-error" : "reply-help"} disabled={pending} className={textareaClass} /></label>
      <div className="mt-2 flex items-center justify-between gap-3"><p id="reply-help" className="text-xs text-muted">{body.length}/5000 karakter</p><button type="button" onClick={submit} disabled={pending || !body.trim()} className="min-h-10 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Sıraya alınıyor…" : "Yanıtı sıraya al"}</button></div>
      {fieldError ? <p id="reply-field-error" className="mt-2 text-sm font-semibold text-danger">{fieldError}</p> : null}
      <ComposerFeedback ref={feedbackRef} result={result} />
    </section>
  );
}

// Burada composer sonucunu toast yerine kalıcı status/alert bölgesinde, trace kimliğiyle gösteriyorum.
function ComposerFeedback({ ref, result, onAcceptConflict }: { ref: React.Ref<HTMLDivElement>; result: ContactMessageActionResult; onAcceptConflict?: () => void }) {
  if (result.status === "idle") return null;
  if (result.status === "success") return <div ref={ref} role="status" tabIndex={-1} className="mt-3 rounded-lg border border-success/25 bg-success/10 p-3 text-sm font-semibold text-success">{result.message}</div>;
  if (result.status === "conflict") return <div ref={ref} role="alert" tabIndex={-1} className="mt-3 rounded-lg border border-warning/30 bg-warning/10 p-3 text-sm text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus"><p className="font-semibold">{result.message}</p>{result.snapshot && onAcceptConflict ? <button type="button" onClick={onAcceptConflict} className="mt-3 min-h-10 rounded-lg border border-border-strong bg-surface-strong px-3 font-semibold">Güncel kaydı kullan; taslağı koru</button> : null}</div>;
  return <div ref={ref} role="alert" tabIndex={-1} className="mt-3 rounded-lg border border-danger/30 bg-danger/10 p-3 text-sm font-semibold text-danger outline-none focus-visible:ring-2 focus-visible:ring-focus">{result.message}{result.retryAfter ? <span className="mt-1 block font-normal">Retry-After: {result.retryAfter}</span> : null}{result.traceId ? <span className="mt-1 block font-mono text-[11px] font-normal">İz: {result.traceId}</span> : null}</div>;
}
