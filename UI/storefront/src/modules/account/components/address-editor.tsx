"use client";

import { useActionState, useState } from "react";

import { saveAddressAction } from "@/modules/account/actions";
import { ActionFeedback } from "@/modules/account/components/action-feedback";
import { INITIAL_ACCOUNT_ACTION_STATE, type AccountAddress } from "@/modules/account/contracts";
import { TurkiyeAddressFields } from "@/components/storefront/turkiye-address-fields";
import { PhoneField } from "@/components/storefront/phone-field";

// Burada adres ekleme ve düzenleme formunu yalnız gerektiğinde açılan küçük bir client sınırında tutuyorum.
export function AddressEditor({ address, primary = false }: { address?: AccountAddress; primary?: boolean }) {
  const [open, setOpen] = useState(false);
  const saveAction = saveAddressAction.bind(null, address?.id ?? null);
  const [state, action, pending] = useActionState(saveAction, INITIAL_ACCOUNT_ACTION_STATE);

  return (
    <div className={address ? "" : "w-full"}>
      <button
        type="button"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        className={`focus-ring inline-flex min-h-11 items-center justify-center px-4 text-sm font-bold ${primary ? "ml-auto flex bg-brand-950 text-white hover:bg-brand-700" : "border border-line text-brand-700 hover:bg-surface-subtle"}`}
      >
        {open ? "Formu kapat" : address ? "Düzenle" : "Yeni adres ekle"}
      </button>

      {open ? (
        <form action={action} className="mt-4 border border-line bg-surface-subtle p-4 sm:p-5" noValidate>
          <div className="grid gap-4 sm:grid-cols-2">
            <AddressSelect defaultValue={String(address?.type ?? 0)} error={state.fieldErrors?.type} />
            <AddressField label="Adres başlığı" name="title" defaultValue={address?.title} error={state.fieldErrors?.title} autoComplete="off" placeholder="Ev, İş" />
            <AddressField label="Ad" name="firstName" defaultValue={address?.firstName} error={state.fieldErrors?.firstName} autoComplete="given-name" />
            <AddressField label="Soyad" name="lastName" defaultValue={address?.lastName} error={state.fieldErrors?.lastName} autoComplete="family-name" />
            <PhoneField variant="account" label="Telefon" name="phoneNumber" defaultValue={address?.phoneNumber} error={state.fieldErrors?.phoneNumber} autoComplete="tel" required />
            <AddressField label="Posta kodu (isteğe bağlı)" name="postalCode" defaultValue={address?.postalCode ?? ""} error={state.fieldErrors?.postalCode} autoComplete="postal-code" required={false} />
            <TurkiyeAddressFields prefix="" errors={state.fieldErrors} defaultCity={address?.city} defaultDistrict={address?.district} defaultNeighborhood={address?.neighborhood} variant="account" />
            <label className="block text-xs font-bold text-ink sm:col-span-2">
              Açık adres
              <textarea name="fullAddress" defaultValue={address?.fullAddress} required autoComplete="street-address" rows={3} aria-invalid={Boolean(state.fieldErrors?.fullAddress)} aria-describedby={state.fieldErrors?.fullAddress ? "fullAddress-error" : undefined} className="focus-ring mt-2 w-full resize-y border border-line bg-surface px-3 py-2.5 text-sm font-normal text-ink" />
              {state.fieldErrors?.fullAddress ? <span id="fullAddress-error" className="mt-1 block text-xs font-semibold text-danger">{state.fieldErrors.fullAddress}</span> : null}
            </label>
          </div>

          <label className="mt-4 flex min-h-11 items-center gap-3 text-sm font-semibold text-ink">
            <input type="checkbox" name="isDefault" defaultChecked={address?.isDefault ?? false} className="size-4 accent-brand-700" />
            Bu tür için varsayılan adres yap
          </label>
          <ActionFeedback state={state} />
          <div className="mt-5 flex flex-wrap justify-end gap-3">
            <button type="button" onClick={() => setOpen(false)} className="focus-ring min-h-11 border border-line px-4 text-sm font-bold text-ink hover:bg-surface">Vazgeç</button>
            <button type="submit" disabled={pending} aria-busy={pending} className="focus-ring min-h-11 bg-brand-950 px-5 text-sm font-bold text-white hover:bg-brand-700 disabled:cursor-wait disabled:bg-ink-muted">{pending ? "Kaydediliyor…" : "Adresi kaydet"}</button>
          </div>
        </form>
      ) : state.status !== "idle" ? <ActionFeedback state={state} /> : null}
    </div>
  );
}

// Burada adres türünü backend'in sayısal enum değerleriyle açık etiketlere bağlıyorum.
function AddressSelect({ defaultValue, error }: { defaultValue: string; error?: string }) {
  return (
    <label className="block text-xs font-bold text-ink">
      Adres türü
      <select name="type" defaultValue={defaultValue} aria-invalid={Boolean(error)} aria-describedby={error ? "type-error" : undefined} className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm font-normal text-ink">
        <option value="0">Teslimat adresi</option>
        <option value="1">Fatura adresi</option>
      </select>
      {error ? <span id="type-error" className="mt-1 block text-xs font-semibold text-danger">{error}</span> : null}
    </label>
  );
}

// Burada adres metin alanlarında kalıcı etiket, autocomplete ve alan bazlı hata ilişkisini koruyorum.
function AddressField({ label, name, defaultValue = "", error, autoComplete, type = "text", placeholder, required = true }: { label: string; name: string; defaultValue?: string; error?: string; autoComplete: string; type?: string; placeholder?: string; required?: boolean }) {
  const errorId = `${name}-error`;
  return (
    <label className="block text-xs font-bold text-ink">
      {label}
      <input name={name} type={type} defaultValue={defaultValue} required={required} autoComplete={autoComplete} placeholder={placeholder} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined} className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm font-normal text-ink placeholder:text-ink-muted/70" />
      {error ? <span id={errorId} className="mt-1 block text-xs font-semibold text-danger">{error}</span> : null}
    </label>
  );
}
