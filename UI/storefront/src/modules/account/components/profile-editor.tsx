"use client";

import Link from "next/link";
import { useActionState, useState } from "react";

import { updateProfileAction } from "@/modules/account/actions";
import { ActionFeedback } from "@/modules/account/components/action-feedback";
import { INITIAL_ACCOUNT_ACTION_STATE, type AccountUser } from "@/modules/account/contracts";
import { PhoneField } from "@/components/storefront/phone-field";

// Burada müşteri profilini önce okunabilir özet, isteğe bağlı olarak da dar kapsamlı düzenleme formu halinde sunuyorum.
export function ProfileEditor({ user }: { user: AccountUser }) {
  const [editing, setEditing] = useState(false);
  const [state, action, pending] = useActionState(updateProfileAction, INITIAL_ACCOUNT_ACTION_STATE);

  return (
    <section className="border border-line bg-surface" aria-labelledby="profile-title">
      <div className="flex items-start justify-between gap-4 border-b border-line px-5 py-4 sm:px-6">
        <div>
          <h2 id="profile-title" className="text-base font-black text-ink">Kişisel bilgiler</h2>
          <p className="mt-1 text-xs leading-5 text-ink-muted">Sipariş iletişiminde kullanılan temel hesap bilgileriniz.</p>
        </div>
        <button type="button" onClick={() => setEditing((value) => !value)} className="focus-ring min-h-10 border border-line px-3 text-xs font-bold text-brand-700 hover:bg-surface-subtle" aria-expanded={editing}>
          {editing ? "Vazgeç" : "Düzenle"}
        </button>
      </div>

      {editing ? (
        <form action={action} className="px-5 py-5 sm:px-6" noValidate>
          <div className="grid gap-4 sm:grid-cols-2">
            <ProfileField label="Ad" name="firstName" defaultValue={user.firstName} error={state.fieldErrors?.firstName} autoComplete="given-name" />
            <ProfileField label="Soyad" name="lastName" defaultValue={user.lastName} error={state.fieldErrors?.lastName} autoComplete="family-name" />
            <PhoneField variant="account" label="Telefon" name="phoneNumber" defaultValue={user.phoneNumber ?? ""} error={state.fieldErrors?.phoneNumber} autoComplete="tel" />
            <div>
              <p className="text-xs font-bold text-ink">E-posta</p>
              <p className="mt-2 min-h-11 border border-line bg-surface-subtle px-3 py-2.5 text-sm text-ink-muted">{user.email}</p>
              <Link href="/account/security" className="focus-ring mt-2 inline-block text-xs font-bold text-brand-700 underline-offset-4 hover:underline">E-posta ve güvenlik ayarları</Link>
            </div>
          </div>
          <ActionFeedback state={state} />
          <div className="mt-5 flex justify-end">
            <button type="submit" disabled={pending} className="focus-ring min-h-11 bg-brand-950 px-5 text-sm font-bold text-white hover:bg-brand-700 disabled:cursor-wait disabled:bg-ink-muted" aria-busy={pending}>
              {pending ? "Kaydediliyor…" : "Değişiklikleri kaydet"}
            </button>
          </div>
        </form>
      ) : (
        <dl className="grid gap-x-8 gap-y-5 px-5 py-5 text-sm sm:grid-cols-2 sm:px-6">
          <ProfileValue label="Ad soyad" value={`${user.firstName} ${user.lastName}`} />
          <ProfileValue label="E-posta" value={user.email} />
          <ProfileValue label="Telefon" value={user.phoneNumber || "Henüz eklenmedi"} />
          <ProfileValue label="Müşteri numarası" value={user.id} />
        </dl>
      )}
    </section>
  );
}

// Burada profil alanlarında kalıcı etiket, autocomplete ve alan hatası ilişkisini koruyorum.
function ProfileField({ label, name, defaultValue, error, autoComplete, type = "text" }: { label: string; name: string; defaultValue: string; error?: string; autoComplete: string; type?: string }) {
  const errorId = `${name}-error`;
  return (
    <label className="block text-xs font-bold text-ink">
      {label}
      <input name={name} type={type} defaultValue={defaultValue} required={name !== "phoneNumber"} autoComplete={autoComplete} aria-invalid={Boolean(error)} aria-describedby={error ? errorId : undefined} className="focus-ring mt-2 min-h-11 w-full border border-line bg-surface px-3 text-sm font-normal text-ink" />
      {error ? <span id={errorId} className="mt-1 block text-xs font-semibold text-danger">{error}</span> : null}
    </label>
  );
}

// Burada profil özet değerlerini uzun müşteri verilerinde de kırılabilir biçimde sunuyorum.
function ProfileValue({ label, value }: { label: string; value: string }) {
  return <div><dt className="text-xs font-bold text-ink-muted">{label}</dt><dd className="mt-1 break-words font-semibold text-ink">{value}</dd></div>;
}
