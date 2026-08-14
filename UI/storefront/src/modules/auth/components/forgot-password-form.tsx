"use client";

import Link from "next/link";
import { useActionState } from "react";

import { forgotPasswordAction } from "@/modules/auth/password-reset-actions";
import { AuthField, SubmitButton } from "@/modules/auth/components/auth-controls";
import { initialForgotPasswordState } from "@/modules/auth/password-reset-state";

// Burada parola bağlantısı talebini bekleme, hata ve kullanıcı varlığını açıklamayan başarı durumlarıyla sunuyorum.
export function ForgotPasswordForm() {
  const [state, formAction] = useActionState(forgotPasswordAction, initialForgotPasswordState);

  if (state.status === "success") {
    return (
      <div role="status" className="border-l-4 border-success bg-surface-subtle px-4 py-4">
        <p className="text-sm leading-6 font-semibold text-ink">{state.message}</p>
        <p className="mt-2 text-xs leading-5 text-ink-muted">E-postanın ulaşması birkaç dakika sürebilir. Aynı adres için kısa süre içinde tekrar istek göndermek yeni bir e-posta oluşturmayabilir.</p>
        <div className="mt-5 flex flex-wrap gap-x-5 gap-y-3 text-sm font-bold">
          <Link href="/login" prefetch={false} className="focus-ring text-brand-700 underline underline-offset-4 hover:text-brand-950">Giriş ekranına dön</Link>
          <Link href="/forgot-password" prefetch={false} className="focus-ring text-ink-muted underline underline-offset-4 hover:text-ink">Başka bir e-posta kullan</Link>
        </div>
      </div>
    );
  }

  return (
    <>
      {state.status === "error" ? <p role="alert" className="mb-5 border-l-4 border-danger bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">{state.message}</p> : null}
      <form action={formAction} className="space-y-5" noValidate>
        <AuthField
          id="forgot-password-email"
          name="email"
          label="E-posta"
          type="email"
          autoComplete="email"
          required
          maxLength={320}
          defaultValue={state.values?.email}
          error={state.fieldErrors?.email}
        />
        <SubmitButton idleLabel="Sıfırlama bağlantısı gönder" pendingLabel="İstek gönderiliyor…" />
      </form>
      <p className="mt-7 text-center text-sm text-ink-muted">
        Parolanı hatırladın mı?{" "}
        <Link href="/login" prefetch={false} className="focus-ring font-black text-brand-700 underline decoration-brand-600/40 underline-offset-4 hover:text-brand-950">Giriş yap</Link>
      </p>
    </>
  );
}
