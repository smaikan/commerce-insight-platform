"use client";

import { useEffect, useState } from "react";
import { ConfirmDialog } from "@/lib/admin/components/confirm-dialog";
import { deleteProductVariantAction } from "@/modules/products/actions";
import type { ProductVariant } from "@/modules/products/types";
import { variantNeedsCanonicalIdentity, variantsByCombination } from "@/modules/products/variant-editing";
import {
  buildVariantCombinations,
  groupsFromVariants,
  splitComposite,
  type VariantCombination,
  type VariantOptionGroupDraft,
} from "@/modules/products/variant-combinations";

type DraftVariant = {
  key: string;
  id?: string;
  expectedConcurrencyToken?: string;
  originalName?: string;
  originalValue?: string;
  changed: boolean;
  identityChanged: boolean;
  schemaNormalized: boolean;
  sku: string;
  price: string;
  stock: string;
  compareAtPrice: string;
  barcode: string;
  material: string;
  isActive: boolean;
};

type DraftField = keyof Pick<
  DraftVariant,
  "sku" | "price" | "stock" | "compareAtPrice" | "barcode" | "material"
>;

type VariantDetailMap = Record<string, DraftVariant[]>;

const inputClass =
  "min-h-11 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary sm:min-h-9";

// Burada API varyantını belirli bir çapraz kombinasyonun satış detaylarına dönüştürüyorum.
function toDraft(variant: ProductVariant, key: string): DraftVariant {
  return {
    key,
    id: variant.id,
    expectedConcurrencyToken: variant.concurrencyToken,
    originalName: variant.name,
    originalValue: variant.value,
    changed: false,
    identityChanged: false,
    schemaNormalized: false,
    sku: variant.sku,
    price: String(variant.price),
    stock: String(variant.stock),
    compareAtPrice: variant.compareAtPrice == null ? "" : String(variant.compareAtPrice),
    barcode: variant.barcode || "",
    material: variant.material || "",
    isActive: variant.isActive,
  };
}

// Burada yeni kombinasyon için kontrollü alanlarda kullanılacak boş satış detayını oluşturuyorum.
function emptyDraft(key = "variant-new-0"): DraftVariant {
  return {
    key,
    changed: false,
    identityChanged: false,
    schemaNormalized: false,
    sku: "",
    price: "",
    stock: "0",
    compareAtPrice: "",
    barcode: "",
    material: "",
    isActive: true,
  };
}

// Burada mevcut birleşik varyantları doğru çapraz kombinasyon anahtarlarına bağlayarak kimliklerini koruyorum.
function detailsFromVariants(
  variants: ProductVariant[],
  combinations: VariantCombination[],
): VariantDetailMap {
  return Object.fromEntries(
    Object.entries(variantsByCombination(variants, combinations)).map(([combinationKey, groupedVariants]) => {
      const combination = combinations.find((item) => item.key === combinationKey);
      if (!combination) return [combinationKey, []];
      return [combinationKey, groupedVariants.map((variant) => {
        const draft = toDraft(variant, `variant-${variant.id}`);
        const schemaNormalized = variantNeedsCanonicalIdentity(variant, combination);
        return {
          ...draft,
          changed: schemaNormalized,
          identityChanged: schemaNormalized,
          schemaNormalized,
        };
      })];
    }),
  );
}

// Burada seçenek yapısı değiştiğinde ortak seçimleri eşleyip mevcut varyant kimliklerini ilk uygun kombinasyona taşıyorum.
function rebaseDetails(
  previousCombinations: VariantCombination[],
  nextCombinations: VariantCombination[],
  current: VariantDetailMap,
): VariantDetailMap {
  const next: VariantDetailMap = {};
  const usedPreviousKeys = new Set<string>();

  nextCombinations.forEach((combination) => {
    if (current[combination.key]?.length && !usedPreviousKeys.has(combination.key)) {
      const previousCombination = previousCombinations.find((item) => item.key === combination.key);
      const combinationChanged = previousCombination
        ? previousCombination.name !== combination.name || previousCombination.value !== combination.value
        : false;
      next[combination.key] = current[combination.key].map((detail) => ({
        ...detail,
        changed: detail.changed || Boolean(detail.id && combinationChanged),
        identityChanged: detail.identityChanged || Boolean(detail.id && combinationChanged),
        schemaNormalized: combinationChanged ? false : detail.schemaNormalized,
      }));
      usedPreviousKeys.add(combination.key);
      return;
    }

    const candidates = previousCombinations.filter(
      (previous) => current[previous.key]?.length && !usedPreviousKeys.has(previous.key) && combinationsAreCompatible(previous, combination),
    );
    const matched = candidates.find((candidate) => current[candidate.key].some((detail) => detail.id)) || candidates[0];
    if (matched) {
      next[combination.key] = current[matched.key].map((detail) => ({
        ...detail,
        changed: detail.changed || Boolean(detail.id),
        identityChanged: detail.identityChanged || Boolean(detail.id),
        schemaNormalized: false,
      }));
      usedPreviousKeys.add(matched.key);
    }
  });

  return next;
}

// Burada eski ve yeni kombinasyonların ortak seçenek gruplarında aynı değeri taşıyıp taşımadığını kontrol ediyorum.
function combinationsAreCompatible(previous: VariantCombination, next: VariantCombination): boolean {
  const previousSelections = new Map(previous.selections.map((selection) => [selection.groupKey, selection.valueKey]));
  return next.selections.every(
    (selection) => !previousSelections.has(selection.groupKey) || previousSelections.get(selection.groupKey) === selection.valueKey,
  );
}

// Burada seçenek adı veya değeri değişince yalnız ilgili kayıtlı varyantları güncellenecek olarak işaretliyorum.
function markPersistedDetailsChanged(
  current: VariantDetailMap,
  affectedKeys?: Set<string>,
): VariantDetailMap {
  return Object.fromEntries(
    Object.entries(current).map(([key, details]) => [
      key,
      details.map((detail) => detail.id && (!affectedKeys || affectedKeys.has(key))
        ? { ...detail, changed: true, identityChanged: true, schemaNormalized: false }
        : detail),
    ]),
  );
}

// Burada varyantsız tek ürün ile en fazla üç seçenekli çapraz varyant akışını aynı bölümde yönetiyorum.
export function VariantEditor({
  variants,
  mode,
  productId,
  initialHasVariants,
  initialMainSku,
  fieldErrors,
  deletionDisabled = false,
  onVariantDeleted,
  onNormalizationNeeded,
}: {
  variants: ProductVariant[];
  mode: "create" | "edit";
  productId?: string;
  initialHasVariants: boolean;
  initialMainSku: string;
  fieldErrors?: Record<string, string[]>;
  deletionDisabled?: boolean;
  onVariantDeleted?: (variantId: string, message: string) => void;
  onNormalizationNeeded?: () => void;
}) {
  const initialGroups = variants.length > 0 ? groupsFromVariants(variants) : [];
  const initialCombinations = buildVariantCombinations(initialGroups);
  const initialDetails = detailsFromVariants(variants, initialCombinations);
  const initialSimpleDraft = variants[0] ? toDraft(variants[0], "variant-new-0") : emptyDraft();

  const [hasVariants, setHasVariants] = useState(initialHasVariants);
  const [mainSku, setMainSku] = useState(initialMainSku);
  const [simpleDraft, setSimpleDraft] = useState<DraftVariant>(initialSimpleDraft);
  const [groups, setGroups] = useState<VariantOptionGroupDraft[]>(initialGroups);
  const [details, setDetails] = useState<VariantDetailMap>(initialDetails);
  const [deleteCandidate, setDeleteCandidate] = useState<{ id: string; label: string }>();
  const [deleteError, setDeleteError] = useState<string>();
  const [deleting, setDeleting] = useState(false);
  const combinations = buildVariantCombinations(groups);
  const preventsDisabling = mode === "edit" && initialHasVariants && variants.length > 1;
  const canDeletePersistedVariant = mode === "edit" && Boolean(productId) && variants.length > 1;
  const hasSchemaNormalization = Object.values(initialDetails).some((items) =>
    items.some((detail) => detail.schemaNormalized),
  );

  // Burada legacy ad/değerlerin canonical şemaya taşınacağını üst forma gerçek bir kayıt değişikliği olarak bildiriyorum.
  useEffect(() => {
    if (hasSchemaNormalization) onNormalizationNeeded?.();
  }, [hasSchemaNormalization, onNormalizationNeeded]);

  // Burada silme onayından sonra gerçek API kaydını kaldırıyor, hata halinde form taslağını yerinde koruyorum.
  const confirmVariantDeletion = async () => {
    if (!productId || !deleteCandidate || deleting || deletionDisabled) return;
    setDeleting(true);
    setDeleteError(undefined);
    const result = await deleteProductVariantAction(productId, deleteCandidate.id);
    setDeleting(false);
    if (result.status === "error") {
      setDeleteError(`${result.message}${result.traceId ? ` Takip kodu: ${result.traceId}` : ""}`);
      return;
    }
    const deletedId = deleteCandidate.id;
    setDeleteCandidate(undefined);
    onVariantDeleted?.(deletedId, result.message);
  };

  // Burada varyant modunu değiştirirken çoklu mevcut varyantların yanlışlıkla gizlenmesini engelliyorum.
  const changeVariantMode = (checked: boolean) => {
    if (!checked && preventsDisabling) return;

    if (!checked && combinations[0]) {
      const source = details[combinations[0].key]?.[0] || emptyDraft();
      setSimpleDraft({ ...source, changed: source.changed || Boolean(source.id) });
    }
    setHasVariants(checked);
  };

  // Burada ana SKU değişikliğini varyantsız üründeki tek satış satırına da değişiklik niyeti olarak yansıtıyorum.
  const changeMainSku = (value: string) => {
    setMainSku(value);
    if (!hasVariants) setSimpleDraft((current) => ({ ...current, changed: true }));
  };

  // Burada seçenek gruplarındaki yapısal değişikliklerden sonra varyant detaylarını yeni çarpıma eşliyorum.
  const applyGroups = (nextGroups: VariantOptionGroupDraft[]) => {
    const nextCombinations = buildVariantCombinations(nextGroups);
    setDetails((current) => rebaseDetails(combinations, nextCombinations, current));
    setGroups(nextGroups);
  };

  // Burada PDF akışındaki gibi yeni bir seçenek adı ve ilk boş değerini en fazla üç gruba kadar ekliyorum.
  const addGroup = () => {
    if (groups.length >= 3) return;
    const groupKey = `group-new-${crypto.randomUUID()}`;
    applyGroups([
      ...groups,
      {
        key: groupKey,
        name: "",
        persisted: false,
        values: [{ key: `${groupKey}-value-${crypto.randomUUID()}`, value: "", persisted: false }],
      },
    ]);
  };

  // Burada seçenek adını değiştirirken kombinasyon anahtarlarını ve girilmiş satış detaylarını sabit tutuyorum.
  const updateGroupName = (groupKey: string, name: string) => {
    setGroups((current) => current.map((group) => (group.key === groupKey ? { ...group, name } : group)));
    setDetails((current) => markPersistedDetailsChanged(current));
  };

  // Burada seçeneğe yeni bir değer ekleyip oluşan yeni çapraz satırları boş detaylarla hazırlıyorum.
  const addValue = (groupKey: string) => {
    const group = groups.find((item) => item.key === groupKey);
    if (!group || projectedCombinationCount(groups, groupKey) > 50) return;
    const nextGroups = groups.map((item) =>
      item.key === groupKey
        ? {
            ...item,
            values: [
              ...item.values,
              { key: `${groupKey}-value-${crypto.randomUUID()}`, value: "", persisted: false },
            ],
          }
        : item,
    );
    applyGroups(nextGroups);
  };

  // Burada seçenek değerinin metnini değiştirirken ona bağlı tüm kombinasyon detaylarını koruyorum.
  const updateValue = (groupKey: string, valueKey: string, value: string) => {
    setGroups((current) =>
      current.map((group) =>
        group.key === groupKey
          ? {
              ...group,
              values: group.values.map((item) => (item.key === valueKey ? { ...item, value } : item)),
            }
          : group,
      ),
    );
    const affectedKeys = new Set(
      combinations
        .filter((combination) => combination.selections.some((selection) => selection.groupKey === groupKey && selection.valueKey === valueKey))
        .map((combination) => combination.key),
    );
    setDetails((current) => markPersistedDetailsChanged(current, affectedKeys));
  };

  // Burada kayıtlı varyant kimliği taşımayan yeni değerleri ve onlardan doğan geçici detayları kaldırıyorum.
  const removeValue = (groupKey: string, valueKey: string) => {
    if (valueOwnsPersistedDetail(combinations, details, groupKey, valueKey)) return;
    const nextGroups = groups.map((group) =>
      group.key === groupKey
        ? { ...group, values: group.values.filter((value) => value.key !== valueKey) }
        : group,
    );
    applyGroups(nextGroups);
  };

  // Burada yalnız bu formda eklenmiş seçenek grubunu kaldırıp ortak kombinasyon detaylarını geriye taşıyorum.
  const removeGroup = (groupKey: string) => {
    const group = groups.find((item) => item.key === groupKey);
    if (!group || group.persisted) return;
    applyGroups(groups.filter((item) => item.key !== groupKey));
  };

  // Burada her kombinasyonun fiyat, stok ve SKU alanlarını kendi anahtarında güncelliyorum.
  const updateDetail = (combinationKey: string, detailKey: string, field: DraftField, value: string) => {
    setDetails((current) => ({
      ...current,
      [combinationKey]: (current[combinationKey]?.length
        ? current[combinationKey]
        : [emptyDraft(combinationKey)]
      ).map((detail) => detail.key === detailKey ? { ...detail, [field]: value, changed: true } : detail),
    }));
  };

  return (
    <section aria-labelledby="variants-title" className="rounded-xl border border-border bg-surface-strong">
      <div className="flex flex-col gap-3 border-b border-border px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 id="variants-title" className="text-base font-semibold text-foreground">Fiyatlandırma ve varyantlar</h2>
          <p className="mt-0.5 text-xs leading-5 text-muted">
            {mode === "edit"
              ? "Mevcut stok yalnız aşağıdaki stok hareketi bölümünden değiştirilir; yeni varyantlarda açılış stoğu girilebilir."
              : "Tek ürün bilgilerini girin veya seçenek değerlerini çaprazlayarak varyantlar oluşturun."}
          </p>
        </div>
        <label className="inline-flex min-h-11 shrink-0 items-center gap-2 rounded-lg border border-border bg-surface-subtle px-3 text-sm font-semibold text-foreground sm:min-h-9">
          <input
            type="checkbox"
            name="hasVariants"
            checked={hasVariants}
            disabled={preventsDisabling}
            onChange={(event) => changeVariantMode(event.target.checked)}
            className="size-4 accent-primary disabled:cursor-not-allowed"
          />
          Varyantlı ürün
        </label>
        {preventsDisabling ? <input type="hidden" name="hasVariants" value="on" /> : null}
      </div>

      <div className="p-4">
        <Field
          label={hasVariants ? "Ana SKU" : "SKU"}
          name="mainSku"
          value={mainSku}
          onChange={changeMainSku}
          required
          maxLength={100}
          help={hasVariants ? "Ürünün arama ve katalog seviyesindeki ana kodudur." : "Tek SKU, ürün ve varsayılan satış kaydı için birlikte kullanılır."}
          error={mergeErrors(fieldErrors?.mainSku, !hasVariants ? fieldErrors?.["variants.0.sku"] : undefined)}
        />

        {preventsDisabling ? (
          <p className="mt-3 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs leading-5 text-amber-900">
            Birden fazla kayıtlı varyant bulunduğu için varyant modu doğrudan kapatılamaz. Varyantları tek kayıt kalana kadar ayrı ayrı silebilirsiniz.
          </p>
        ) : null}
        {fieldErrors?.hasVariants ? <FieldError messages={fieldErrors.hasVariants} /> : null}
        {fieldErrors?.variants ? <FieldError messages={fieldErrors.variants} /> : null}

        {hasVariants ? (
          <VariantOptions
            groups={groups}
            combinations={combinations}
            details={details}
            addGroup={addGroup}
            updateGroupName={updateGroupName}
            removeGroup={removeGroup}
            addValue={addValue}
            updateValue={updateValue}
            removeValue={removeValue}
            updateDetail={updateDetail}
            fieldErrors={fieldErrors}
            canDeletePersistedVariant={canDeletePersistedVariant}
            deletionDisabled={deletionDisabled || deleting}
            requestVariantDeletion={(id, label) => {
              setDeleteError(undefined);
              setDeleteCandidate({ id, label });
            }}
          />
        ) : (
          <SimpleProductFields
            draft={simpleDraft}
            mainSku={mainSku}
            setDraft={setSimpleDraft}
            fieldErrors={fieldErrors}
          />
        )}
      </div>
      {canDeletePersistedVariant && deletionDisabled ? (
        <p className="mx-4 mb-4 rounded-lg border border-amber-200 bg-amber-50 px-3 py-2 text-xs leading-5 text-amber-900">
          Varyant silmeden önce formdaki bekleyen değişiklikleri kaydedin.
        </p>
      ) : null}
      <ConfirmDialog
        open={Boolean(deleteCandidate)}
        title="Varyant silinsin mi?"
        description={`${deleteCandidate?.label || "Seçilen varyant"} katalogdan ve normal ürün sorgularından kaldırılacak. Stok ve işlem geçmişi korunacak; bu işlem Admin panelinden geri alınamaz.`}
        confirmLabel="Varyantı sil"
        pending={deleting}
        error={deleteError}
        onCancel={() => {
          if (!deleting) {
            setDeleteCandidate(undefined);
            setDeleteError(undefined);
          }
        }}
        onConfirm={() => void confirmVariantDeletion()}
      />
    </section>
  );
}

// Burada varyantsız üründe kullanıcıya yalnız tek satış kaydının gerekli alanlarını gösteriyorum.
function SimpleProductFields({
  draft,
  mainSku,
  setDraft,
  fieldErrors,
}: {
  draft: DraftVariant;
  mainSku: string;
  setDraft: React.Dispatch<React.SetStateAction<DraftVariant>>;
  fieldErrors?: Record<string, string[]>;
}) {
  // Burada varyantsız ürünün satış alanlarından biri değiştiğinde yalnız tek varyant güncellemesini etkinleştiriyorum.
  const update = (field: DraftField, value: string) => setDraft((current) => ({
    ...current,
    [field]: value,
    changed: true,
  }));
  const prefix = "variants.0";

  return (
    <div className="mt-4 border-t border-border pt-4">
      <input type="hidden" name="variantCount" value="1" />
      <input type="hidden" name={`${prefix}.name`} value="Varsayılan" />
      <input type="hidden" name={`${prefix}.value`} value="Standart" />
      <input type="hidden" name={`${prefix}.sku`} value={mainSku} />
      {draft.id ? <input type="hidden" name={`${prefix}.id`} value={draft.id} /> : null}
      {draft.id && draft.expectedConcurrencyToken ? <input type="hidden" name={`${prefix}.expectedConcurrencyToken`} value={draft.expectedConcurrencyToken} /> : null}
      {draft.id && draft.changed ? <input type="hidden" name={`${prefix}.changed`} value="on" /> : null}
      {draft.id && draft.identityChanged ? <input type="hidden" name={`${prefix}.identityChanged`} value="on" /> : null}
      {draft.id && draft.schemaNormalized ? <input type="hidden" name={`${prefix}.schemaNormalized`} value="on" /> : null}
      {draft.isActive ? <input type="hidden" name={`${prefix}.isActive`} value="on" /> : null}

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        <Field label="Barkod" name={`${prefix}.barcode`} value={draft.barcode} onChange={(value) => update("barcode", value)} maxLength={100} error={fieldErrors?.[`${prefix}.barcode`]} />
        <Field label="Fiyat" name={`${prefix}.price`} value={draft.price} onChange={(value) => update("price", value)} type="number" step="0.01" min="0.01" required error={fieldErrors?.[`${prefix}.price`]} />
        <Field label="Karşılaştırma fiyatı" name={`${prefix}.compareAtPrice`} value={draft.compareAtPrice} onChange={(value) => update("compareAtPrice", value)} type="number" step="0.01" min="0" error={fieldErrors?.[`${prefix}.compareAtPrice`]} />
        <Field
          label={draft.id ? "Mevcut stok" : "Açılış stoğu"}
          name={`${prefix}.stock`}
          value={draft.stock}
          onChange={(value) => update("stock", value)}
          type="number"
          step="1"
          min="0"
          required
          readOnly={Boolean(draft.id)}
          error={fieldErrors?.[`${prefix}.stock`]}
        />
        <Field label="Materyal" name={`${prefix}.material`} value={draft.material} onChange={(value) => update("material", value)} maxLength={120} error={fieldErrors?.[`${prefix}.material`]} />
      </div>
    </div>
  );
}

// Burada çoklu seçenek adı/değeri editörünü ve oluşan çapraz varyant detaylarını birlikte sunuyorum.
function VariantOptions({
  groups,
  combinations,
  details,
  addGroup,
  updateGroupName,
  removeGroup,
  addValue,
  updateValue,
  removeValue,
  updateDetail,
  fieldErrors,
  canDeletePersistedVariant,
  deletionDisabled,
  requestVariantDeletion,
}: {
  groups: VariantOptionGroupDraft[];
  combinations: VariantCombination[];
  details: VariantDetailMap;
  addGroup: () => void;
  updateGroupName: (groupKey: string, name: string) => void;
  removeGroup: (groupKey: string) => void;
  addValue: (groupKey: string) => void;
  updateValue: (groupKey: string, valueKey: string, value: string) => void;
  removeValue: (groupKey: string, valueKey: string) => void;
  updateDetail: (combinationKey: string, detailKey: string, field: DraftField, value: string) => void;
  fieldErrors?: Record<string, string[]>;
  canDeletePersistedVariant: boolean;
  deletionDisabled: boolean;
  requestVariantDeletion: (variantId: string, label: string) => void;
}) {
  if (groups.length === 0) {
    return (
      <div className="mt-4 rounded-lg border border-dashed border-border-strong bg-surface-subtle/50 px-4 py-6 text-center">
        <p className="text-sm font-semibold text-foreground">Henüz varyant seçeneği eklenmedi</p>
        <p className="mx-auto mt-1 max-w-md text-sm leading-6 text-muted">Renk, cins veya beden gibi ilk seçeneği ekleyin; değerleriniz otomatik olarak çaprazlanacak.</p>
        <button type="button" onClick={addGroup} className="mt-4 min-h-10 rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover">
          Varyant seçeneği ekle
        </button>
      </div>
    );
  }

  const duplicateNames = groups
    .map((group) => group.name.trim())
    .filter((name, index, all) => name && all.indexOf(name) !== index);
  const variantRows = combinations.flatMap((combination) => {
    const combinationDetails = details[combination.key]?.length
      ? details[combination.key]
      : [emptyDraft(combination.key)];
    return combinationDetails.map((detail) => ({ combination, detail }));
  });

  return (
    <div className="mt-4 space-y-4 border-t border-border pt-4">
      <div className="space-y-3">
        {groups.map((group, groupIndex) => {
          const canAddValue = projectedCombinationCount(groups, group.key) <= 50;
          const duplicateValues = group.values
            .map((optionValue) => optionValue.value.trim())
            .filter((value, index, all) => value && all.indexOf(value) !== index);
          return (
            <fieldset key={group.key} className="rounded-lg border border-border bg-surface-subtle/45 p-3">
              <legend className="px-1 text-sm font-semibold text-foreground">Seçenek {groupIndex + 1}</legend>
              <div className="flex flex-col gap-3 sm:flex-row sm:items-start">
                <div className="min-w-0 flex-1">
                  <Field
                    label="Seçenek adı"
                    name={`variantOptionName.${groupIndex}`}
                    value={group.name}
                    onChange={(value) => updateGroupName(group.key, value)}
                    placeholder={groupIndex === 0 ? "Örn. Renk" : "Örn. Cins"}
                    maxLength={45}
                    required
                    error={duplicateNames.includes(group.name.trim()) ? ["Seçenek adları birbirinden farklı olmalıdır."] : undefined}
                  />
                </div>
                {!group.persisted ? (
                  <button type="button" onClick={() => removeGroup(group.key)} className="min-h-10 rounded-lg px-3 text-sm font-semibold text-danger hover:bg-red-50 sm:mt-6">
                    Seçeneği kaldır
                  </button>
                ) : (
                  <span className="mt-6 inline-flex min-h-10 items-center px-2 text-xs text-muted">Kayıtlı seçenek</span>
                )}
              </div>

              <div className="mt-3 space-y-2">
                <p className="text-sm font-medium text-foreground">Seçenek değerleri</p>
                {group.values.map((optionValue, valueIndex) => {
                  const hasPersistedDetail = valueOwnsPersistedDetail(combinations, details, group.key, optionValue.key);
                  return (
                    <div key={optionValue.key} className="flex items-start gap-2">
                      <label className="min-w-0 flex-1">
                        <span className="sr-only">{group.name || `Seçenek ${groupIndex + 1}`} değeri {valueIndex + 1}</span>
                        <input
                          type="text"
                          value={optionValue.value}
                          onChange={(event) => updateValue(group.key, optionValue.key, event.target.value)}
                          placeholder={valueIndex === 0 ? "Örn. Siyah" : "Başka bir değer"}
                          maxLength={48}
                          required
                          aria-invalid={duplicateValues.includes(optionValue.value.trim())}
                          className={inputClass}
                        />
                        {duplicateValues.includes(optionValue.value.trim()) ? <FieldError messages={["Aynı seçenek değeri tekrar eklenemez."]} /> : null}
                      </label>
                      {!optionValue.persisted && !hasPersistedDetail && group.values.length > 1 ? (
                        <button type="button" onClick={() => removeValue(group.key, optionValue.key)} className="min-h-10 rounded-lg px-3 text-sm font-semibold text-danger hover:bg-red-50">
                          Kaldır
                        </button>
                      ) : (
                        <span className="inline-flex min-h-10 items-center px-2 text-xs text-muted">{hasPersistedDetail ? "Kayıtlı" : ""}</span>
                      )}
                    </div>
                  );
                })}
                <button
                  type="button"
                  onClick={() => addValue(group.key)}
                  disabled={!canAddValue}
                  className="min-h-10 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle disabled:cursor-not-allowed disabled:opacity-50"
                >
                  + Başka bir değer ekle
                </button>
                {!canAddValue ? <p className="text-xs font-medium text-warning">En fazla 50 çapraz varyant oluşturabilirsiniz.</p> : null}
              </div>
            </fieldset>
          );
        })}
      </div>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-xs leading-5 text-muted">Backend en fazla üç seçenek adını slash ayracıyla kabul eder.</p>
        {groups.length < 3 ? (
          <button type="button" onClick={addGroup} className="min-h-10 rounded-lg border border-border-strong bg-surface-strong px-3 text-sm font-semibold text-foreground hover:bg-surface-subtle">
            + Başka bir seçenek ekle
          </button>
        ) : <span className="text-xs font-semibold text-muted">En fazla 3 seçenek</span>}
      </div>

      <div>
        <div className="mb-3 flex items-end justify-between gap-3">
          <div>
            <h3 className="text-sm font-semibold text-foreground">Varyant detayları</h3>
            <p className="mt-1 text-xs leading-5 text-muted">Seçenek değerlerinin tüm çapraz kombinasyonları aşağıda listelenir. Satış alanlarını tamamen boş bıraktığınız yeni varyantlar kayda eklenmez.</p>
          </div>
          <span className="text-xs font-semibold text-muted">{variantRows.length} varyant</span>
        </div>

        <input type="hidden" name="variantCount" value={variantRows.length} />
        {combinations.length > 0 ? (
          <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
            {variantRows.map(({ combination, detail }, index) => {
              const prefix = `variants.${index}`;
              const requiresSalesDetails = Boolean(detail.id) || !isBlankDraft(detail);
              const submittedName = detail.id && !detail.identityChanged ? detail.originalName || combination.name : combination.name;
              const submittedValue = detail.id && !detail.identityChanged ? detail.originalValue || combination.value : combination.value;
              return (
                <section key={`${combination.key}:${detail.key}`} aria-labelledby={`variant-detail-${index}`} className="bg-surface-strong p-3">
                  <div id={`variant-detail-${index}`} className="mb-3 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm font-normal leading-5 text-foreground">
                    {formatCombinationParts(combination).map((part, partIndex) => (
                      <span key={`${combination.key}-${part.name}`} className="inline-flex items-baseline gap-1">
                        {partIndex > 0 ? <span className="mr-1 text-border-strong" aria-hidden="true">·</span> : null}
                        <span className="font-bold text-foreground">{part.name}:</span>
                        <span className="font-semibold text-gray-500">{part.value}</span>
                      </span>
                    ))}
                    {detail.id && canDeletePersistedVariant ? (
                      <button
                        type="button"
                        disabled={deletionDisabled}
                        onClick={() => requestVariantDeletion(detail.id as string, `${combination.name} · ${combination.value}`)}
                        className="ml-auto inline-flex min-h-9 items-center rounded-lg border border-red-200 bg-white px-3 text-xs font-bold text-red-700 hover:bg-red-50 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus disabled:cursor-not-allowed disabled:opacity-50"
                        aria-label={`${combination.name} ${combination.value} varyantını sil`}
                      >
                        Varyantı sil
                      </button>
                    ) : null}
                  </div>
                  <input type="hidden" name={`${prefix}.name`} value={submittedName} />
                  <input type="hidden" name={`${prefix}.value`} value={submittedValue} />
                  {detail.id ? <input type="hidden" name={`${prefix}.id`} value={detail.id} /> : null}
                  {detail.id && detail.expectedConcurrencyToken ? <input type="hidden" name={`${prefix}.expectedConcurrencyToken`} value={detail.expectedConcurrencyToken} /> : null}
                  {detail.id && detail.changed ? <input type="hidden" name={`${prefix}.changed`} value="on" /> : null}
                  {detail.id && detail.identityChanged ? <input type="hidden" name={`${prefix}.identityChanged`} value="on" /> : null}
                  {detail.id && detail.schemaNormalized ? <input type="hidden" name={`${prefix}.schemaNormalized`} value="on" /> : null}
                  {detail.isActive ? <input type="hidden" name={`${prefix}.isActive`} value="on" /> : null}
                  <FieldError messages={mergeErrors(fieldErrors?.[`${prefix}.name`], fieldErrors?.[`${prefix}.value`])} />

                  <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                    <Field label="SKU" name={`${prefix}.sku`} value={detail.sku} onChange={(value) => updateDetail(combination.key, detail.key, "sku", value)} maxLength={100} required={requiresSalesDetails} error={fieldErrors?.[`${prefix}.sku`]} />
                    <Field label="Barkod" name={`${prefix}.barcode`} value={detail.barcode} onChange={(value) => updateDetail(combination.key, detail.key, "barcode", value)} maxLength={100} error={fieldErrors?.[`${prefix}.barcode`]} />
                    <Field label="Fiyat" name={`${prefix}.price`} value={detail.price} onChange={(value) => updateDetail(combination.key, detail.key, "price", value)} type="number" step="0.01" min="0.01" required={requiresSalesDetails} error={fieldErrors?.[`${prefix}.price`]} />
                    <Field label="Karşılaştırma fiyatı" name={`${prefix}.compareAtPrice`} value={detail.compareAtPrice} onChange={(value) => updateDetail(combination.key, detail.key, "compareAtPrice", value)} type="number" step="0.01" min="0" error={fieldErrors?.[`${prefix}.compareAtPrice`]} />
                    <Field
                      label={detail.id ? "Mevcut stok" : "Açılış stoğu"}
                      name={`${prefix}.stock`}
                      value={detail.stock}
                      onChange={(value) => updateDetail(combination.key, detail.key, "stock", value)}
                      type="number"
                      step="1"
                      min="0"
                      required={requiresSalesDetails}
                      readOnly={Boolean(detail.id)}
                      error={fieldErrors?.[`${prefix}.stock`]}
                    />
                    <Field label="Materyal" name={`${prefix}.material`} value={detail.material} onChange={(value) => updateDetail(combination.key, detail.key, "material", value)} maxLength={120} error={fieldErrors?.[`${prefix}.material`]} />
                  </div>
                </section>
              );
            })}
          </div>
        ) : (
          <div className="rounded-xl border border-dashed border-border-strong bg-surface-subtle/40 px-4 py-6 text-center text-sm text-muted">
            Her seçenek için en az bir değer eklediğinizde varyant detayları burada oluşacak.
          </div>
        )}
      </div>
    </div>
  );
}

// Burada birleşik backend ad/değer metinlerini belirgin “ad: değer” başlık parçalarına dönüştürüyorum.
function formatCombinationParts(combination: VariantCombination): Array<{ name: string; value: string }> {
  const names = splitComposite(combination.name);
  const values = splitComposite(combination.value);
  return names.map((name, index) => ({ name, value: values[index] || "—" }));
}

// Burada yeni bir değer eklendiğinde oluşacak kartezyen varyant sayısını form üst sınırıyla karşılaştırıyorum.
function projectedCombinationCount(groups: VariantOptionGroupDraft[], targetGroupKey: string): number {
  return groups.reduce(
    (total, group) => total * Math.max(1, group.values.length + (group.key === targetGroupKey ? 1 : 0)),
    1,
  );
}

// Burada belirli bir seçenek değerinin kaydedilmiş varyant kimliği taşıyan kombinasyonda kullanılıp kullanılmadığını buluyorum.
function valueOwnsPersistedDetail(
  combinations: VariantCombination[],
  details: VariantDetailMap,
  groupKey: string,
  valueKey: string,
): boolean {
  return combinations.some(
    (combination) =>
      Boolean(details[combination.key]?.some((detail) => detail.id)) &&
      combination.selections.some((selection) => selection.groupKey === groupKey && selection.valueKey === valueKey),
  );
}

// Burada satış bilgisi girilmemiş yeni çapraz varyantın tarayıcı doğrulamasını tetiklemeden payload dışında kalmasını sağlıyorum.
function isBlankDraft(detail: DraftVariant): boolean {
  const stock = detail.stock.trim();
  return [detail.sku, detail.price, detail.compareAtPrice, detail.barcode, detail.material]
    .every((value) => !value.trim())
    && (!stock || Number(stock) === 0);
}

// Burada varyant alanlarının etiket, yardım metni ve hata ilişkisini kontrollü inputlarla kuruyorum.
function Field({
  label,
  name,
  value,
  onChange,
  type = "text",
  step,
  min,
  maxLength,
  required,
  readOnly,
  help,
  placeholder,
  error,
}: {
  label: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  type?: string;
  step?: string;
  min?: string;
  maxLength?: number;
  required?: boolean;
  readOnly?: boolean;
  help?: string;
  placeholder?: string;
  error?: string[];
}) {
  const errorId = `${name}-error`;
  const helpId = `${name}-help`;
  return (
    <label className="block text-sm font-medium text-foreground">
      {label}{required ? " *" : ""}
      <input
        name={name}
        type={type}
        step={step}
        min={min}
        maxLength={maxLength}
        required={required}
        readOnly={readOnly}
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : help ? helpId : undefined}
        className={`${inputClass} mt-1.5 read-only:cursor-not-allowed read-only:bg-surface-subtle read-only:text-muted`}
      />
      {help ? <span id={helpId} className="mt-1 block text-xs font-normal leading-5 text-muted">{help}</span> : null}
      <FieldError id={errorId} messages={error} />
    </label>
  );
}

// Burada aynı görünen alana ait birden fazla backend hata grubunu birlikte gösteriyorum.
function mergeErrors(...groups: Array<string[] | undefined>): string[] | undefined {
  const messages = groups.flatMap((group) => group || []);
  return messages.length > 0 ? [...new Set(messages)] : undefined;
}

// Burada ilgili varyant alanının hata mesajlarını kontrolün hemen altında görünür tutuyorum.
function FieldError({ id, messages }: { id?: string; messages?: string[] }) {
  return messages ? <span id={id} className="mt-1 block text-xs font-semibold text-danger">{messages.join(" ")}</span> : null;
}
