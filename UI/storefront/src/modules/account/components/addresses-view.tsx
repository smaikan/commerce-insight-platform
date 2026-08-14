import { AccountPageHeader } from "@/modules/account/components/account-page-header";
import { AddressCardActions } from "@/modules/account/components/address-card-actions";
import { AddressEditor } from "@/modules/account/components/address-editor";
import type { AccountAddress } from "@/modules/account/contracts";

// Burada adresleri teslimat ve fatura olarak ayırıp mobilde tek, geniş ekranda iki sütunlu yönetilebilir kartlara dönüştürüyorum.
export function AddressesView({ addresses }: { addresses: AccountAddress[] }) {
  const shipping = addresses.filter((address) => address.type === 0);
  const billing = addresses.filter((address) => address.type === 1);

  return (
    <section>
      <AccountPageHeader
        eyebrow="Teslimat bilgileri"
        title="Adreslerim"
        description="Teslimat ve fatura adreslerinizi yönetin; her adres türü için ayrı bir varsayılan seçebilirsiniz."
      />
      <div className="mt-5 flex justify-end"><AddressEditor primary /></div>

      {addresses.length ? (
        <div className="mt-7 grid gap-8 xl:grid-cols-2">
          <AddressGroup title="Teslimat adresleri" addresses={shipping} />
          <AddressGroup title="Fatura adresleri" addresses={billing} />
        </div>
      ) : (
        <div className="mt-7 border border-line bg-surface px-6 py-10 text-center">
          <h2 className="text-lg font-black text-ink">Kayıtlı adresiniz yok</h2>
          <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-ink-muted">Checkout sırasında kullanmak için ilk teslimat veya fatura adresinizi ekleyin.</p>
        </div>
      )}
    </section>
  );
}

// Burada aynı türdeki adresleri tekrarlı varlık kartları olarak, boş tür durumunu da kaybetmeden listeliyorum.
function AddressGroup({ title, addresses }: { title: string; addresses: AccountAddress[] }) {
  return (
    <section aria-labelledby={`${title.replaceAll(" ", "-")}-title`}>
      <div className="flex items-center justify-between border-b border-line pb-3">
        <h2 id={`${title.replaceAll(" ", "-")}-title`} className="text-base font-black text-ink">{title}</h2>
        <span className="text-xs font-bold text-ink-muted">{addresses.length} adres</span>
      </div>
      {addresses.length ? <div className="mt-4 space-y-4">{addresses.map((address) => <AddressCard key={address.id} address={address} />)}</div> : <p className="mt-4 border border-dashed border-line px-4 py-6 text-center text-sm text-ink-muted">Bu türde kayıtlı adres bulunmuyor.</p>}
    </section>
  );
}

// Burada tek adresin kimlik, iletişim ve aksiyonlarını okunabilir fakat kompakt bir yüzeyde topluyorum.
function AddressCard({ address }: { address: AccountAddress }) {
  return (
    <article className="border border-line bg-surface p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h3 className="text-base font-black text-ink">{address.title}</h3>
            {address.isDefault ? <span className="border border-brand-600/25 bg-surface-subtle px-2 py-1 text-[0.6875rem] font-bold text-brand-700">Varsayılan</span> : null}
          </div>
          <p className="mt-2 text-sm font-semibold text-ink">{address.firstName} {address.lastName}</p>
          <p className="mt-1 text-xs text-ink-muted">{address.phoneNumber}</p>
        </div>
        <AddressEditor address={address} />
      </div>
      <address className="mt-4 text-sm not-italic leading-6 text-ink-muted">
        <span className="block">{address.fullAddress}</span>
        <span className="block">{address.district} / {address.city}{address.postalCode ? ` · ${address.postalCode}` : ""}</span>
      </address>
      <AddressCardActions id={address.id} isDefault={address.isDefault} />
    </article>
  );
}
