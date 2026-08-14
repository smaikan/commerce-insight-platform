"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { useActionState } from "react";

import { PasswordField, SubmitButton } from "@/modules/auth/components/auth-controls";
import { resetPasswordAction } from "@/modules/auth/password-reset-actions";
import { consumeResetToken } from "@/modules/auth/password-reset-fragment";
import { redirectAfterPasswordReset } from "@/modules/auth/password-reset-navigation";
import { initialResetPasswordState } from "@/modules/auth/password-reset-state";

type ResetLinkState =
  | { status: "checking" }
  | { status: "invalid" }
  | { status: "ready"; token: string };

const INVALID_RESET_LINK = "Bu parola sıfırlama bağlantısı geçersiz, kullanılmış veya süresi dolmuş. Yeni bir bağlantı isteyin.";

// Burada tokenı fragmenttan yalnız bir kez okuyup adres çubuğundan temizleyerek geçici React belleğinde tutuyorum.
export function ResetPasswordForm() {
  const fragmentRead = useRef(false);
  const [linkState, setLinkState] = useState<ResetLinkState>({ status: "checking" });

  useEffect(() => {
    if (fragmentRead.current) return;
    fragmentRead.current = true;

    const token = consumeResetToken(window.location, window.history);
    setLinkState(token ? { status: "ready", token } : { status: "invalid" });
  }, []);

  if (linkState.status === "checking") {
    return <p role="status" className="border-l-4 border-brand-600 bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">Bağlantı güvenle kontrol ediliyor…</p>;
  }

  if (linkState.status === "invalid") {
    return <InvalidResetLink message={INVALID_RESET_LINK} />;
  }

  return <ReadyResetPasswordForm token={linkState.token} />;
}

// Burada geçerli fragment tokenı bulunduğunda yalnız yeni parola alanlarını ve güvenli işlem durumlarını gösteriyorum.
function ReadyResetPasswordForm({ token }: { token: string }) {
  const actionWithToken = resetPasswordAction.bind(null, token);
  const [state, formAction] = useActionState(actionWithToken, initialResetPasswordState);

  useEffect(() => {
    if (state.status === "success") {
      redirectAfterPasswordReset(window.location);
    }
  }, [state.status]);

  if (state.status === "invalid-link") {
    return <InvalidResetLink message={state.message || INVALID_RESET_LINK} />;
  }

  if (state.status === "success") {
    return <p role="status" className="border-l-4 border-success bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">Parolanız değiştirildi. Giriş ekranına yönlendiriliyorsunuz…</p>;
  }

  return (
    <>
      {state.status === "error" ? <p role="alert" className="mb-5 border-l-4 border-danger bg-surface-subtle px-4 py-3 text-sm font-semibold text-ink">{state.message}</p> : null}
      <form action={formAction} className="space-y-5" noValidate>
        <PasswordField
          key={`new-${state.revision}`}
          id="reset-new-password"
          name="newPassword"
          label="Yeni parola"
          autoComplete="new-password"
          hint="6–128 karakter kullanın. Parolanızı başka hesaplarla paylaşmayın."
          error={state.fieldErrors?.newPassword}
        />
        <PasswordField
          key={`confirm-${state.revision}`}
          id="reset-confirm-password"
          name="confirmPassword"
          label="Yeni parola tekrar"
          autoComplete="new-password"
          error={state.fieldErrors?.confirmPassword}
        />
        <SubmitButton idleLabel="Parolamı değiştir" pendingLabel="Parola değiştiriliyor…" />
      </form>
    </>
  );
}

// Burada kullanılamayan bağlantıyı hassas neden ayrımı yapmadan tek bir güvenli yenileme yoluyla sunuyorum.
function InvalidResetLink({ message }: { message: string }) {
  return (
    <div role="alert" className="border-l-4 border-danger bg-surface-subtle px-4 py-4">
      <p className="text-sm leading-6 font-semibold text-ink">{message}</p>
      <Link href="/forgot-password" prefetch={false} className="focus-ring mt-5 inline-flex min-h-11 items-center bg-brand-950 px-5 text-sm font-black text-white hover:bg-brand-700">Yeni bağlantı iste</Link>
    </div>
  );
}
