"use client";

import { useActionState, useEffect, useRef } from "react";
import { changeEmailAction, changePasswordAction, updateProfileAction } from "@/modules/settings/actions";
import { getSettingsFieldError, SettingsField, settingsInputClass } from "@/modules/settings/components/form-controls";
import { formatSettingsDate } from "@/modules/settings/presentation";
import { initialSettingsActionState, type AccountUser, type SettingsActionState } from "@/modules/settings/types";

// Burada profil, e-posta ve parola işlerini ayrı güvenlik sınırlarında tek hesap ekranında topluyorum.
export function AccountSettings({ user }: { user: AccountUser }) {
  return (
    <div className="grid gap-4 xl:grid-cols-[minmax(0,1fr)_280px] xl:items-start">
      <div className="space-y-4">
        <ProfileForm user={user} />
        <EmailForm email={user.email} />
        <PasswordForm />
      </div>
      <aside className="space-y-4 xl:sticky xl:top-4">
        <section className="rounded-xl border border-border bg-surface p-4">
          <div className="flex items-center gap-3">
            <span className="flex size-10 items-center justify-center rounded-lg bg-primary-soft text-sm font-bold text-primary-hover" aria-hidden="true">{initials(user)}</span>
            <div className="min-w-0"><h2 className="truncate text-sm font-semibold text-foreground">{user.firstName} {user.lastName}</h2><p className="truncate text-xs text-muted">{user.email}</p></div>
          </div>
          <dl className="mt-4 divide-y divide-border text-sm">
            <DetailRow label="Hesap kimliği" value={user.id} mono />
            <DetailRow label="Rol" value={user.role === 2 ? "Yönetici" : "Müşteri"} />
            <DetailRow label="Durum" value={user.status === 1 ? "Aktif" : user.status === 2 ? "Pasif" : "Askıya alınmış"} />
            <DetailRow label="Son giriş" value={formatSettingsDate(user.lastLoginAt)} />
            <DetailRow label="Hesap açılışı" value={formatSettingsDate(user.createdAt)} />
          </dl>
        </section>
        <section className="rounded-xl border border-primary/20 bg-primary/5 p-4 text-sm text-muted">
          <h2 className="font-semibold text-foreground">Hesap güvenliği</h2>
          <p className="mt-1 leading-5">E-posta ve parola değişiklikleri mevcut parolanızla doğrulanır. Parolanız panel tarafından okunmaz veya saklanmaz.</p>
        </section>
      </aside>
    </div>
  );
}

// Burada ad, soyad ve telefon alanlarını kişisel profil endpoint'ine bağlıyorum.
function ProfileForm({ user }: { user: AccountUser }) {
  const [state, formAction, pending] = useActionState(updateProfileAction, initialSettingsActionState);
  return (
    <form action={formAction} className="overflow-hidden rounded-xl border border-border bg-surface">
      <SectionHeader title="Profil bilgileri" description="Panelde hesabınızı tanımlayan temel iletişim bilgileri." />
      <div className="grid gap-4 p-4 sm:grid-cols-2 sm:p-5">
        <SettingsField label="Ad" htmlFor="account-first-name" error={getSettingsFieldError(state, "firstName")}><input id="account-first-name" name="firstName" required maxLength={100} autoComplete="given-name" defaultValue={user.firstName} className={settingsInputClass} /></SettingsField>
        <SettingsField label="Soyad" htmlFor="account-last-name" error={getSettingsFieldError(state, "lastName")}><input id="account-last-name" name="lastName" required maxLength={100} autoComplete="family-name" defaultValue={user.lastName} className={settingsInputClass} /></SettingsField>
        <SettingsField label="Telefon" htmlFor="account-phone" error={getSettingsFieldError(state, "phoneNumber")} className="sm:col-span-2"><input id="account-phone" name="phoneNumber" type="tel" maxLength={30} autoComplete="tel" defaultValue={user.phoneNumber ?? ""} placeholder="Opsiyonel" className={settingsInputClass} /></SettingsField>
      </div>
      <FormFooter state={state} pending={pending} submitLabel="Profili kaydet" />
    </form>
  );
}

// Burada e-posta değişikliğini mevcut parola doğrulamasıyla ayrı formda tutuyorum.
function EmailForm({ email }: { email: string }) {
  const [state, formAction, pending] = useActionState(changeEmailAction, initialSettingsActionState);
  const formRef = useRef<HTMLFormElement>(null);
  useEffect(() => { if (state.status === "success") formRef.current?.reset(); }, [state.status]);
  return (
    <form ref={formRef} action={formAction} className="overflow-hidden rounded-xl border border-border bg-surface">
      <SectionHeader title="E-posta adresi" description={`Mevcut adres: ${email}`} />
      <div className="grid gap-4 p-4 sm:grid-cols-2 sm:p-5">
        <SettingsField label="Yeni e-posta" htmlFor="account-new-email" error={getSettingsFieldError(state, "newEmail")}><input id="account-new-email" name="newEmail" type="email" required maxLength={320} autoComplete="email" placeholder="yeni@adres.com" className={settingsInputClass} /></SettingsField>
        <SettingsField label="Mevcut parola" htmlFor="account-email-password" error={getSettingsFieldError(state, "currentPassword")}><input id="account-email-password" name="currentPassword" type="password" required maxLength={128} autoComplete="current-password" className={settingsInputClass} /></SettingsField>
      </div>
      <FormFooter state={state} pending={pending} submitLabel="E-postayı değiştir" />
    </form>
  );
}

// Burada parola değişikliğini doğrulama tekrarıyla ayrı ve güvenli bir formda tutuyorum.
function PasswordForm() {
  const [state, formAction, pending] = useActionState(changePasswordAction, initialSettingsActionState);
  const formRef = useRef<HTMLFormElement>(null);
  useEffect(() => { if (state.status === "success") formRef.current?.reset(); }, [state.status]);
  return (
    <form ref={formRef} action={formAction} className="overflow-hidden rounded-xl border border-border bg-surface">
      <SectionHeader title="Parola" description="Yeni parolanız 6–128 karakter olmalı ve mevcut parolanızdan farklı olmalıdır." />
      <div className="grid gap-4 p-4 sm:grid-cols-2 sm:p-5">
        <SettingsField label="Mevcut parola" htmlFor="account-current-password" error={getSettingsFieldError(state, "currentPassword")} className="sm:col-span-2"><input id="account-current-password" name="currentPassword" type="password" required maxLength={128} autoComplete="current-password" className={settingsInputClass} /></SettingsField>
        <SettingsField label="Yeni parola" htmlFor="account-new-password" error={getSettingsFieldError(state, "newPassword")}><input id="account-new-password" name="newPassword" type="password" required minLength={6} maxLength={128} autoComplete="new-password" className={settingsInputClass} /></SettingsField>
        <SettingsField label="Yeni parola tekrarı" htmlFor="account-confirm-password" error={getSettingsFieldError(state, "confirmPassword")}><input id="account-confirm-password" name="confirmPassword" type="password" required minLength={6} maxLength={128} autoComplete="new-password" className={settingsInputClass} /></SettingsField>
      </div>
      <FormFooter state={state} pending={pending} submitLabel="Parolayı değiştir" />
    </form>
  );
}

// Burada her hesap bölümüne tutarlı ve kompakt bir başlık veriyorum.
function SectionHeader({ title, description }: { title: string; description: string }) {
  return <header className="border-b border-border bg-surface-subtle/60 px-4 py-3.5 sm:px-5"><h2 className="text-base font-semibold text-foreground">{title}</h2><p className="mt-1 text-sm leading-5 text-muted">{description}</p></header>;
}

// Burada form sonucunu yerleşimi oynatmadan eylem alanında duyuruyorum.
function FormFooter({ state, pending, submitLabel }: { state: SettingsActionState; pending: boolean; submitLabel: string }) {
  return (
    <div className="flex flex-col gap-3 border-t border-border bg-surface-subtle/30 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5">
      <div aria-live="polite" className="min-h-5 text-sm">
        {state.status !== "idle" ? <p className={state.status === "success" ? "font-medium text-success" : "text-danger"}>{state.message}{state.traceId ? <span className="ml-2 font-mono text-xs">Takip: {state.traceId}</span> : null}</p> : null}
      </div>
      <button type="submit" disabled={pending} className="inline-flex min-h-10 shrink-0 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Kaydediliyor…" : submitLabel}</button>
    </div>
  );
}

// Burada hesap özetindeki etiket ve değeri taranabilir bir satırda gösteriyorum.
function DetailRow({ label, value, mono = false }: { label: string; value: string; mono?: boolean }) {
  return <div className="flex items-start justify-between gap-3 py-2.5"><dt className="text-muted">{label}</dt><dd className={`max-w-[60%] break-words text-right font-medium text-foreground ${mono ? "font-mono text-xs" : ""}`}>{value}</dd></div>;
}

// Burada avatar yerine kalıcı marka üretmeden kullanıcının baş harflerini gösteriyorum.
function initials(user: AccountUser): string {
  return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toLocaleUpperCase("tr-TR");
}
