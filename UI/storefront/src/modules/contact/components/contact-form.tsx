"use client";

import Link from "next/link";
import { useEffect, useRef, useState, type FormEvent } from "react";

import { ApiError } from "@/lib/api/problem";
import { submitContactMessage } from "@/modules/contact/client";
import { ContactTurnstile } from "@/modules/contact/components/contact-turnstile";
import {
  CONTACT_SUBJECT_OPTIONS,
  EMPTY_CONTACT_DRAFT,
  type ContactDraft,
  type ContactFieldErrors,
  type ContactFieldName,
  type ContactMessageSubject,
  type ContactSubmissionReceipt,
} from "@/modules/contact/types";
import { mapApiFieldErrors, validateContactDraft } from "@/modules/contact/validation";

type ContactFormProps = { turnstileSiteKey: string; turnstileRequired: boolean };
type Intent = { fingerprint: string; idempotencyKey: string };

const fieldClass = "focus-ring mt-2 w-full rounded-xl border bg-surface-subtle/30 px-3.5 text-sm text-ink transition-colors placeholder:text-ink-muted/60 hover:border-line-strong focus:border-brand-600 focus:bg-surface";

function createIdempotencyKey(): string {
  return `contact-${crypto.randomUUID()}`;
}

function retryDelaySeconds(value?: string): number {
  if (!value) return 60;
  const seconds = Number(value);
  if (Number.isFinite(seconds) && seconds > 0) return Math.ceil(seconds);
  const dateDelay = Math.ceil((Date.parse(value) - Date.now()) / 1_000);
  return Number.isFinite(dateDelay) && dateDelay > 0 ? dateDelay : 60;
}

function firstError(errors: ContactFieldErrors, field: ContactFieldName): string | undefined {
  return errors[field]?.[0];
}

function errorMessage(error: unknown): string {
  if (!(error instanceof ApiError)) {
    return "Talebiniz şu anda alınamadı. Form içeriğiniz korunuyor; lütfen tekrar deneyin.";
  }
  switch (error.problem.code) {
    case "idempotency_key_reused":
      return "Form içeriği önceki gönderimden sonra değişti. Bilgilerinizi kontrol edip tekrar gönderin.";
    case "turnstile_token_required":
    case "turnstile_verification_failed":
      return "Güvenlik doğrulaması tamamlanamadı. Doğrulamayı yenileyip tekrar deneyin.";
    case "rate_limit_exceeded":
      return "Çok sayıda deneme yapıldı. Belirtilen süre dolduktan sonra tekrar deneyin.";
    default:
      return error.problem.detail || "Talebiniz şu anda alınamadı. Form içeriğiniz korunuyor; lütfen tekrar deneyin.";
  }
}

export function ContactForm({ turnstileSiteKey, turnstileRequired }: ContactFormProps) {
  const [draft, setDraft] = useState<ContactDraft>(EMPTY_CONTACT_DRAFT);
  const [errors, setErrors] = useState<ContactFieldErrors>({});
  const [formError, setFormError] = useState("");
  const [receipt, setReceipt] = useState<ContactSubmissionReceipt | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [turnstileToken, setTurnstileToken] = useState("");
  const [challengeError, setChallengeError] = useState("");
  const [challengeRequired, setChallengeRequired] = useState(turnstileRequired);
  const [challengeResetVersion, setChallengeResetVersion] = useState(0);
  const [retryUntil, setRetryUntil] = useState<number | null>(null);
  const [retryRemaining, setRetryRemaining] = useState(0);
  const intentRef = useRef<Intent | null>(null);
  const errorSummaryRef = useRef<HTMLDivElement>(null);
  const firstInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (formError) errorSummaryRef.current?.focus();
  }, [formError]);

  useEffect(() => {
    if (!retryUntil) return;
    const update = () => {
      const remaining = Math.max(0, Math.ceil((retryUntil - Date.now()) / 1_000));
      setRetryRemaining(remaining);
      if (remaining === 0) setRetryUntil(null);
    };
    update();
    const timer = window.setInterval(update, 1_000);
    return () => window.clearInterval(timer);
  }, [retryUntil]);

  function updateField<K extends keyof ContactDraft>(field: K, value: ContactDraft[K]) {
    setDraft((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
    setFormError("");
  }

  function fieldA11y(field: ContactFieldName, helpId?: string) {
    const hasError = Boolean(firstError(errors, field));
    return {
      "aria-invalid": hasError || undefined,
      "aria-describedby": [helpId, hasError ? `contact-${field}-error` : null].filter(Boolean).join(" ") || undefined,
    };
  }

  function resetChallenge() {
    setTurnstileToken("");
    setChallengeResetVersion((value) => value + 1);
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting || retryRemaining > 0) return;

    setFormError("");
    setErrors({});
    const result = validateContactDraft(draft);
    if (!result.ok) {
      setErrors(result.errors);
      setFormError(result.formError || "Lütfen işaretli alanları kontrol edin.");
      return;
    }
    if (challengeRequired && !turnstileToken) {
      setChallengeError("Mesajı göndermeden önce güvenlik doğrulamasını tamamlayın.");
      setFormError("Güvenlik doğrulaması gerekli.");
      return;
    }

    if (!intentRef.current || intentRef.current.fingerprint !== result.fingerprint) {
      intentRef.current = { fingerprint: result.fingerprint, idempotencyKey: createIdempotencyKey() };
    }

    setIsSubmitting(true);
    try {
      const nextReceipt = await submitContactMessage(result.value, intentRef.current.idempotencyKey, turnstileToken || undefined);
      setReceipt(nextReceipt);
      setDraft(EMPTY_CONTACT_DRAFT);
      setErrors({});
      setFormError("");
      intentRef.current = null;
      resetChallenge();
    } catch (error) {
      if (error instanceof ApiError) {
        const apiErrors = mapApiFieldErrors(error.problem.errors);
        if (Object.keys(apiErrors).length > 0) setErrors(apiErrors);
        if (error.problem.status === 428 || error.problem.code?.includes("turnstile")) {
          setChallengeRequired(true);
          setChallengeError("Güvenlik doğrulamasını yenileyip tekrar deneyin.");
        }
        if (error.problem.status === 429) {
          const delay = retryDelaySeconds(error.problem.retryAfter);
          setRetryRemaining(delay);
          setRetryUntil(Date.now() + delay * 1_000);
        }
        if (error.problem.code === "idempotency_key_reused") intentRef.current = null;
      }
      setFormError(errorMessage(error));
      resetChallenge();
    } finally {
      setIsSubmitting(false);
    }
  }

  function handleNewMessage() {
    setReceipt(null);
    setDraft(EMPTY_CONTACT_DRAFT);
    setFormError("");
    setErrors({});
    setChallengeError("");
    requestAnimationFrame(() => firstInputRef.current?.focus());
  }

  if (receipt) {
    return (
      <section className="rounded-2xl border border-brand-200 bg-brand-50/50 p-8 text-center sm:p-10" aria-labelledby="contact-success-title" role="status">
        <div className="mx-auto flex size-14 items-center justify-center rounded-full bg-brand-700 text-white shadow-sm" aria-hidden="true">
          <svg className="size-7" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}><path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" /></svg>
        </div>
        <h3 id="contact-success-title" className="mt-4 text-xl font-bold text-brand-950">Talebiniz alındı</h3>
        <p className="mt-2 text-sm leading-relaxed text-ink-muted">Müşteri deneyimi ekibimiz mesajınızı inceleyecek. Takip için referans numaranızı saklayın.</p>
        <p className="mt-5 text-xs font-semibold uppercase tracking-wider text-ink-muted">Referans numarası</p>
        <p className="mt-1 select-all text-lg font-bold tracking-wide text-brand-950">{receipt.referenceNumber}</p>
        <button type="button" onClick={handleNewMessage} className="focus-ring mt-6 inline-flex h-11 items-center justify-center rounded-xl bg-brand-950 px-6 text-sm font-semibold text-white transition-colors hover:bg-brand-800">Yeni mesaj gönder</button>
      </section>
    );
  }

  return (
    <form onSubmit={handleSubmit} noValidate aria-busy={isSubmitting} className="space-y-5 rounded-2xl border border-line bg-surface p-6 shadow-sm sm:p-8">
      {formError ? (
        <div ref={errorSummaryRef} tabIndex={-1} role="alert" className="focus-ring rounded-xl border border-danger/30 bg-danger/5 p-4">
          <p className="text-sm font-bold text-danger">Mesaj gönderilemedi</p>
          <p className="mt-1 text-sm leading-6 text-ink">{formError}</p>
        </div>
      ) : null}

      <div className="grid gap-5 sm:grid-cols-2">
        <div>
          <label htmlFor="contact-name" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Adınız Soyadınız <span className="text-brand-600" aria-hidden="true">*</span></label>
          <input ref={firstInputRef} id="contact-name" name="name" autoComplete="name" maxLength={150} value={draft.name} onChange={(event) => updateField("name", event.target.value)} {...fieldA11y("name")} className={`${fieldClass} h-11 ${firstError(errors, "name") ? "border-danger" : "border-line"}`} />
          {firstError(errors, "name") ? <p id="contact-name-error" className="mt-1.5 text-xs font-semibold text-danger">{firstError(errors, "name")}</p> : null}
        </div>
        <div>
          <label htmlFor="contact-email" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">E-posta Adresiniz <span className="text-brand-600" aria-hidden="true">*</span></label>
          <input id="contact-email" name="email" type="email" inputMode="email" autoComplete="email" maxLength={320} value={draft.email} onChange={(event) => updateField("email", event.target.value)} {...fieldA11y("email")} className={`${fieldClass} h-11 ${firstError(errors, "email") ? "border-danger" : "border-line"}`} />
          {firstError(errors, "email") ? <p id="contact-email-error" className="mt-1.5 text-xs font-semibold text-danger">{firstError(errors, "email")}</p> : null}
        </div>
      </div>

      <div className="grid gap-5 sm:grid-cols-2">
        <div>
          <label htmlFor="contact-phone" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Telefon Numarası <span className="font-normal normal-case">(isteğe bağlı)</span></label>
          <input id="contact-phone" name="phone" type="tel" inputMode="tel" autoComplete="tel" maxLength={30} value={draft.phone} onChange={(event) => updateField("phone", event.target.value)} {...fieldA11y("phone")} className={`${fieldClass} h-11 ${firstError(errors, "phone") ? "border-danger" : "border-line"}`} />
          {firstError(errors, "phone") ? <p id="contact-phone-error" className="mt-1.5 text-xs font-semibold text-danger">{firstError(errors, "phone")}</p> : null}
        </div>
        <div>
          <label htmlFor="contact-order" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Sipariş Numarası <span className="font-normal normal-case">(varsa)</span></label>
          <input id="contact-order" name="orderNumber" maxLength={50} value={draft.orderNumber} onChange={(event) => updateField("orderNumber", event.target.value)} placeholder="Örn. ORD-20260821-000001" {...fieldA11y("orderNumber", "contact-order-help")} className={`${fieldClass} h-11 ${firstError(errors, "orderNumber") ? "border-danger" : "border-line"}`} />
          <p id="contact-order-help" className="mt-1.5 text-xs text-ink-muted">Varsa hesabınızdaki sipariş numarasını eksiksiz yazın.</p>
          {firstError(errors, "orderNumber") ? <p id="contact-orderNumber-error" className="mt-1.5 text-xs font-semibold text-danger">{firstError(errors, "orderNumber")}</p> : null}
        </div>
      </div>

      <div>
        <label htmlFor="contact-subject" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Konu <span className="text-brand-600" aria-hidden="true">*</span></label>
        <select id="contact-subject" name="subject" value={draft.subject} onChange={(event) => updateField("subject", Number(event.target.value) as ContactMessageSubject)} {...fieldA11y("subject")} className={`${fieldClass} h-11 ${firstError(errors, "subject") ? "border-danger" : "border-line"}`}>
          {CONTACT_SUBJECT_OPTIONS.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}
        </select>
        {firstError(errors, "subject") ? <p id="contact-subject-error" className="mt-1.5 text-xs font-semibold text-danger">{firstError(errors, "subject")}</p> : null}
      </div>

      <div>
        <div className="flex items-end justify-between gap-3">
          <label htmlFor="contact-message" className="block text-xs font-semibold uppercase tracking-wider text-ink-muted">Mesajınız <span className="text-brand-600" aria-hidden="true">*</span></label>
          <span id="contact-message-count" className="text-xs tabular-nums text-ink-muted">{draft.message.length}/5000</span>
        </div>
        <textarea id="contact-message" name="message" rows={6} minLength={20} maxLength={5_000} value={draft.message} onChange={(event) => updateField("message", event.target.value)} placeholder="Talebinizi en az 20 karakterle ayrıntılı biçimde yazın." {...fieldA11y("message", "contact-message-count")} className={`${fieldClass} p-3.5 ${firstError(errors, "message") ? "border-danger" : "border-line"}`} />
        {firstError(errors, "message") ? <p id="contact-message-error" className="mt-1.5 text-xs font-semibold text-danger">{firstError(errors, "message")}</p> : null}
      </div>

      {challengeRequired ? (
        <ContactTurnstile siteKey={turnstileSiteKey} resetVersion={challengeResetVersion} error={challengeError} onToken={(token) => { setTurnstileToken(token); setChallengeError(""); }} onExpired={() => { setTurnstileToken(""); setChallengeError("Doğrulamanın süresi doldu. Lütfen yeniden tamamlayın."); }} onError={() => { setTurnstileToken(""); setChallengeError("Güvenlik doğrulaması yüklenemedi. Lütfen tekrar deneyin."); }} />
      ) : null}

      <p className="text-xs leading-5 text-ink-muted">Formu gönderdiğinizde iletişim talebinizin yanıtlanması amacıyla verdiğiniz bilgiler işlenir. Ayrıntılar için <Link href="/privacy-policy" target="_blank" rel="noreferrer" className="focus-ring font-semibold text-brand-700 underline underline-offset-2">KVKK ve Gizlilik Politikası<span className="sr-only"> (yeni sekmede açılır)</span></Link>.</p>

      <button type="submit" disabled={isSubmitting || retryRemaining > 0 || (challengeRequired && !turnstileToken)} className="focus-ring inline-flex h-12 w-full cursor-pointer items-center justify-center rounded-xl bg-brand-950 px-6 text-sm font-semibold text-white transition-colors hover:bg-brand-800 disabled:cursor-not-allowed disabled:bg-line disabled:text-ink-muted">
        {isSubmitting ? <span className="inline-flex items-center gap-2"><svg className="size-4 animate-spin motion-reduce:animate-none" viewBox="0 0 24 24" fill="none" aria-hidden="true"><circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" /><path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" /></svg>Gönderiliyor…</span> : retryRemaining > 0 ? `${retryRemaining} saniye sonra tekrar deneyin` : "Mesajı gönder"}
      </button>
      {challengeRequired && !turnstileToken ? <p className="text-center text-xs text-ink-muted">Güvenlik doğrulaması tamamlandığında gönderim butonu açılır.</p> : null}
    </form>
  );
}
