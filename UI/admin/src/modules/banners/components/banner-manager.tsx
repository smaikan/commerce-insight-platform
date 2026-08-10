"use client";

/* eslint-disable @next/next/no-img-element */

import { useEffect, useRef, useState } from "react";
import {
  getCloudinaryBannerUploadPreset,
  uploadCloudinaryAsset,
  validateBannerFile,
  type CloudinaryAsset,
} from "@/lib/cloudinary/browser-upload";
import {
  reloadBannerSectionAction,
  updateBannerSectionAction,
} from "@/modules/banners/actions";
import {
  createBannerItemDraft,
  moveBannerDraftItem,
  removeBannerItem,
  selectMainBanner,
  suggestBannerKey,
  toBannerCommitItems,
  toBannerItemDrafts,
  type BannerItemDraft,
} from "@/modules/banners/components/banner-section-draft";
import {
  BANNER_SECTION_CONFIGS,
  BANNER_SECTION_KEYS,
} from "@/modules/banners/section-config";
import type {
  BannerActionResult,
  BannerSection,
  BannerSectionKey,
  BannerSectionLoadResult,
} from "@/modules/banners/types";

const BANNER_ACCEPT = "image/jpeg,image/png,image/webp,video/mp4,video/webm";
const MAX_ITEMS = 5;

type SectionPhase = "idle" | "loading" | "uploading" | "saving" | "success" | "error";

// Burada altı bölümün her birini kendi yükleme ve kaydetme durumuyla ayrı yönetim yüzeyi olarak kuruyorum.
export function BannerManager({ initialSections }: { initialSections: BannerSectionLoadResult[] }) {
  const results = new Map(initialSections.map((result) => [result.key, result]));

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-border bg-surface px-4 py-3 text-sm leading-6 text-muted">
        Her bölüm bağımsız kaydedilir. Bir bölümdeki değişiklik veya hata diğer banner alanlarını etkilemez.
      </div>
      {BANNER_SECTION_KEYS.map((sectionKey) => (
        <BannerSectionEditor
          key={sectionKey}
          sectionKey={sectionKey}
          initialResult={results.get(sectionKey) || {
            key: sectionKey,
            status: "error",
            message: "Banner bölümü yüklenemedi.",
          }}
        />
      ))}
    </div>
  );
}

// Burada tek bölümün taslak, upload, validation ve atomik PUT akışını diğer bölümlerden yalıtıyorum.
function BannerSectionEditor({
  sectionKey,
  initialResult,
}: {
  sectionKey: BannerSectionKey;
  initialResult: BannerSectionLoadResult;
}) {
  const config = BANNER_SECTION_CONFIGS[sectionKey];
  const initialSection = initialResult.status === "success" ? initialResult.section : null;
  const [section, setSection] = useState<BannerSection | null>(initialSection);
  const [drafts, setDrafts] = useState<BannerItemDraft[]>(() => toBannerItemDrafts(initialSection?.items || [], config.isMain));
  const [loadError, setLoadError] = useState(initialResult.status === "error" ? initialResult.message : "");
  const [phase, setPhase] = useState<SectionPhase>(initialResult.status === "error" ? "error" : "idle");
  const [message, setMessage] = useState(initialResult.status === "error" ? initialResult.message : "");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string[]>>({});
  const [dirty, setDirty] = useState(false);
  const uploadedAssetsRef = useRef(new Map<string, CloudinaryAsset>());
  const previewUrlsRef = useRef(new Set<string>());
  const uploadControllerRef = useRef<AbortController | null>(null);
  const isBusy = phase === "loading" || phase === "uploading" || phase === "saving";

  // Burada bölüm kapanırken geçici önizlemeleri ve devam eden yüklemeleri temizliyorum.
  useEffect(() => {
    const previews = previewUrlsRef.current;
    return () => {
      uploadControllerRef.current?.abort();
      previews.forEach((url) => URL.revokeObjectURL(url));
    };
  }, []);

  // Burada kaydedilmemiş bölüm değişikliklerinde sekme kapanışından önce tarayıcı uyarısını etkinleştiriyorum.
  useEffect(() => {
    const warn = (event: BeforeUnloadEvent) => {
      if (!dirty) return;
      event.preventDefault();
    };
    window.addEventListener("beforeunload", warn);
    return () => window.removeEventListener("beforeunload", warn);
  }, [dirty]);

  // Burada bölümdeki alan değişikliklerini tek taslağa uygulayıp kaydedilmemiş durumu işaretliyorum.
  const changeDraft = (clientId: string, changes: Partial<BannerItemDraft>) => {
    setDrafts((current) => current.map((item) => item.clientId === clientId ? { ...item, ...changes } : item));
    setDirty(true);
    setPhase("idle");
    setMessage("");
    setFieldErrors({});
  };

  // Burada bölüme kapasite sınırını aşmadan yeni ve kararlı kimlikli kayıt ekliyorum.
  const addDraft = () => {
    if (isBusy || drafts.length >= MAX_ITEMS) return;
    const draft = createBannerItemDraft(`new-${crypto.randomUUID()}`, config.isMain && drafts.length === 0);
    setDrafts((current) => [...current, { ...draft, displayOrder: current.length }]);
    setDirty(true);
    setPhase("idle");
    setMessage("");
  };

  // Burada seçilen kayıtla ilişkili yerel dosya ve önizlemeyi güvenli biçimde bırakıyorum.
  const releaseDraftMedia = (draft: BannerItemDraft | undefined) => {
    if (draft?.previewUrl) {
      URL.revokeObjectURL(draft.previewUrl);
      previewUrlsRef.current.delete(draft.previewUrl);
    }
    if (draft) uploadedAssetsRef.current.delete(draft.clientId);
  };

  // Burada kullanıcı dosyasını boyut ve tür kurallarıyla doğrulayıp yalnız yerel önizlemeye alıyorum.
  const selectFile = (clientId: string, file: File | undefined) => {
    if (!file || isBusy) return;
    const validationError = validateBannerFile(file);
    if (validationError) {
      setPhase("error");
      setMessage(`${file.name}: ${validationError}`);
      return;
    }
    const current = drafts.find((item) => item.clientId === clientId);
    releaseDraftMedia(current);
    const previewUrl = URL.createObjectURL(file);
    previewUrlsRef.current.add(previewUrl);
    changeDraft(clientId, {
      file,
      previewUrl,
      mediaUrl: "",
      mediaType: file.type.startsWith("video/") ? 2 : 1,
    });
  };

  // Burada manuel URL düzenlendiğinde önceki yerel upload niyetini iptal edip kullanıcı değerini koruyorum.
  const changeMediaUrl = (clientId: string, mediaUrl: string) => {
    const current = drafts.find((item) => item.clientId === clientId);
    releaseDraftMedia(current);
    changeDraft(clientId, { mediaUrl, file: undefined, previewUrl: undefined });
  };

  // Burada kaydı kaldırıp main bölümünde gerekiyorsa ilk kalan kaydı geçerli ana banner yapıyorum.
  const removeDraft = (clientId: string) => {
    const current = drafts.find((item) => item.clientId === clientId);
    releaseDraftMedia(current);
    setDrafts((items) => removeBannerItem(items, clientId, config.isMain));
    setDirty(true);
    setPhase("idle");
    setMessage("");
  };

  // Burada bölüm temizleme niyetini yalnız bu bölümün boş items PUT isteğine hazırlıyorum.
  const clearSection = () => {
    if (isBusy || drafts.length === 0) return;
    drafts.forEach(releaseDraftMedia);
    setDrafts([]);
    setDirty(true);
    setPhase("idle");
    setMessage("");
  };

  // Burada daha önce başarıyla yüklenmemiş yerel dosyaları Cloudinary'ye paralel gönderiyorum.
  const uploadPendingFiles = async (): Promise<string | null> => {
    const pending = drafts.filter((item) => item.file && !uploadedAssetsRef.current.has(item.clientId));
    if (pending.length === 0) return null;

    setPhase("uploading");
    setMessage("Medya dosyaları yükleniyor…");
    const controller = new AbortController();
    uploadControllerRef.current = controller;
    let uploadPreset: string;
    try {
      uploadPreset = getCloudinaryBannerUploadPreset();
    } catch (error) {
      uploadControllerRef.current = null;
      return error instanceof Error ? error.message : "Medya yükleme ayarları bulunamadı.";
    }

    const results = await Promise.all(pending.map(async (item) => {
      try {
        const asset = await uploadCloudinaryAsset({
          file: item.file as File,
          folder: config.folder,
          uploadPreset,
          resourceType: "auto",
          tags: ["storefront-banner", sectionKey],
        }, controller.signal);
        uploadedAssetsRef.current.set(item.clientId, asset);
        return null;
      } catch (error) {
        return `${item.file?.name || item.name || "Medya"}: ${error instanceof Error ? error.message : "Yüklenemedi."}`;
      }
    }));
    uploadControllerRef.current = null;
    return results.find((result): result is string => typeof result === "string") || null;
  };

  // Burada bütün yeni dosyalar yüklenmeden seçilen bölümün atomik PUT işlemini başlatmıyorum.
  const saveSection = async () => {
    if (isBusy || !dirty) return;
    const uploadError = await uploadPendingFiles();
    if (uploadError) {
      setPhase("error");
      setMessage(`${uploadError} Başarılı yüklemeler yeniden denemede tekrar gönderilmeyecek.`);
      return;
    }

    setPhase("saving");
    setMessage(`${config.label} kaydediliyor…`);
    const result = await updateBannerSectionAction(sectionKey, {
      items: toBannerCommitItems(drafts, config.isMain, uploadedAssetsRef.current),
    });
    applyActionResult(result);
  };

  // Burada action sonucunu taslağı koruyarak hata veya güncel yetkili bölüm verisine uyguluyorum.
  const applyActionResult = (result: BannerActionResult) => {
    if (result.status === "error" || !result.section) {
      setPhase("error");
      setMessage(`${result.message}${result.traceId ? ` Takip kodu: ${result.traceId}` : ""}`);
      setFieldErrors(result.fieldErrors || {});
      return;
    }
    previewUrlsRef.current.forEach((url) => URL.revokeObjectURL(url));
    previewUrlsRef.current.clear();
    uploadedAssetsRef.current.clear();
    setSection(result.section);
    setDrafts(toBannerItemDrafts(result.section.items, config.isMain));
    setLoadError("");
    setDirty(false);
    setPhase("success");
    setMessage(result.message);
    setFieldErrors({});
  };

  // Burada yalnız hata alan bölümü güncel admin endpointinden yeniden yükleyip diğer taslaklara dokunmuyorum.
  const reloadSection = async () => {
    if (isBusy) return;
    setPhase("loading");
    setMessage(`${config.label} yeniden yükleniyor…`);
    const result = await reloadBannerSectionAction(sectionKey);
    if (result.status === "error" || !result.section) {
      setPhase("error");
      setLoadError(result.message);
      setMessage(result.message);
      return;
    }
    applyActionResult(result);
  };

  const headingId = `banner-section-${sectionKey}`;

  return (
    <section className="rounded-xl border border-border bg-surface-strong" aria-labelledby={headingId} aria-busy={isBusy}>
      <div className="flex flex-wrap items-start justify-between gap-3 border-b border-border px-4 py-4 sm:px-5">
        <div>
          <div className="flex flex-wrap items-center gap-2">
            <h2 id={headingId} className="text-base font-semibold text-foreground">{config.label}</h2>
            <span className="rounded-md border border-border bg-surface-subtle px-2 py-0.5 text-xs font-semibold tabular-nums text-muted">
              {drafts.length}/{MAX_ITEMS}
            </span>
            {dirty ? <span className="text-xs font-semibold text-warning">Kaydedilmemiş değişiklik</span> : null}
          </div>
          <p className="mt-1 text-sm leading-5 text-muted">
            {config.isMain ? "Seçili ana kayıt ilk gösterilir; bölüm en fazla beş kayıt içerir." : "Bu alan diğer alt banner bölümlerinden bağımsızdır."}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {drafts.length > 0 ? (
            <button type="button" disabled={isBusy} onClick={clearSection} className="min-h-10 rounded-lg border border-red-200 bg-surface px-3 text-sm font-semibold text-danger hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-50">
              Bölümü temizle
            </button>
          ) : null}
          <button type="button" disabled={isBusy || drafts.length >= MAX_ITEMS || Boolean(loadError)} onClick={addDraft} className="min-h-10 rounded-lg border border-border-strong bg-surface px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-50">
            Kayıt ekle
          </button>
        </div>
      </div>

      {message ? (
        <div className={`mx-4 mt-4 rounded-lg border px-3 py-2.5 text-sm sm:mx-5 ${phase === "error" ? "border-red-200 bg-red-50 text-red-900" : phase === "success" ? "border-emerald-200 bg-emerald-50 text-emerald-900" : "border-blue-200 bg-blue-50 text-blue-900"}`} role={phase === "error" ? "alert" : "status"} aria-live="polite">
          <p>{message}</p>
          {fieldErrors.itemsMain?.[0] ? <p className="mt-1 font-medium">{fieldErrors.itemsMain[0]}</p> : null}
          {fieldErrors.items?.[0] ? <p className="mt-1 font-medium">{fieldErrors.items[0]}</p> : null}
        </div>
      ) : null}

      {loadError && !section ? (
        <div className="px-4 py-6 sm:px-5">
          <p className="text-sm text-muted">Bu bölümün kayıtları alınamadı. Diğer bölümleri kullanmaya devam edebilirsiniz.</p>
          <button type="button" disabled={isBusy} onClick={() => void reloadSection()} className="mt-3 min-h-10 rounded-lg bg-primary px-3.5 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-60">
            Bölümü yeniden yükle
          </button>
        </div>
      ) : (
        <div className="space-y-4 px-4 py-4 sm:px-5">
          {drafts.length === 0 ? (
            <div className="rounded-lg border border-dashed border-border-strong bg-surface-subtle px-4 py-6 text-center">
              <p className="text-sm font-semibold text-foreground">Bu bölümde banner bulunmuyor</p>
              <p className="mt-1 text-xs leading-5 text-muted">Yeni kayıt ekleyebilir veya bölümü boş bırakabilirsiniz.</p>
            </div>
          ) : (
            <ol className="space-y-4">
              {drafts.map((draft, index) => (
                <li key={draft.id || draft.clientId}>
                  <BannerItemEditor
                    sectionKey={sectionKey}
                    draft={draft}
                    index={index}
                    count={drafts.length}
                    isMainSection={config.isMain}
                    disabled={isBusy}
                    fieldErrors={fieldErrors}
                    onChange={(changes) => changeDraft(draft.clientId, changes)}
                    onMediaUrlChange={(value) => changeMediaUrl(draft.clientId, value)}
                    onFileSelect={(file) => selectFile(draft.clientId, file)}
                    onRemove={() => removeDraft(draft.clientId)}
                    onMove={(direction) => {
                      setDrafts((items) => moveBannerDraftItem(items, index, direction, config.isMain));
                      setDirty(true);
                      setPhase("idle");
                      setMessage("");
                    }}
                    onSelectMain={() => {
                      setDrafts((items) => selectMainBanner(items, draft.clientId));
                      setDirty(true);
                      setPhase("idle");
                      setMessage("");
                    }}
                  />
                </li>
              ))}
            </ol>
          )}

          <div className="flex flex-col gap-3 border-t border-border pt-4 sm:flex-row sm:items-center sm:justify-between">
            <p className="text-xs leading-5 text-muted">Yalnız {config.label} kayıtları kaydedilir.</p>
            <button type="button" disabled={isBusy || !dirty} onClick={() => void saveSection()} className="inline-flex min-h-11 min-w-40 items-center justify-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:bg-muted disabled:text-white/80">
              {isBusy ? "İşleniyor…" : "Bölümü kaydet"}
            </button>
          </div>
        </div>
      )}
    </section>
  );
}

// Burada tek banner kaydının tüm belgeli alanlarını medya önizlemesi ve erişilebilir sıra kontrolleriyle düzenliyorum.
function BannerItemEditor({
  sectionKey,
  draft,
  index,
  count,
  isMainSection,
  disabled,
  fieldErrors,
  onChange,
  onMediaUrlChange,
  onFileSelect,
  onRemove,
  onMove,
  onSelectMain,
}: {
  sectionKey: BannerSectionKey;
  draft: BannerItemDraft;
  index: number;
  count: number;
  isMainSection: boolean;
  disabled: boolean;
  fieldErrors: Record<string, string[]>;
  onChange: (changes: Partial<BannerItemDraft>) => void;
  onMediaUrlChange: (value: string) => void;
  onFileSelect: (file: File | undefined) => void;
  onRemove: () => void;
  onMove: (direction: -1 | 1) => void;
  onSelectMain: () => void;
}) {
  const baseId = `${sectionKey}-${draft.clientId.replace(/[^A-Za-z0-9_-]/g, "-")}`;

  return (
    <article className="overflow-hidden rounded-lg border border-border bg-surface" aria-labelledby={`${baseId}-title`}>
      <div className="flex flex-wrap items-center justify-between gap-2 border-b border-border bg-surface-subtle px-3 py-2.5">
        <div>
          <h3 id={`${baseId}-title`} className="text-sm font-semibold text-foreground">Kayıt {index + 1}</h3>
          <p className="text-xs text-muted">Sıra {index}</p>
        </div>
        <div className="flex flex-wrap gap-1.5">
          {isMainSection && !draft.isMain ? (
            <button type="button" disabled={disabled} onClick={onSelectMain} className="min-h-9 rounded-md border border-primary/40 bg-primary-soft px-2.5 text-xs font-semibold text-primary hover:border-primary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-50">
              Ana banner yap
            </button>
          ) : null}
          {isMainSection && draft.isMain ? <span className="inline-flex min-h-9 items-center rounded-md bg-primary-soft px-2.5 text-xs font-semibold text-primary">Ana banner</span> : null}
          <button type="button" disabled={disabled || index === 0 || (isMainSection && (draft.isMain || index === 1))} onClick={() => onMove(-1)} aria-label={`${draft.name || `Kayıt ${index + 1}`} yukarı taşı`} className="min-h-9 rounded-md border border-border-strong bg-surface px-2.5 text-xs font-semibold text-foreground hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-40">Yukarı</button>
          <button type="button" disabled={disabled || index === count - 1 || (isMainSection && draft.isMain)} onClick={() => onMove(1)} aria-label={`${draft.name || `Kayıt ${index + 1}`} aşağı taşı`} className="min-h-9 rounded-md border border-border-strong bg-surface px-2.5 text-xs font-semibold text-foreground hover:bg-surface-subtle focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-40">Aşağı</button>
          <button type="button" disabled={disabled} onClick={onRemove} className="min-h-9 rounded-md border border-red-200 bg-surface px-2.5 text-xs font-semibold text-danger hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:opacity-50">Kaldır</button>
        </div>
      </div>

      <div className="grid gap-4 p-3 lg:grid-cols-[minmax(14rem,0.8fr)_minmax(0,2fr)] lg:p-4">
        <div className="space-y-3">
          <BannerMediaPreview draft={draft} />
          <label className={`flex min-h-10 cursor-pointer items-center justify-center rounded-lg border border-border-strong bg-surface px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle focus-within:ring-2 focus-within:ring-focus ${disabled ? "pointer-events-none opacity-50" : ""}`}>
            Medya yükle veya değiştir
            <input type="file" className="sr-only" accept={BANNER_ACCEPT} disabled={disabled} aria-label={`${draft.name || `Kayıt ${index + 1}`} için medya seç`} onChange={(event) => {
              onFileSelect(event.target.files?.[0]);
              event.target.value = "";
            }} />
          </label>
          <p className="text-xs leading-5 text-muted">Görsel: JPG, PNG, WebP ve 8 MB. Video: MP4, WebM ve 25 MB.</p>
        </div>

        <div className="grid content-start gap-3 sm:grid-cols-2">
          <Field label="Ad" id={`${baseId}-name`} error={fieldError(fieldErrors, index, "name")}>
            <input id={`${baseId}-name`} value={draft.name} maxLength={150} disabled={disabled} onChange={(event) => onChange({ name: event.target.value })} className={inputClass(Boolean(fieldError(fieldErrors, index, "name")))} />
          </Field>
          <Field label="Anahtar" id={`${baseId}-key`} error={fieldError(fieldErrors, index, "key")} action={draft.name && !draft.key ? (
            <button type="button" disabled={disabled} onClick={() => onChange({ key: suggestBannerKey(draft.name) })} className="text-xs font-semibold text-primary hover:underline focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">Anahtar öner</button>
          ) : undefined}>
            <input id={`${baseId}-key`} value={draft.key} maxLength={100} disabled={disabled} spellCheck={false} onChange={(event) => onChange({ key: event.target.value })} className={inputClass(Boolean(fieldError(fieldErrors, index, "key")))} />
          </Field>
          <Field label="Medya URL'si" id={`${baseId}-media-url`} error={fieldError(fieldErrors, index, "mediaUrl")} className="sm:col-span-2">
            <input id={`${baseId}-media-url`} type="url" value={draft.mediaUrl} maxLength={500} disabled={disabled} placeholder="https://..." onChange={(event) => onMediaUrlChange(event.target.value)} className={inputClass(Boolean(fieldError(fieldErrors, index, "mediaUrl")))} />
          </Field>
          <Field label="Medya tipi" id={`${baseId}-media-type`} error={fieldError(fieldErrors, index, "mediaType")}>
            <select id={`${baseId}-media-type`} value={draft.mediaType} disabled={disabled || Boolean(draft.file)} onChange={(event) => onChange({ mediaType: Number(event.target.value) as 1 | 2 })} className={inputClass(Boolean(fieldError(fieldErrors, index, "mediaType")))}>
              <option value={1}>Görsel</option>
              <option value={2}>Video</option>
            </select>
          </Field>
          <div className="flex items-end pb-1">
            <label className="inline-flex min-h-10 items-center gap-2 text-sm font-medium text-foreground">
              <input type="checkbox" checked={draft.isActive} disabled={disabled || (isMainSection && draft.isMain)} onChange={(event) => onChange({ isActive: event.target.checked })} className="size-4 accent-primary" />
              Aktif
            </label>
          </div>
          <Field label="Hedef URL" id={`${baseId}-target-url`} error={fieldError(fieldErrors, index, "targetUrl")} className="sm:col-span-2">
            <input id={`${baseId}-target-url`} value={draft.targetUrl} maxLength={500} disabled={disabled} placeholder="/collections/yaz veya https://..." onChange={(event) => onChange({ targetUrl: event.target.value })} className={inputClass(Boolean(fieldError(fieldErrors, index, "targetUrl")))} />
          </Field>
          <Field label="Alternatif metin" id={`${baseId}-alt-text`} error={fieldError(fieldErrors, index, "altText")} className="sm:col-span-2">
            <textarea id={`${baseId}-alt-text`} value={draft.altText} maxLength={500} rows={2} disabled={disabled} onChange={(event) => onChange({ altText: event.target.value })} className={`${inputClass(Boolean(fieldError(fieldErrors, index, "altText")))} min-h-20 py-2`} />
          </Field>
        </div>
      </div>
    </article>
  );
}

// Burada kayıt medyasını sabit oranlı görsel, kontrollü video veya sessiz boş durum olarak gösteriyorum.
function BannerMediaPreview({ draft }: { draft: BannerItemDraft }) {
  const [failed, setFailed] = useState(false);
  const source = draft.previewUrl || draft.mediaUrl;

  if (!source || failed) {
    return (
      <div className="flex aspect-video items-center justify-center rounded-lg border border-dashed border-border-strong bg-surface-subtle px-4 text-center text-xs text-muted">
        {failed ? "Medya önizlemesi açılamadı." : "Henüz medya seçilmedi."}
      </div>
    );
  }
  if (draft.mediaType === 2) {
    return <video key={source} src={source} controls muted playsInline preload="metadata" aria-label={draft.altText || draft.name || "Banner videosu"} onError={() => setFailed(true)} className="aspect-video w-full rounded-lg bg-black object-cover" />;
  }
  return <img key={source} src={source} alt={draft.altText || draft.name || ""} onError={() => setFailed(true)} className="aspect-video w-full rounded-lg bg-surface-subtle object-cover" />;
}

// Burada alan etiketi, yardımcı eylem ve API doğrulama hatasını aynı erişilebilir grupta tutuyorum.
function Field({ label, id, error, action, className = "", children }: {
  label: string;
  id: string;
  error?: string;
  action?: React.ReactNode;
  className?: string;
  children: React.ReactNode;
}) {
  return (
    <div className={className}>
      <div className="mb-1.5 flex items-center justify-between gap-2">
        <label htmlFor={id} className="text-sm font-medium text-foreground">{label}</label>
        {action}
      </div>
      {children}
      {error ? <p className="mt-1 text-xs leading-5 text-danger">{error}</p> : null}
    </div>
  );
}

// Burada API'nin items.N.field hata anahtarını ilgili düzenleme alanına bağlıyorum.
function fieldError(errors: Record<string, string[]>, index: number, field: string): string | undefined {
  return errors[`items.${index}.${field}`]?.[0];
}

// Burada form kontrollerinin ortak yoğunluk, focus ve hata görünümünü tek sınıf setinde tutuyorum.
function inputClass(error: boolean): string {
  return `min-h-10 w-full rounded-lg border bg-surface px-3 text-sm text-foreground outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:cursor-not-allowed disabled:bg-surface-subtle ${error ? "border-danger" : "border-border-strong"}`;
}
