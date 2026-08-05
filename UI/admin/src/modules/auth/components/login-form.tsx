"use client";

import { useActionState, useEffect, useRef } from "react";
import { useFormStatus } from "react-dom";
import { loginAction } from "@/modules/auth/actions";
import { initialLoginActionState } from "@/modules/auth/types";

// Burada login formunun hata odağını, parola temizliğini ve bekleyen gönderim durumunu küçük bir Client Component içinde yönetiyorum.
export function LoginForm({ returnTo, notice }: { returnTo: string; notice?: string }) {
  const [state, formAction] = useActionState(loginAction, initialLoginActionState);
  const alertRef = useRef<HTMLDivElement>(null);
  const passwordRef = useRef<HTMLInputElement>(null);

  // Burada başarısız girişten sonra parolayı DOM'da bırakmadan hatayı klavye ve ekran okuyucu odağına taşıyorum.
  useEffect(() => {
    if (state.status !== "error") return;
    if (passwordRef.current) passwordRef.current.value = "";
    alertRef.current?.focus();
  }, [state]);

  const emailError = state.fieldErrors?.email;
  const passwordError = state.fieldErrors?.password;

  return (
    <form action={formAction} className="mt-6 space-y-4" noValidate>
      <input type="hidden" name="returnTo" value={returnTo} />

      {notice && state.status === "idle" ? (
        <div className="rounded-lg border border-border-strong bg-surface-subtle px-3 py-2 text-sm leading-6 text-foreground" role="status">
          {notice}
        </div>
      ) : null}

      {state.status === "error" ? (
        <div
          ref={alertRef}
          tabIndex={-1}
          className="rounded-lg border border-red-300 bg-red-50 px-3 py-2 text-sm leading-6 text-red-900"
          role="alert"
        >
          <p className="font-semibold">Giriş yapılamadı</p>
          <p>{state.message}</p>
          {state.traceId ? <p className="mt-1 text-xs">Takip kodu: {state.traceId}</p> : null}
        </div>
      ) : null}

      <div>
        <label htmlFor="email" className="block text-sm font-medium text-foreground">E-posta adresi</label>
        <input
          id="email"
          name="email"
          type="email"
          autoComplete="username"
          inputMode="email"
          defaultValue={state.email || ""}
          maxLength={320}
          required
          autoFocus
          aria-invalid={Boolean(emailError)}
          aria-describedby={emailError ? "email-error" : undefined}
          className="mt-1.5 min-h-11 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
        />
        {emailError ? <p id="email-error" className="mt-1 text-xs font-semibold text-danger">{emailError.join(" ")}</p> : null}
      </div>

      <div>
        <label htmlFor="password" className="block text-sm font-medium text-foreground">Parola</label>
        <input
          ref={passwordRef}
          id="password"
          name="password"
          type="password"
          autoComplete="current-password"
          maxLength={128}
          required
          aria-invalid={Boolean(passwordError)}
          aria-describedby={passwordError ? "password-error" : undefined}
          className="mt-1.5 min-h-11 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
        />
        {passwordError ? <p id="password-error" className="mt-1 text-xs font-semibold text-danger">{passwordError.join(" ")}</p> : null}
      </div>

      <SubmitButton />
    </form>
  );
}

// Burada yinelenen login niyetini engelleyip bekleme durumunu görünür ve erişilebilir metinle bildiriyorum.
function SubmitButton() {
  const { pending } = useFormStatus();
  return (
    <button
      type="submit"
      disabled={pending}
      aria-disabled={pending}
      className="inline-flex min-h-11 w-full items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white transition-colors hover:bg-primary-hover disabled:cursor-wait disabled:opacity-70 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
    >
      {pending ? "Giriş yapılıyor…" : "Giriş yap"}
    </button>
  );
}
