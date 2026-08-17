"use client";

import { useActionState, useEffect, useState, useTransition } from "react";
import { useRouter } from "next/navigation";

import {
  changePasswordAction,
  logoutAllSessionsAction,
  revokeSessionAction,
} from "@/modules/account/actions";
import { ActionFeedback } from "@/modules/account/components/action-feedback";
import { AccountPageHeader } from "@/modules/account/components/account-page-header";
import {
  INITIAL_ACCOUNT_ACTION_STATE,
  type AccountActionState,
  type AccountSession,
} from "@/modules/account/contracts";

// Burada parola değişimi ile oturum kapatma işlemlerini aynı sayfada, ayrı ve anlaşılır güvenlik bölgeleri olarak sunuyorum.
export function AccountSecurityView({ sessions }: { sessions: AccountSession[] }) {
  return (
    <section>
      <AccountPageHeader
        eyebrow="Hesap güvenliği"
        title="Güvenlik"
        description="Parolanızı değiştirin ve hesabınıza erişimi olan cihazlardaki oturumları yönetin."
      />
      <div className="mt-7 grid gap-7 xl:grid-cols-[minmax(0,1fr)_minmax(18rem,0.72fr)]">
        <PasswordPanel />
        <SessionPanel sessions={sessions} />
      </div>
    </section>
  );
}

// Burada parola değişimi sonrasında API'nin geçersiz kıldığı oturumla tekrar giriş ekranına yönlendiriyorum.
function PasswordPanel() {
  const router = useRouter();
  const [state, action, pending] = useActionState(changePasswordAction, INITIAL_ACCOUNT_ACTION_STATE);

  useEffect(() => {
    if (state.status === "success") router.replace("/login?loggedOut=1");
  }, [router, state.status]);

  return (
    <section className="border border-line bg-surface" aria-labelledby="password-title">
      <div className="border-b border-line px-5 py-4 sm:px-6">
        <h2 id="password-title" className="text-lg font-black text-ink">Parolayı değiştir</h2>
        <p className="mt-1 text-sm leading-6 text-ink-muted">İşlem tamamlandığında güvenliğiniz için tüm cihazlarda yeniden giriş yapmanız gerekir.</p>
      </div>
      <form action={action} className="space-y-5 px-5 py-5 sm:px-6" noValidate>
        <PasswordField label="Mevcut parola" name="currentPassword" autoComplete="current-password" error={state.fieldErrors?.currentPassword} />
        <PasswordField label="Yeni parola" name="newPassword" autoComplete="new-password" error={state.fieldErrors?.newPassword} />
        <PasswordField label="Yeni parola (tekrar)" name="confirmPassword" autoComplete="new-password" error={state.fieldErrors?.confirmPassword} />
        <ActionFeedback state={state} />
        <div className="flex justify-end">
          <button type="submit" disabled={pending} aria-busy={pending} className="focus-ring min-h-11 bg-brand-950 px-5 text-sm font-bold text-white hover:bg-brand-700 disabled:cursor-wait disabled:bg-ink-muted">
            {pending ? "Parola değiştiriliyor…" : "Parolayı değiştir"}
          </button>
        </div>
      </form>
    </section>
  );
}

// Burada parolaların erişilebilir etiket, otomatik doldurma ve API alan hatası ilişkisini tek kontrolde koruyorum.
function PasswordField({ label, name, autoComplete, error }: { label: string; name: string; autoComplete: string; error?: string }) {
  const errorId = `${name}-error`;
  return (
    <label className="block text-sm font-bold text-ink" htmlFor={name}>
      {label}
      <input id={name} name={name} type="password" required autoComplete={autoComplete} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined} className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm font-normal text-ink aria-[invalid=true]:border-danger" />
      {error ? <span id={errorId} className="mt-1 block text-xs font-semibold text-danger">{error}</span> : null}
    </label>
  );
}

// Burada API'nin verdiği cihaz ve zaman özetleriyle seçili veya tüm oturum kapatma kararını açık onayla sunuyorum.
function SessionPanel({ sessions }: { sessions: AccountSession[] }) {
  const router = useRouter();
  const [state, setState] = useState<AccountActionState>(INITIAL_ACCOUNT_ACTION_STATE);
  const [confirmAll, setConfirmAll] = useState(false);
  const [pending, startTransition] = useTransition();

  function run(operation: () => Promise<AccountActionState>) {
    startTransition(() => void operation().then((result) => {
      setState(result);
      if (result.status === "success") router.refresh();
    }));
  }

  useEffect(() => {
    if (state.status === "success" && state.message?.includes("Tüm cihaz")) router.replace("/login?loggedOut=1");
  }, [router, state]);

  return (
    <section className="border border-line bg-surface" aria-labelledby="sessions-title">
      <div className="border-b border-line px-5 py-4 sm:px-6">
        <h2 id="sessions-title" className="text-lg font-black text-ink">Aktif oturumlar</h2>
        <p className="mt-1 text-sm leading-6 text-ink-muted">Yalnız tanıdığınız cihazların erişimine izin verin.</p>
      </div>
      <div className="px-5 py-5 sm:px-6">
        {sessions.length ? (
          <ul className="divide-y divide-line border-y border-line">
            {sessions.map((session) => (
              <li key={session.id} className="py-4 first:pt-4">
                <p className="break-words text-sm font-bold text-ink">{session.deviceName || "Bilinmeyen cihaz"}</p>
                <p className="mt-1 text-xs leading-5 text-ink-muted">Başlangıç: {formatDate(session.createdAt)}{session.createdByIp ? ` · ${session.createdByIp}` : ""}</p>
                <p className="text-xs leading-5 text-ink-muted">Bitiş: {formatDate(session.expiresAt)}</p>
                <button type="button" disabled={pending} onClick={() => run(() => revokeSessionAction(session.id))} className="focus-ring mt-3 min-h-10 border border-line px-3 text-xs font-bold text-danger hover:bg-danger/5 disabled:cursor-wait">Bu oturumu kapat</button>
              </li>
            ))}
          </ul>
        ) : <p className="border border-dashed border-line px-4 py-6 text-sm leading-6 text-ink-muted">Görüntülenecek aktif oturum bulunmuyor.</p>}

        <div className="mt-5 border-t border-line pt-5">
          {!confirmAll ? <button type="button" disabled={pending} onClick={() => setConfirmAll(true)} className="focus-ring min-h-11 border border-danger/40 px-4 text-sm font-bold text-danger hover:bg-danger/5 disabled:cursor-wait">Tüm cihazlardan çıkış yap</button> : (
            <div role="group" aria-label="Tüm oturumları kapatma onayı">
              <p className="text-sm font-semibold text-ink">Bu işlem bu cihaz dahil tüm oturumları kapatır. Devam edilsin mi?</p>
              <div className="mt-3 flex flex-wrap gap-2">
                <button type="button" disabled={pending} onClick={() => run(logoutAllSessionsAction)} className="focus-ring min-h-11 bg-danger px-4 text-sm font-bold text-white disabled:cursor-wait">Evet, tümünü kapat</button>
                <button type="button" disabled={pending} onClick={() => setConfirmAll(false)} className="focus-ring min-h-11 border border-line px-4 text-sm font-bold text-ink">Vazgeç</button>
              </div>
            </div>
          )}
          <ActionFeedback state={state} />
        </div>
      </div>
    </section>
  );
}

// Burada UTC zamanlarını kullanıcının yerel Türkçe tarih biçiminde gösteriyorum.
function formatDate(value: string) {
  return new Intl.DateTimeFormat("tr-TR", { dateStyle: "medium", timeStyle: "short" }).format(new Date(value));
}
