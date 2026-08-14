"use client";

import Link from "next/link";
import { useActionState } from "react";

import { loginAction } from "@/modules/auth/actions";
import { AuthField, GoogleDevelopmentButton, PasswordField, SubmitButton } from "@/modules/auth/components/auth-controls";
import { initialAuthState } from "@/modules/auth/state";

// Burada login formunun yalnızca hata ve bekleme durumunu istemcide tutup kimlik bilgilerini doğrudan Server Action'a gönderiyorum.
export function LoginForm({ returnTo, registered, autoLoginFailed, loggedOut, passwordReset }: { returnTo: string; registered: boolean; autoLoginFailed: boolean; loggedOut: boolean; passwordReset: boolean }) {
  const [state, formAction] = useActionState(loginAction, initialAuthState);
  return (
    <>
      {registered ? (
        <p role="status" className="mb-5 border-l-4 border-success bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">
          {autoLoginFailed
            ? "Hesabın oluşturuldu ancak otomatik giriş tamamlanamadı. Bilgilerinle giriş yapabilirsin."
            : "Hesabın oluşturuldu. Şimdi güvenle giriş yapabilirsin."}
        </p>
      ) : null}
      {loggedOut ? <p role="status" className="mb-5 border-l-4 border-success bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">Oturumun güvenle kapatıldı.</p> : null}
      {passwordReset ? <p role="status" className="mb-5 border-l-4 border-success bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">Parolanız değiştirildi. Yeni parolanızla giriş yapabilirsiniz.</p> : null}
      {state.status === "error" ? <p role="alert" className="mb-5 border-l-4 border-danger bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">{state.message}</p> : null}

      <form action={formAction} className="space-y-5" noValidate>
        <input type="hidden" name="returnTo" value={returnTo} />
        <AuthField id="login-email" name="email" label="E-posta" type="email" autoComplete="email" required maxLength={320} defaultValue={state.values?.email} error={state.fieldErrors?.email} />
        <PasswordField key={state.revision} id="login-password" name="password" label="Şifre" autoComplete="current-password" error={state.fieldErrors?.password} />
        <div className="-mt-2 flex justify-end">
          <Link href="/forgot-password" prefetch={false} className="focus-ring min-h-11 py-3 text-sm font-bold text-brand-700 underline decoration-brand-600/40 underline-offset-4 hover:text-brand-950">Parolamı unuttum</Link>
        </div>
        <SubmitButton idleLabel="Giriş yap" pendingLabel="Giriş yapılıyor…" />
      </form>

      <div className="my-6 flex items-center gap-3" aria-hidden="true"><span className="h-px flex-1 bg-line" /><span className="text-[0.6875rem] font-bold tracking-widest text-ink-muted uppercase">veya</span><span className="h-px flex-1 bg-line" /></div>
      <GoogleDevelopmentButton />
      <p className="mt-7 text-center text-sm text-ink-muted">Henüz hesabın yok mu? <Link href="/register" prefetch={false} className="focus-ring font-black text-brand-700 underline decoration-brand-600/40 underline-offset-4 hover:text-brand-950">Hesap oluştur</Link></p>
    </>
  );
}
