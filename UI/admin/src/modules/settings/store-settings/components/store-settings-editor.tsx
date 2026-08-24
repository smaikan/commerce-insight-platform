"use client";

import { useEffect, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { reloadStoreSettingsAction, saveStoreSettingsSectionAction } from "@/modules/settings/store-settings/actions";
import { StoreImageField } from "@/modules/settings/store-settings/components/store-image-field";
import {
  draftsFromSettings,
  STORE_SETTINGS_SECTIONS,
  type StoreSettingsActionResult,
  type StoreSettingsDrafts,
  type StoreSettingsSection,
} from "@/modules/settings/store-settings/types";
import type { AdminStoreSettings } from "@/modules/settings/types";

export function StoreSettingsEditor({ initialSettings }: { initialSettings: AdminStoreSettings }) {
  const router = useRouter();
  const [settings, setSettings] = useState(initialSettings);
  const [drafts, setDrafts] = useState(() => draftsFromSettings(initialSettings));
  const [activeSection, setActiveSection] = useState<StoreSettingsSection>("identity");
  const [dirtySections, setDirtySections] = useState<Set<StoreSettingsSection>>(() => new Set());
  const [results, setResults] = useState<Partial<Record<StoreSettingsSection, StoreSettingsActionResult>>>({});
  const [pending, startTransition] = useTransition();

  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => {
      if (!dirtySections.size) return;
      event.preventDefault();
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirtySections.size]);

  const changeSection = <Section extends StoreSettingsSection>(section: Section, value: StoreSettingsDrafts[Section]) => {
    setDrafts((current) => ({ ...current, [section]: value }));
    setDirtySections((current) => new Set(current).add(section));
    setResults((current) => ({ ...current, [section]: undefined }));
  };

  const save = (section: StoreSettingsSection) => {
    startTransition(async () => {
      const result = await saveStoreSettingsSectionAction({
        section,
        expectedConcurrencyToken: settings.concurrencyToken,
        values: drafts[section],
      } as Parameters<typeof saveStoreSettingsSectionAction>[0]);
      setResults((current) => ({ ...current, [section]: result }));
      if (result.status !== "success" || !result.settings) return;

      const freshDrafts = draftsFromSettings(result.settings);
      setSettings(result.settings);
      setDrafts((current) => ({ ...current, [section]: freshDrafts[section] }));
      setDirtySections((current) => {
        const next = new Set(current);
        next.delete(section);
        return next;
      });
      // Burada kimlik değişikliğinden sonra sidebar mağaza adı ve root favicon metadata'sını güncel server verisiyle yeniden çiziyorum.
      if (section === "identity") router.refresh();
    });
  };

  const loadCurrent = (snapshot?: AdminStoreSettings) => {
    startTransition(async () => {
      const result = snapshot
        ? { status: "success" as const, message: "Güncel ayarlar yüklendi.", settings: snapshot }
        : await reloadStoreSettingsAction();
      if (result.status === "success" && result.settings) {
        setSettings(result.settings);
        setDrafts(draftsFromSettings(result.settings));
        setDirtySections(new Set());
        setResults({});
      } else {
        setResults((current) => ({ ...current, [activeSection]: result }));
      }
    });
  };

  const result = results[activeSection];
  return (
    <div className="overflow-hidden rounded-xl border border-border bg-surface">
      <header className="border-b border-border bg-surface-subtle/50 px-4 py-4 sm:px-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-base font-semibold text-foreground">Mağaza yapılandırması</h2>
            <p className="mt-1 text-sm leading-5 text-muted">Her bölüm bağımsız kaydedilir; diğer ayarlarınız değişmeden kalır.</p>
          </div>
          <StoreStatusBadge status={settings.status} />
        </div>
      </header>

      <div className="grid min-w-0 lg:grid-cols-[210px_minmax(0,1fr)]">
        <nav className="border-b border-border bg-surface-subtle/25 p-2 lg:border-b-0 lg:border-r" aria-label="Mağaza ayarı bölümleri">
          <div className="grid grid-cols-2 gap-1 sm:grid-cols-3 lg:grid-cols-1">
            {STORE_SETTINGS_SECTIONS.map((section) => {
              const active = section.key === activeSection;
              return (
                <button
                  key={section.key}
                  type="button"
                  aria-pressed={active}
                  onClick={() => setActiveSection(section.key)}
                  className={`min-h-12 cursor-pointer rounded-lg px-3 py-2 text-left outline-none transition-colors focus-visible:ring-2 focus-visible:ring-focus ${active ? "bg-primary-soft text-primary-hover" : "text-muted hover:bg-surface hover:text-foreground"}`}
                >
                  <span className="flex items-center gap-2 text-sm font-semibold">
                    {section.label}
                    {dirtySections.has(section.key) ? <span className="size-2 rounded-full bg-warning" aria-label="Kaydedilmemiş değişiklik var" /> : null}
                  </span>
                  <span className="mt-0.5 hidden text-xs leading-4 opacity-80 lg:block">{section.description}</span>
                </button>
              );
            })}
          </div>
        </nav>

        <main className="min-w-0">
          <form onSubmit={(event) => { event.preventDefault(); save(activeSection); }}>
            <div className="p-4 sm:p-5">
              {activeSection === "identity" ? <IdentitySection value={drafts.identity} disabled={pending} result={result} onChange={(value) => changeSection("identity", value)} /> : null}
              {activeSection === "contact" ? <ContactSection value={drafts.contact} result={result} onChange={(value) => changeSection("contact", value)} /> : null}
              {activeSection === "legal" ? <LegalSection value={drafts.legal} result={result} onChange={(value) => changeSection("legal", value)} /> : null}
              {activeSection === "seo" ? <SeoSection value={drafts.seo} disabled={pending} result={result} onChange={(value) => changeSection("seo", value)} /> : null}
              {activeSection === "storefront" ? <StorefrontSection value={drafts.storefront} result={result} onChange={(value) => changeSection("storefront", value)} /> : null}
            </div>

            {result?.status === "conflict" ? (
              <div role="alert" className="mx-4 mb-4 rounded-xl border border-warning/40 bg-warning/10 p-4 text-sm sm:mx-5">
                <p className="font-semibold text-foreground">Ayarlar güncellendi</p>
                <p className="mt-1 leading-5 text-muted">{result.message}</p>
                <button type="button" disabled={pending} onClick={() => loadCurrent(result.currentSettings)} className="mt-3 min-h-9 cursor-pointer rounded-lg border border-warning/50 bg-surface px-3 text-xs font-semibold text-foreground transition-colors hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-60">
                  Güncel ayarları yükle
                </button>
              </div>
            ) : result?.status === "error" ? (
              <div role="alert" className="mx-4 mb-4 rounded-xl border border-danger/30 bg-danger/10 p-3 text-sm text-danger sm:mx-5">
                <p className="font-semibold">{result.message}</p>
                {result.traceId ? <p className="mt-1 font-mono text-xs">Takip: {result.traceId}</p> : null}
              </div>
            ) : null}

            <footer className="flex flex-col gap-3 border-t border-border bg-surface-subtle/35 px-4 py-3 sm:flex-row sm:items-center sm:justify-between sm:px-5">
              <div aria-live="polite" className="min-h-5 text-sm">
                {result?.status === "success" ? <p className="font-medium text-success">{result.message}</p> : dirtySections.has(activeSection) ? <p className="text-muted">Kaydedilmemiş değişiklikler var.</p> : <p className="text-muted">Bu bölüm güncel.</p>}
              </div>
              <button type="submit" disabled={pending || !dirtySections.has(activeSection)} className="inline-flex min-h-10 cursor-pointer items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white transition-colors hover:bg-primary-hover disabled:cursor-not-allowed disabled:opacity-50">
                {pending ? "Kaydediliyor…" : "Değişiklikleri kaydet"}
              </button>
            </footer>
          </form>
        </main>
      </div>
    </div>
  );
}

function IdentitySection({ value, disabled, result, onChange }: SectionProps<"identity"> & { disabled: boolean }) {
  return (
    <Section title="Mağaza kimliği" description="Müşterilerin mağazanızı tanıdığı ad, kısa açıklama ve marka görselleri.">
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField id="store-display-name" label="Mağaza adı" value={value.displayName} maxLength={150} required error={fieldError(result, "displayName")} onChange={(displayName) => onChange({ ...value, displayName })} />
        <TextArea id="store-short-description" label="Kısa açıklama" value={value.shortDescription} maxLength={500} rows={3} className="sm:col-span-2" error={fieldError(result, "shortDescription")} onChange={(shortDescription) => onChange({ ...value, shortDescription })} />
      </div>
      <div className="mt-5 grid gap-3 xl:grid-cols-2">
        <StoreImageField id="store-logo" label="Açık zemin logosu" hint="JPG, PNG veya WebP · en fazla 8 MB" slot="logo" value={value.logoUrl ?? null} disabled={disabled} error={fieldError(result, "logoUrl")} onChange={(logoUrl) => onChange({ ...value, logoUrl })} />
        <StoreImageField id="store-dark-logo" label="Koyu zemin logosu" hint="Koyu yüzeylerde kullanılacak alternatif logo" slot="darkLogo" value={value.darkLogoUrl ?? null} disabled={disabled} error={fieldError(result, "darkLogoUrl")} onChange={(darkLogoUrl) => onChange({ ...value, darkLogoUrl })} />
        <StoreImageField id="store-favicon" label="Favicon" hint="Kare ve sade bir görsel önerilir" slot="favicon" value={value.faviconUrl ?? null} disabled={disabled} error={fieldError(result, "faviconUrl")} onChange={(faviconUrl) => onChange({ ...value, faviconUrl })} />
        <StoreImageField id="store-share-image" label="Varsayılan paylaşım görseli" hint="Bağlantı paylaşımlarında mağazayı temsil eder" slot="defaultShareImage" value={value.defaultShareImageUrl ?? null} disabled={disabled} error={fieldError(result, "defaultShareImageUrl")} onChange={(defaultShareImageUrl) => onChange({ ...value, defaultShareImageUrl })} />
      </div>
    </Section>
  );
}

function ContactSection({ value, result, onChange }: SectionProps<"contact">) {
  return (
    <Section title="İletişim bilgileri" description="İletişim kanallarınızı girin ve hangilerinin storefront’ta gösterileceğini seçin.">
      <div className="grid gap-4 sm:grid-cols-2">
        <ContactField label="Destek e-postası" id="store-support-email" type="email" value={value.supportEmail} maxLength={320} visible={value.showSupportEmail} error={fieldError(result, "supportEmail")} onValue={(supportEmail) => onChange({ ...value, supportEmail })} onVisible={(showSupportEmail) => onChange({ ...value, showSupportEmail })} />
        <ContactField label="Destek telefonu" id="store-support-phone" type="tel" value={value.supportPhone} maxLength={30} visible={value.showSupportPhone} error={fieldError(result, "supportPhone")} onValue={(supportPhone) => onChange({ ...value, supportPhone })} onVisible={(showSupportPhone) => onChange({ ...value, showSupportPhone })} />
        <ContactField label="WhatsApp numarası" id="store-whatsapp" type="tel" value={value.whatsappNumber} maxLength={30} visible={value.showWhatsapp} error={fieldError(result, "whatsappNumber")} onValue={(whatsappNumber) => onChange({ ...value, whatsappNumber })} onVisible={(showWhatsapp) => onChange({ ...value, showWhatsapp })} />
        <ContactField label="Harita bağlantısı" id="store-map-url" type="url" value={value.mapUrl} maxLength={500} visible={value.showMap} error={fieldError(result, "mapUrl")} onValue={(mapUrl) => onChange({ ...value, mapUrl })} onVisible={(showMap) => onChange({ ...value, showMap })} />
        <ContactTextArea label="İletişim adresi" id="store-contact-address" value={value.contactAddress} maxLength={1000} visible={value.showContactAddress} error={fieldError(result, "contactAddress")} onValue={(contactAddress) => onChange({ ...value, contactAddress })} onVisible={(showContactAddress) => onChange({ ...value, showContactAddress })} />
      </div>
    </Section>
  );
}

function LegalSection({ value, result, onChange }: SectionProps<"legal">) {
  return (
    <Section title="Yasal şirket bilgileri" description="Fatura, mesafeli satış ve resmi mağaza belgelerinde kullanılan işletme kayıtları.">
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField id="store-legal-name" label="Yasal şirket adı" value={value.legalCompanyName} maxLength={200} error={fieldError(result, "legalCompanyName")} onChange={(legalCompanyName) => onChange({ ...value, legalCompanyName })} />
        <TextField id="store-tax-office" label="Vergi dairesi" value={value.taxOffice} maxLength={150} error={fieldError(result, "taxOffice")} onChange={(taxOffice) => onChange({ ...value, taxOffice })} />
        <TextField id="store-tax-number" label="Vergi numarası" value={value.taxNumber} maxLength={50} error={fieldError(result, "taxNumber")} onChange={(taxNumber) => onChange({ ...value, taxNumber })} />
        <TextField id="store-identity-number" label="T.C. kimlik numarası" value={value.nationalIdentityNumber} maxLength={50} error={fieldError(result, "nationalIdentityNumber")} onChange={(nationalIdentityNumber) => onChange({ ...value, nationalIdentityNumber })} />
        <TextField id="store-mersis" label="MERSİS numarası" value={value.mersisNumber} maxLength={50} error={fieldError(result, "mersisNumber")} onChange={(mersisNumber) => onChange({ ...value, mersisNumber })} />
        <TextField id="store-registry" label="Ticaret sicil numarası" value={value.tradeRegistryNumber} maxLength={50} error={fieldError(result, "tradeRegistryNumber")} onChange={(tradeRegistryNumber) => onChange({ ...value, tradeRegistryNumber })} />
        <TextField id="store-country" label="Ülke" value={value.country} maxLength={150} error={fieldError(result, "country")} onChange={(country) => onChange({ ...value, country })} />
        <TextField id="store-city" label="Şehir" value={value.city} maxLength={150} error={fieldError(result, "city")} onChange={(city) => onChange({ ...value, city })} />
        <TextField id="store-district" label="İlçe" value={value.district} maxLength={150} error={fieldError(result, "district")} onChange={(district) => onChange({ ...value, district })} />
        <TextField id="store-postal-code" label="Posta kodu" value={value.postalCode} maxLength={20} error={fieldError(result, "postalCode")} onChange={(postalCode) => onChange({ ...value, postalCode })} />
        <TextArea id="store-address" label="Şirket adresi" value={value.addressLine} maxLength={1000} rows={4} className="sm:col-span-2" error={fieldError(result, "addressLine")} onChange={(addressLine) => onChange({ ...value, addressLine })} />
      </div>
    </Section>
  );
}

function SeoSection({ value, disabled, result, onChange }: SectionProps<"seo"> & { disabled: boolean }) {
  return (
    <Section title="SEO ve sosyal hesaplar" description="Storefront sayfalarının varsayılan arama görünümünü ve resmi sosyal bağlantılarını belirleyin.">
      <div className="grid gap-4 sm:grid-cols-2">
        <TextField id="store-default-title" label="Varsayılan sayfa başlığı" value={value.defaultTitle} maxLength={200} error={fieldError(result, "defaultTitle")} onChange={(defaultTitle) => onChange({ ...value, defaultTitle })} />
        <TextField id="store-title-template" label="Başlık şablonu" value={value.titleTemplate} maxLength={250} placeholder="%s | Mağaza Adı" hint="Sayfa başlığının yerini %s ile belirtin." error={fieldError(result, "titleTemplate")} onChange={(titleTemplate) => onChange({ ...value, titleTemplate })} />
        <TextArea id="store-default-description" label="Varsayılan meta açıklaması" value={value.defaultDescription} maxLength={500} rows={3} className="sm:col-span-2" error={fieldError(result, "defaultDescription")} onChange={(defaultDescription) => onChange({ ...value, defaultDescription })} />
        <div className="sm:col-span-2">
          <StoreImageField id="store-og-image" label="Open Graph görseli" hint="Sosyal paylaşım önizlemeleri için yatay görsel önerilir" slot="defaultOpenGraphImage" value={value.defaultOpenGraphImageUrl ?? null} disabled={disabled} error={fieldError(result, "defaultOpenGraphImageUrl")} onChange={(defaultOpenGraphImageUrl) => onChange({ ...value, defaultOpenGraphImageUrl })} />
        </div>
        <Switch label="Arama motorlarının dizine eklemesine izin ver" description="Kapatıldığında storefront noindex tercihini uygulayabilir." checked={value.allowIndexing} onChange={(allowIndexing) => onChange({ ...value, allowIndexing })} className="sm:col-span-2" />
      </div>
      <h3 className="mt-6 border-t border-border pt-5 text-sm font-semibold text-foreground">Sosyal medya bağlantıları</h3>
      <div className="mt-3 grid gap-4 sm:grid-cols-2">
        {(["facebookUrl", "instagramUrl", "tiktokUrl", "youtubeUrl", "xUrl", "pinterestUrl"] as const).map((field) => (
          <TextField key={field} id={`store-${field}`} type="url" label={SOCIAL_LABELS[field]} value={value[field]} maxLength={500} placeholder="https://" error={fieldError(result, field)} onChange={(next) => onChange({ ...value, [field]: next })} />
        ))}
      </div>
    </Section>
  );
}

function StorefrontSection({ value, result, onChange }: SectionProps<"storefront">) {
  return (
    <Section title="Storefront tercihleri" description="Mağazanın çalışma durumunu ve ürünlerin müşterilere nasıl sunulacağını belirleyin.">
      <fieldset>
        <legend className="text-sm font-semibold text-foreground">Mağaza durumu</legend>
        <div className="mt-2 grid gap-2 sm:grid-cols-3">
          {STATUS_OPTIONS.map((option) => (
            <label key={option.value} className={`cursor-pointer rounded-xl border p-3 ${value.status === option.value ? "border-primary bg-primary-soft" : "border-border bg-surface hover:bg-surface-subtle"}`}>
              <span className="flex items-center gap-2"><input type="radio" name="store-status" checked={value.status === option.value} onChange={() => onChange({ ...value, status: option.value })} className="size-4 text-primary focus:ring-focus" /><span className="text-sm font-semibold text-foreground">{option.label}</span></span>
              <span className="mt-1 block pl-6 text-xs leading-5 text-muted">{option.description}</span>
            </label>
          ))}
        </div>
        {fieldError(result, "status") ? <p className="mt-1 text-xs font-medium text-danger">{fieldError(result, "status")}</p> : null}
      </fieldset>
      <div className="mt-4">
        <TextArea id="store-status-message" label="Durum mesajı" value={value.statusMessage} maxLength={500} rows={3} hint="Bakım veya kapalı ekranında müşterilere gösterilecek açıklama." error={fieldError(result, "statusMessage")} onChange={(statusMessage) => onChange({ ...value, statusMessage })} />
      </div>
      <div className="mt-6 grid gap-3 border-t border-border pt-5 sm:grid-cols-2">
        <Switch label="Stokta olmayan ürünleri göster" description="Kapalıysa stokta olmayan ürünler liste sonuçlarından çıkarılır." checked={value.showOutOfStockProducts} onChange={(showOutOfStockProducts) => onChange({ ...value, showOutOfStockProducts })} />
        <Switch label="Fiyatsız ürünleri göster" description="Kapalıysa satış fiyatı bulunmayan ürünler listelenmez." checked={value.showProductsWithoutPrice} onChange={(showProductsWithoutPrice) => onChange({ ...value, showProductsWithoutPrice })} />
        <Switch label="Karşılaştırma fiyatını göster" description="İndirim öncesi fiyatın ürün kartlarında görünmesini belirler." checked={value.showCompareAtPrice} onChange={(showCompareAtPrice) => onChange({ ...value, showCompareAtPrice })} />
        <Switch label="Düşük stok uyarısını göster" description="Belirlenen eşik içindeki ürünlerde stok uyarısını etkinleştirir." checked={value.showStockWarning} onChange={(showStockWarning) => onChange({ ...value, showStockWarning })} />
      </div>
      <div className="mt-5 grid gap-4 sm:grid-cols-3">
        <label className="block text-sm font-medium text-foreground sm:col-span-2">
          Varsayılan ürün sıralaması
          <select value={value.defaultProductSort} onChange={(event) => onChange({ ...value, defaultProductSort: Number(event.target.value) as 0 | 1 | 2 | 3 })} className={INPUT_CLASS}>
            <option value={0}>En yeni</option><option value={1}>Popülerlik</option><option value={2}>Gösterim sırası</option><option value={3}>Ürün adı</option>
          </select>
          {fieldError(result, "defaultProductSort") ? <span className="mt-1 block text-xs font-medium text-danger">{fieldError(result, "defaultProductSort")}</span> : null}
        </label>
        <TextField id="store-low-stock" label="Düşük stok eşiği" type="number" value={String(value.lowStockThreshold)} min={1} max={1_000_000} error={fieldError(result, "lowStockThreshold")} onChange={(next) => onChange({ ...value, lowStockThreshold: Number(next) })} />
        <Switch label="Azalan sıralama" description="Seçilen sıralamayı büyükten küçüğe uygular." checked={value.defaultProductSortDescending} onChange={(defaultProductSortDescending) => onChange({ ...value, defaultProductSortDescending })} className="sm:col-span-3" />
      </div>
    </Section>
  );
}

type SectionProps<Section extends StoreSettingsSection> = {
  value: StoreSettingsDrafts[Section];
  result?: StoreSettingsActionResult;
  onChange: (value: StoreSettingsDrafts[Section]) => void;
};

function Section({ title, description, children }: { title: string; description: string; children: React.ReactNode }) {
  return <section><header className="mb-5"><h2 className="text-base font-semibold text-foreground">{title}</h2><p className="mt-1 text-sm leading-5 text-muted">{description}</p></header>{children}</section>;
}

function TextField({ id, label, value, onChange, error, hint, className = "", type = "text", ...props }: {
  id: string; label: string; value?: string | null; onChange: (value: string) => void; error?: string; hint?: string; className?: string; type?: string; required?: boolean; maxLength?: number; min?: number; max?: number; placeholder?: string;
}) {
  return <label htmlFor={id} className={`block text-sm font-medium text-foreground ${className}`}>{label}{props.required ? " *" : ""}<input id={id} type={type} value={value ?? ""} onChange={(event) => onChange(event.target.value)} aria-invalid={Boolean(error)} className={INPUT_CLASS} {...props} />{error ? <span className="mt-1 block text-xs font-medium text-danger">{error}</span> : hint ? <span className="mt-1 block text-xs leading-5 text-muted">{hint}</span> : null}</label>;
}

function TextArea({ id, label, value, onChange, error, hint, className = "", rows, maxLength }: { id: string; label: string; value?: string | null; onChange: (value: string) => void; error?: string; hint?: string; className?: string; rows: number; maxLength: number }) {
  return <label htmlFor={id} className={`block text-sm font-medium text-foreground ${className}`}>{label}<textarea id={id} value={value ?? ""} onChange={(event) => onChange(event.target.value)} rows={rows} maxLength={maxLength} aria-invalid={Boolean(error)} className={`${INPUT_CLASS} py-2`} />{error ? <span className="mt-1 block text-xs font-medium text-danger">{error}</span> : hint ? <span className="mt-1 block text-xs leading-5 text-muted">{hint}</span> : null}</label>;
}

function ContactField({ visible, onVisible, onValue, ...props }: Omit<Parameters<typeof TextField>[0], "onChange"> & { visible: boolean; onVisible: (value: boolean) => void; onValue: (value: string) => void }) {
  return <div className="rounded-xl border border-border p-3"><TextField {...props} onChange={onValue} /><VisibilityToggle checked={visible} onChange={onVisible} /></div>;
}

function ContactTextArea({ visible, onVisible, onValue, ...props }: Omit<Parameters<typeof TextArea>[0], "onChange" | "rows"> & { visible: boolean; onVisible: (value: boolean) => void; onValue: (value: string) => void }) {
  return <div className="rounded-xl border border-border p-3"><TextArea {...props} rows={3} onChange={onValue} /><VisibilityToggle checked={visible} onChange={onVisible} /></div>;
}

function VisibilityToggle({ checked, onChange }: { checked: boolean; onChange: (value: boolean) => void }) {
  return <label className="mt-3 flex cursor-pointer items-center gap-2 border-t border-border pt-3 text-xs font-semibold text-muted"><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="size-4 rounded border-border-strong text-primary focus:ring-focus" />Storefront’ta göster</label>;
}

function Switch({ label, description, checked, onChange, className = "" }: { label: string; description: string; checked: boolean; onChange: (value: boolean) => void; className?: string }) {
  return <label className={`flex cursor-pointer items-start justify-between gap-3 rounded-xl border border-border bg-surface-subtle/30 p-3 ${className}`}><span><span className="block text-sm font-semibold text-foreground">{label}</span><span className="mt-0.5 block text-xs leading-5 text-muted">{description}</span></span><input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="mt-0.5 size-4 shrink-0 rounded border-border-strong text-primary focus:ring-focus" /></label>;
}

function StoreStatusBadge({ status }: { status: 0 | 1 | 2 }) {
  const option = STATUS_OPTIONS.find((candidate) => candidate.value === status) ?? STATUS_OPTIONS[0];
  const tone = status === 0 ? "border-success/30 bg-success/10 text-success" : status === 1 ? "border-warning/40 bg-warning/10 text-foreground" : "border-danger/30 bg-danger/10 text-danger";
  return <span className={`inline-flex w-fit items-center rounded-full border px-2.5 py-1 text-xs font-semibold ${tone}`}>{option.label}</span>;
}

function fieldError(result: StoreSettingsActionResult | undefined, name: string): string | undefined {
  const key = Object.keys(result?.fieldErrors ?? {}).find((candidate) => candidate.toLocaleLowerCase("tr-TR") === name.toLocaleLowerCase("tr-TR"));
  return key ? result?.fieldErrors?.[key]?.[0] : undefined;
}

const INPUT_CLASS = "mt-1.5 min-h-10 w-full rounded-lg border border-border-strong bg-surface px-3 text-sm text-foreground outline-none placeholder:text-muted focus:border-primary focus:ring-2 focus:ring-focus/30";
const STATUS_OPTIONS = [
  { value: 0 as const, label: "Aktif", description: "Mağaza normal şekilde açık." },
  { value: 1 as const, label: "Bakım", description: "Geçici bakım mesajı gösterilir." },
  { value: 2 as const, label: "Kapalı", description: "Mağaza kapalı durumu gösterilir." },
];
const SOCIAL_LABELS = { facebookUrl: "Facebook", instagramUrl: "Instagram", tiktokUrl: "TikTok", youtubeUrl: "YouTube", xUrl: "X", pinterestUrl: "Pinterest" } as const;
