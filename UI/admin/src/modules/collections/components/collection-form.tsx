import type { Collection } from "@/modules/collections/types";

type CollectionFormProps = {
  action: (formData: FormData) => Promise<void>;
  collection?: Collection;
  mode: "create" | "edit";
};

// Burada manuel koleksiyon oluşturma ve düzenleme alanlarını tek, tutarlı form yüzeyinde sunuyorum.
export function CollectionForm({ action, collection, mode }: CollectionFormProps) {
  const isCreate = mode === "create";
  return (
    <form action={action} className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_18rem]">
      <section className="rounded-xl border border-border bg-surface-strong p-4 sm:p-5" aria-labelledby="collection-details-title">
        <div className="border-b border-border pb-4">
          <h2 id="collection-details-title" className="text-base font-semibold text-foreground">Koleksiyon bilgileri</h2>
          <p className="mt-1 text-sm leading-5 text-muted">Müşterilerin göreceği adı, açıklamayı ve bağlantı adresini belirleyin.</p>
        </div>
        <div className="mt-5 grid gap-4">
          <Field label="Koleksiyon adı" name="name" defaultValue={collection?.name || ""} required maxLength={150} />
          <Field label="Bağlantı" name="url" defaultValue={collection?.url || ""} maxLength={200} help="Boş bırakırsanız API koleksiyon adına göre bir bağlantı üretir." />
          <label className="block text-sm font-medium text-foreground">
            Açıklama
            <textarea name="description" rows={7} maxLength={1000} defaultValue={collection?.description || ""} className="mt-1.5 w-full rounded-lg border border-border-strong bg-surface-strong px-3 py-2 text-sm leading-6 text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft" />
            <span className="mt-1 block text-xs font-normal leading-5 text-muted">En fazla 1.000 karakter.</span>
          </label>
        </div>
      </section>

      <aside className="space-y-4 lg:sticky lg:top-20">
        <section className="rounded-xl border border-border bg-surface-strong p-4" aria-labelledby="collection-type-title">
          <h2 id="collection-type-title" className="text-base font-semibold text-foreground">Koleksiyon türü</h2>
          <div className="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-3">
            <p className="text-sm font-semibold text-slate-800">Manuel</p>
            <p className="mt-1 text-xs leading-5 text-slate-600">Ürünleri ürün oluşturma veya düzenleme ekranından seçersiniz.</p>
          </div>
          <div className="mt-3 rounded-lg border border-border bg-surface-subtle p-3" aria-disabled="true">
            <div className="flex items-center justify-between gap-3">
              <p className="text-sm font-semibold text-muted">Otomatik</p>
              <span className="text-[11px] font-semibold text-muted">Geliştirme aşamasında</span>
            </div>
            <p className="mt-1 text-xs leading-5 text-muted">Koşul sözleşmesi API&apos;ye eklendiğinde etkinleştirilecek.</p>
          </div>
        </section>

        <section className="rounded-xl border border-border bg-surface-strong p-4" aria-labelledby="collection-order-title">
          <h2 id="collection-order-title" className="text-base font-semibold text-foreground">Sıralama</h2>
          <div className="mt-3">
            <Field label="Görüntüleme sırası" name="displayOrder" type="number" defaultValue={String(collection?.displayOrder ?? 0)} min="0" />
          </div>
          {isCreate ? (
            <div className="mt-4 border-t border-border pt-3">
              <Checkbox name="isActive" label="Aktif" defaultChecked />
              <Checkbox name="isFeatured" label="Vitrinde öne çıkar" />
            </div>
          ) : (
            <p className="mt-3 text-xs leading-5 text-muted">Aktiflik ve vitrin durumu, kaydı yanlışlıkla değiştirmemek için ayrı kontrollerden yönetilir.</p>
          )}
        </section>

        <button type="submit" className="inline-flex min-h-11 w-full items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white outline-none hover:bg-primary-hover focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2">
          {isCreate ? "Koleksiyonu oluştur" : "Değişiklikleri kaydet"}
        </button>
      </aside>
    </form>
  );
}

// Burada koleksiyon formundaki tek satırlı alanları aynı etiket ve yardım yapısında tutuyorum.
function Field({ label, name, defaultValue, type = "text", required = false, maxLength, min, help }: { label: string; name: string; defaultValue: string; type?: string; required?: boolean; maxLength?: number; min?: string; help?: string }) {
  return (
    <label className="block text-sm font-medium text-foreground">
      {label}{required ? " *" : ""}
      <input name={name} type={type} defaultValue={defaultValue} required={required} maxLength={maxLength} min={min} className="mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary focus:ring-2 focus:ring-primary-soft" />
      {help ? <span className="mt-1 block text-xs font-normal leading-5 text-muted">{help}</span> : null}
    </label>
  );
}

// Burada oluşturma sırasında başlangıç durumlarını erişilebilir onay kutularıyla topluyorum.
function Checkbox({ name, label, defaultChecked = false }: { name: string; label: string; defaultChecked?: boolean }) {
  return <label className="flex min-h-10 items-center gap-2 text-sm font-medium text-foreground"><input name={name} type="checkbox" defaultChecked={defaultChecked} className="size-4 accent-primary" />{label}</label>;
}
