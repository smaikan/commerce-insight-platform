"use client";

import { useActionState, useRef } from "react";
import { revokeAllSessionsAction, revokeSessionAction } from "@/modules/settings/actions";
import { initialSettingsActionState } from "@/modules/settings/types";

// Burada tek cihaz oturumunu yinelenen tıklamayı engelleyerek sonlandırıyorum.
export function RevokeSessionButton({ sessionId, deviceName }: { sessionId: string; deviceName: string }) {
  const [state, formAction, pending] = useActionState(revokeSessionAction.bind(null, sessionId), initialSettingsActionState);
  return (
    <div className="flex flex-col items-end gap-1.5">
      <form action={formAction}>
        <button type="submit" disabled={pending} aria-label={`${deviceName} oturumunu sonlandır`} className="min-h-9 cursor-pointer rounded-lg border border-border-strong bg-surface-strong px-3 text-xs font-semibold text-foreground transition-colors hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Sonlandırılıyor…" : "Oturumu sonlandır"}</button>
      </form>
      {state.status !== "idle" ? <p role="status" className={`max-w-52 text-right text-xs ${state.status === "error" ? "text-danger" : "text-success"}`}>{state.message}</p> : null}
    </div>
  );
}

// Burada bütün cihazları etkileyen çıkış işlemini sonucu açıkça anlatan modal onayıyla koruyorum.
export function RevokeAllSessionsDialog({ sessionCount }: { sessionCount: number }) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [state, formAction, pending] = useActionState(revokeAllSessionsAction, initialSettingsActionState);
  return (
    <>
      <button type="button" onClick={() => dialogRef.current?.showModal()} className="inline-flex min-h-10 cursor-pointer items-center justify-center rounded-lg border border-danger/40 bg-surface-strong px-4 text-sm font-semibold text-danger transition-colors hover:bg-danger/5">Tüm oturumları kapat</button>
      <dialog ref={dialogRef} aria-labelledby="revoke-all-title" aria-describedby="revoke-all-description" className="m-auto w-[min(92vw,30rem)] rounded-xl border border-border bg-surface p-0 text-foreground shadow-xl backdrop:bg-foreground/45">
        <form action={formAction} className="p-5">
          <div className="flex size-10 items-center justify-center rounded-lg bg-danger/10 text-danger" aria-hidden="true"><WarningIcon /></div>
          <h2 id="revoke-all-title" className="mt-4 text-lg font-semibold">Tüm oturumlar kapatılsın mı?</h2>
          <p id="revoke-all-description" className="mt-2 text-sm leading-6 text-muted">Listede {sessionCount} aktif cihaz oturumu görünüyor. Bu cihazdaki erişim dahil hesabın bütün oturumları sonlandırılacak; işlemden sonra yeniden giriş gerekecek.</p>
          {state.status === "error" ? <p role="alert" className="mt-3 rounded-lg border border-danger/30 bg-danger/10 px-3 py-2 text-sm text-danger">{state.message}</p> : null}
          <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
            <button type="button" onClick={() => dialogRef.current?.close()} disabled={pending} className="min-h-10 cursor-pointer rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground transition-colors hover:bg-surface-subtle disabled:cursor-not-allowed">Vazgeç</button>
            <button type="submit" disabled={pending} className="min-h-10 cursor-pointer rounded-lg bg-danger px-4 text-sm font-semibold text-white transition-colors hover:brightness-95 disabled:cursor-not-allowed disabled:opacity-60">{pending ? "Oturumlar kapatılıyor…" : "Tümünü kapat ve çıkış yap"}</button>
          </div>
        </form>
      </dialog>
    </>
  );
}

// Burada sonuçları yalnızca renkle anlatmamak için uyarı simgesini metinli onayla birlikte kullanıyorum.
function WarningIcon() {
  return <svg viewBox="0 0 20 20" className="size-5 fill-none stroke-current" strokeWidth="1.8"><path d="M10 3 18 17H2L10 3Z" strokeLinejoin="round" /><path d="M10 8v4m0 2v.1" strokeLinecap="round" /></svg>;
}
