"use client";

import { useActionState, useState } from "react";
import { createCouponAction, updateCouponAction } from "@/modules/coupons/actions";
import { initialCouponActionState, type Coupon } from "@/modules/coupons/types";

// Burada yeni ve mevcut kupon için aynı gerçek sözleşmeye bağlı düzenleme formunu sunuyorum.
export function CouponForm({ coupon, onCancel }: { coupon?: Coupon; onCancel?: () => void }) {
  const [discountType, setDiscountType] = useState<number>(coupon?.discountType ?? 0);
  const action = coupon ? updateCouponAction.bind(null, coupon.id) : createCouponAction;
  const [state, formAction, isPending] = useActionState(action, initialCouponActionState);
  const fieldError = (name: string) => state.fieldErrors?.[name]?.[0];

  return (
    <form action={formAction} className="space-y-5">
      {state.status === "error" ? <div role="alert" className="rounded-xl border border-danger/30 bg-danger/10 px-4 py-3 text-sm text-danger"><p className="font-semibold">İşlem tamamlanamadı</p><p className="mt-1">{state.message}</p>{state.traceId ? <p className="mt-2 font-mono text-xs">Takip: {state.traceId}</p> : null}</div> : null}
      <section aria-labelledby="coupon-basics-title" className="rounded-xl border border-border bg-surface">
        <div className="border-b border-border bg-surface-subtle px-4 py-3 sm:px-5"><h2 id="coupon-basics-title" className="text-base font-semibold text-foreground">Kupon bilgileri</h2><p className="mt-1 text-sm text-muted">Müşterilerin ödeme adımında kullanacağı kodu ve indirim değerini belirleyin.</p></div>
        <div className="grid gap-4 p-4 sm:grid-cols-2 sm:p-5">
          <Field label="Kupon kodu" error={fieldError("code")}><input id="coupon-code" name="code" required maxLength={100} defaultValue={coupon?.code} placeholder="YAZ10" className={inputClass} /></Field>
          <Field label="Açıklama" error={fieldError("description")}><input id="coupon-description" name="description" maxLength={500} defaultValue={coupon?.description ?? ""} placeholder="Yaz kampanyası" className={inputClass} /></Field>
          <Field label="İndirim türü" error={fieldError("discountType")}><select id="coupon-discount-type" name="discountType" value={discountType} onChange={(event) => setDiscountType(Number(event.target.value))} className={inputClass}><option value={0}>Yüzde indirimi</option><option value={1}>Sabit tutar indirimi</option></select></Field>
          <Field label={discountType === 0 ? "İndirim oranı" : "İndirim tutarı"} error={fieldError("discountValue")}><div className="relative"><input id="coupon-discount-value" name="discountValue" required type="number" inputMode="decimal" min="0.01" max={discountType === 0 ? 100 : undefined} step="0.01" defaultValue={coupon?.discountValue} className={`${inputClass} pr-12`} /><span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm font-semibold text-muted">{discountType === 0 ? "%" : "TL"}</span></div></Field>
        </div>
      </section>
      <section aria-labelledby="coupon-conditions-title" className="rounded-xl border border-border bg-surface">
        <div className="border-b border-border bg-surface-subtle px-4 py-3 sm:px-5"><h2 id="coupon-conditions-title" className="text-base font-semibold text-foreground">Koşullar ve süre</h2><p className="mt-1 text-sm text-muted">Boş bırakılan limit ve tarih alanları için ilgili kısıt uygulanmaz.</p></div>
        <div className="grid gap-4 p-4 sm:grid-cols-2 sm:p-5">
          <Field label="Minimum sepet tutarı" error={fieldError("minimumOrderAmount")}><div className="relative"><input id="coupon-minimum-order" name="minimumOrderAmount" type="number" inputMode="decimal" min="0" step="0.01" defaultValue={coupon?.minimumOrderAmount ?? ""} placeholder="Koşul yok" className={`${inputClass} pr-10`} /><span className="pointer-events-none absolute inset-y-0 right-3 flex items-center text-sm font-semibold text-muted">TL</span></div></Field>
          <Field label="Toplam kullanım limiti" error={fieldError("usageLimit")}><input id="coupon-usage-limit" name="usageLimit" type="number" inputMode="numeric" min="1" step="1" defaultValue={coupon?.usageLimit ?? ""} placeholder="Sınırsız" className={inputClass} /></Field>
          <Field label="Başlangıç tarihi" error={fieldError("startsAt")}><input id="coupon-starts-at" name="startsAt" type="datetime-local" defaultValue={toDateTimeLocal(coupon?.startsAt)} className={inputClass} /></Field>
          <Field label="Bitiş tarihi" error={fieldError("expiresAt")}><input id="coupon-expires-at" name="expiresAt" type="datetime-local" defaultValue={toDateTimeLocal(coupon?.expiresAt)} className={inputClass} /></Field>
        </div>
      </section>
      <section aria-labelledby="coupon-availability-title" className="rounded-xl border border-border bg-surface px-4 py-4 sm:px-5"><h2 id="coupon-availability-title" className="text-base font-semibold text-foreground">Uygunluk</h2><div className="mt-3 grid gap-3 sm:grid-cols-2"><CheckField name="isActive" label="Kupon aktif" description="Kapalı kupon checkout'ta kullanılamaz." defaultChecked={coupon?.isActive ?? true} /><CheckField name="isMemberOnly" label="Yalnız üyelere özel" description="Misafir checkout bu kuponu kullanamaz." defaultChecked={coupon?.isMemberOnly ?? false} /></div></section>
      <section aria-label="Kupon kapsamı" className="rounded-xl border border-primary/20 bg-primary/5 px-4 py-3 text-sm text-muted"><p className="font-semibold text-foreground">Bu kuponun kapsamı</p><p className="mt-1 leading-6">İndirim tutarı checkout sırasında backend tarafından hesaplanır. Ürün, kategori veya koleksiyona özel indirim; ücretsiz kargo; hediye ürün ve otomatik kampanya bu sözleşmede yer almaz.</p></section>
      <div className="flex flex-col-reverse gap-2 border-t border-border pt-4 sm:flex-row sm:justify-end"><a href="/coupons" onClick={onCancel} className="inline-flex min-h-10 items-center justify-center rounded-lg border border-border-strong bg-surface-strong px-4 text-sm font-semibold text-foreground hover:bg-surface-subtle">Vazgeç</a><button type="submit" disabled={isPending} className="inline-flex min-h-10 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-60">{isPending ? "Kaydediliyor…" : coupon ? "Değişiklikleri kaydet" : "Kuponu oluştur"}</button></div>
    </form>
  );
}

const inputClass = "min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30";

// Burada alan etiketi, içeriği ve sözleşmeden gelen hata mesajını aynı düzende gösteriyorum.
function Field({ label, error, children }: { label: string; error?: string; children: React.ReactNode }) {
  return <label className="block text-sm font-medium text-foreground"><span>{label}</span><div className="mt-1.5">{children}</div>{error ? <span className="mt-1 block text-xs font-medium text-danger">{error}</span> : null}</label>;
}

// Burada iki boolean kupon koşulunu erişilebilir açıklamasıyla sunuyorum.
function CheckField({ name, label, description, defaultChecked }: { name: string; label: string; description: string; defaultChecked: boolean }) {
  return <label className="flex min-h-16 items-start gap-3 rounded-lg border border-border bg-surface-subtle/50 px-3 py-3"><input name={name} type="checkbox" defaultChecked={defaultChecked} className="mt-0.5 size-4 rounded border-border-strong text-primary focus:ring-focus" /><span><span className="block text-sm font-semibold text-foreground">{label}</span><span className="mt-0.5 block text-xs leading-5 text-muted">{description}</span></span></label>;
}

// Burada API'nin UTC tarihini datetime-local alanının kabul ettiği yerel değere çeviriyorum.
function toDateTimeLocal(value: string | null | undefined): string {
  if (!value) return "";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "";
  const offset = date.getTimezoneOffset() * 60_000;
  return new Date(date.getTime() - offset).toISOString().slice(0, 16);
}
