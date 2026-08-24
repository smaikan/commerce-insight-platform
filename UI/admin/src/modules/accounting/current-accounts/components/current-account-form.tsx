"use client";

import Link from "next/link";
import { useActionState, useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { saveCurrentAccountAction } from "@/modules/accounting/current-accounts/actions";
import type { CurrentAccount, CurrentAccountFormState } from "@/modules/accounting/current-accounts/types";
import { initialCurrentAccountFormState } from "@/modules/accounting/current-accounts/types";

export function CurrentAccountForm({ account }: { account?: CurrentAccount }) {
  const router = useRouter();
  const errorSummaryRef = useRef<HTMLDivElement>(null);
  const submitGuardRef = useRef(false);
  const action = saveCurrentAccountAction.bind(null, account?.id);
  const [state, formAction, pending] = useActionState<CurrentAccountFormState, FormData>(action, initialCurrentAccountFormState);
  const draft = state.draft;
  const formKey = draft ? JSON.stringify(draft) : "initial";

  useEffect(() => {
    if (state.redirectHref) {
      router.replace(state.redirectHref);
      router.refresh();
    }
  }, [router, state.redirectHref]);

  useEffect(() => {
    if (state.status === "error") errorSummaryRef.current?.focus();
    submitGuardRef.current = false;
  }, [state]);

  return (
    <form
      key={formKey}
      action={formAction}
      onSubmit={(event) => {
        if (submitGuardRef.current) event.preventDefault();
        else submitGuardRef.current = true;
      }}
      className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_20rem]"
    >
      {state.status === "error" ? (
        <div ref={errorSummaryRef} role="alert" tabIndex={-1} className="rounded-xl border border-danger/30 bg-red-50 px-4 py-3 text-sm text-red-900 outline-none focus:ring-2 focus:ring-danger/30 lg:col-span-2">
          <strong>{state.message}</strong>
          {state.fieldErrors ? (
            <ul className="mt-2 list-disc space-y-1 pl-5">
              {Object.entries(state.fieldErrors).flatMap(([key, messages]) => messages.map((message) => <li key={`${key}-${message}`}><a href={`#${fieldIdFromErrorKey(key)}`} className="underline underline-offset-2">{message}</a></li>))}
            </ul>
          ) : null}
          {state.retryAfter ? <span className="mt-1 block text-xs">Retry-After: {state.retryAfter}</span> : null}
          {state.traceId ? <span className="mt-1 block text-xs">Takip kodu: {state.traceId}</span> : null}
        </div>
      ) : null}

      <div className="space-y-4">
        <FormSection title="Cari kimliği" description="Belge ve ekstrelerde kullanılacak muhasebe master kaydı.">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field name="code" label="Cari kodu" required maxLength={50} value={draft?.code ?? account?.code} error={error(state, "code")} />
            <Field name="name" label="Cari unvanı" required maxLength={250} value={draft?.name ?? account?.name} error={error(state, "name")} />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field name="tradeName" label="Ticari unvan" maxLength={250} value={draft?.tradeName ?? account?.tradeName} error={error(state, "tradeName")} />
            <label className="text-sm font-medium text-foreground">
              Cari türü *
              <select id="type" name="type" required defaultValue={draft?.type ?? account?.type ?? 1} aria-invalid={Boolean(error(state, "type"))} aria-describedby={error(state, "type") ? "type-error" : undefined} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft">
                <option value="1">Müşteri</option>
                <option value="2">Tedarikçi</option>
                <option value="3">Müşteri ve tedarikçi</option>
              </select>
              {error(state, "type") ? <ErrorText id="type-error" text={error(state, "type")!} /> : null}
            </label>
          </div>
        </FormSection>

        <FormSection title="Vergi ve iletişim" description="Resmî ve operasyonel iletişim alanları.">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field name="taxNumber" label="Vergi numarası" maxLength={20} value={draft?.taxNumber ?? account?.taxNumber} error={error(state, "taxNumber")} />
            <Field name="taxOffice" label="Vergi dairesi" maxLength={100} value={draft?.taxOffice ?? account?.taxOffice} error={error(state, "taxOffice")} />
            <Field name="nationalIdentityNumber" label="T.C. kimlik numarası" maxLength={20} value={draft?.nationalIdentityNumber ?? account?.nationalIdentityNumber} error={error(state, "nationalIdentityNumber")} />
            <Field name="phoneNumber" label="Telefon" maxLength={30} value={draft?.phoneNumber ?? account?.phoneNumber} error={error(state, "phoneNumber")} />
            <Field name="email" label="E-posta" type="email" maxLength={320} value={draft?.email ?? account?.email} error={error(state, "email")} />
            <Field name="userId" label="Bağlı kullanıcı ID" value={draft?.userId ?? account?.userId} error={error(state, "userId")} help="Opsiyoneldir; yalnız müşteri içeren cari türlerinde bağlanabilir." />
          </div>
        </FormSection>

        <FormSection title="Adres" description="Cari master kaydına doğrudan bağlı adres.">
          <div className="grid gap-4 sm:grid-cols-2">
            <Field name="country" label="Ülke" maxLength={150} value={draft?.country ?? account?.country} error={error(state, "country")} />
            <Field name="city" label="Şehir" maxLength={150} value={draft?.city ?? account?.city} error={error(state, "city")} />
            <Field name="district" label="İlçe" maxLength={150} value={draft?.district ?? account?.district} error={error(state, "district")} />
            <Field name="neighborhood" label="Mahalle" maxLength={150} value={draft?.neighborhood ?? account?.neighborhood} error={error(state, "neighborhood")} />
            <Field name="postalCode" label="Posta kodu" maxLength={20} value={draft?.postalCode ?? account?.postalCode} error={error(state, "postalCode")} />
            <Field name="addressLine" label="Adres satırı" maxLength={500} value={draft?.addressLine ?? account?.addressLine} error={error(state, "addressLine")} />
          </div>
        </FormSection>
      </div>

      <aside className="space-y-4 lg:sticky lg:top-20">
        <section className="rounded-xl border border-border bg-surface p-4">
          <h2 className="text-sm font-semibold">Kullanılabilirlik</h2>
          {account ? (
            <label className="mt-3 flex cursor-pointer gap-3 rounded-lg border border-border bg-surface-subtle/50 p-3">
              <input name="isActive" type="checkbox" defaultChecked={draft?.isActive ?? account.isActive} className="mt-0.5 size-4 cursor-pointer" />
              <span><span className="block text-sm font-semibold">Aktif cari hesap</span><span className="mt-1 block text-xs leading-5 text-muted">Pasif hesap yeni satış ve alış belgelerinde kullanılamaz.</span></span>
            </label>
          ) : (
            <p className="mt-2 text-sm leading-6 text-muted">Yeni cari hesap aktif olarak oluşturulur. Gerekirse daha sonra düzenleme ekranından pasife alınabilir.</p>
          )}
        </section>
        <section className="rounded-xl border border-border bg-surface p-4">
          <h2 className="text-sm font-semibold">Kayıt ilkesi</h2>
          <p className="mt-2 text-sm leading-6 text-muted">Cari hareketler bu formdan düzenlenmez. Ekstre yalnız post edilmiş muhasebe hareketlerinden türetilir.</p>
        </section>
      </aside>

      <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end lg:col-span-2">
        <Link href={account ? `/accounting/current-accounts/${account.id}` : "/accounting/current-accounts"} className="inline-flex min-h-11 items-center justify-center rounded-lg border border-border-strong bg-surface px-4 text-sm font-semibold hover:bg-surface-subtle">Vazgeç</Link>
        <button type="submit" disabled={pending} aria-busy={pending} className="inline-flex min-h-11 cursor-pointer items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">
          {pending ? "Kaydediliyor…" : account ? "Değişiklikleri kaydet" : "Cari hesap oluştur"}
        </button>
      </div>
    </form>
  );
}

function FormSection({ title, description, children }: { title: string; description: string; children: React.ReactNode }) {
  return <section className="rounded-xl border border-border bg-surface p-4 sm:p-5"><div className="border-b border-border pb-4"><h2 className="text-base font-semibold">{title}</h2><p className="mt-1 text-sm text-muted">{description}</p></div><div className="mt-5 space-y-4">{children}</div></section>;
}

function Field({ name, label, value, maxLength, required = false, type = "text", error: message, help }: { name: string; label: string; value?: string | null; maxLength?: number; required?: boolean; type?: string; error?: string; help?: string }) {
  const errorId = `${name}-error`;
  const helpId = `${name}-help`;
  return (
    <label className="text-sm font-medium text-foreground">
      {label}{required ? " *" : ""}
      <input id={name} name={name} type={type} defaultValue={value ?? ""} required={required} maxLength={maxLength} aria-invalid={Boolean(message)} aria-describedby={message ? errorId : help ? helpId : undefined} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft" />
      {message ? <ErrorText id={errorId} text={message} /> : help ? <span id={helpId} className="mt-1 block text-xs font-normal text-muted">{help}</span> : null}
    </label>
  );
}

function ErrorText({ id, text }: { id: string; text: string }) {
  return <span id={id} className="mt-1 block text-xs font-semibold text-danger">{text}</span>;
}

function error(state: CurrentAccountFormState, key: string): string | undefined {
  return state.fieldErrors?.[key]?.[0] ?? state.fieldErrors?.[`Account.${key[0]?.toUpperCase()}${key.slice(1)}`]?.[0];
}

function fieldIdFromErrorKey(key: string): string {
  const field = key.split(".").at(-1) || key;
  return `${field[0]?.toLocaleLowerCase("tr-TR")}${field.slice(1)}`;
}
