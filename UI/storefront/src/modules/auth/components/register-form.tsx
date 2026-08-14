"use client";

import Link from "next/link";
import { useActionState } from "react";

import { registerAction } from "@/modules/auth/actions";
import { AuthField, GoogleDevelopmentButton, PasswordField, SubmitButton } from "@/modules/auth/components/auth-controls";
import { initialAuthState } from "@/modules/auth/state";

// Burada kayıt formunda başarısız istekte yalnızca güvenli metin alanlarını koruyup şifreleri hiçbir zaman state'e taşımıyorum.
export function RegisterForm() {
  const [state, formAction] = useActionState(registerAction, initialAuthState);
  return (
    <>
      {state.status === "error" ? <p role="alert" className="mb-5 border-l-4 border-danger bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">{state.message}</p> : null}
      <form action={formAction} className="space-y-5" noValidate>
        <div className="grid gap-5 sm:grid-cols-2">
          <AuthField id="register-first-name" name="firstName" label="Ad" autoComplete="given-name" required maxLength={100} defaultValue={state.values?.firstName} error={state.fieldErrors?.firstName} />
          <AuthField id="register-last-name" name="lastName" label="Soyad" autoComplete="family-name" required maxLength={100} defaultValue={state.values?.lastName} error={state.fieldErrors?.lastName} />
        </div>
        <AuthField id="register-email" name="email" label="E-posta" type="email" autoComplete="email" required maxLength={320} defaultValue={state.values?.email} error={state.fieldErrors?.email} />
        <AuthField id="register-phone" name="phoneNumber" label="Telefon (isteğe bağlı)" type="tel" autoComplete="tel" maxLength={30} defaultValue={state.values?.phoneNumber} error={state.fieldErrors?.phoneNumber} />
        <div className="grid gap-5 sm:grid-cols-2">
          <PasswordField key={`password-${state.revision}`} id="register-password" name="password" label="Şifre" autoComplete="new-password" hint="En az 6, en fazla 128 karakter." error={state.fieldErrors?.password} />
          <PasswordField key={`confirm-${state.revision}`} id="register-confirm-password" name="confirmPassword" label="Şifre tekrarı" autoComplete="new-password" error={state.fieldErrors?.confirmPassword} />
        </div>
        <p className="text-xs leading-5 text-ink-muted">Hesap oluşturarak kişisel verilerinin <Link href="/privacy-policy" prefetch={false} className="focus-ring font-bold text-brand-700 underline underline-offset-2">Gizlilik Politikası</Link> kapsamında işlenmesini kabul edersin.</p>
        <SubmitButton idleLabel="Hesap oluştur" pendingLabel="Hesap oluşturuluyor…" />
      </form>

      <div className="my-6 flex items-center gap-3" aria-hidden="true"><span className="h-px flex-1 bg-line" /><span className="text-[0.6875rem] font-bold tracking-widest text-ink-muted uppercase">veya</span><span className="h-px flex-1 bg-line" /></div>
      <GoogleDevelopmentButton />
      <p className="mt-7 text-center text-sm text-ink-muted">Zaten hesabın var mı? <Link href="/login" prefetch={false} className="focus-ring font-black text-brand-700 underline decoration-brand-600/40 underline-offset-4 hover:text-brand-950">Giriş yap</Link></p>
    </>
  );
}
