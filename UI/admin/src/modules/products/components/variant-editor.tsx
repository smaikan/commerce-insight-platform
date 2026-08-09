"use client";

import { useState } from "react";
import type { ProductVariant } from "@/modules/products/types";
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
  changed: boolean;
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

const inputClass =
  "min-h-11 w-full rounded-lg border border-border-strong bg-surface-strong px-3 text-sm text-foreground outline-none focus:border-primary sm:min-h-9";

// Burada API varyantını belirli bir çapraz kombinasyonun satış detaylarına dönüştürüyorum.
function toDraft(variant: ProductVariant, key: string): DraftVariant {
  return {
    key,
    id: variant.id,
    changed: false,
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
): Record<string, DraftVariant> {
  const details: Record<string, DraftVariant> = {};
  variants.forEach((variant, index) => {
    const values = splitComposite(variant.value);
    const combination = combinations.find((item) => {
      const combinationValues = splitComposite(item.value);
      return combinationValues.length === values.length && combinationValues.every((value, partIndex) => value === values[partIndex]);
    }) || combinations[index];
    if (combination) details[combination.key] = toDraft(variant, combination.key);
  });
  return details;
}

// Burada seçenek yapısı değiştiğinde ortak seçimleri eşleyip mevcut varyant kimliklerini ilk uygun kombinasyona taşıyorum.
function rebaseDetails(
  previousCombinations: VariantCombination[],
  nextCombinations: VariantCombination[],
  current: Record<string, DraftVariant>,
): Record<string, DraftVariant> {
  const next: Record<string, DraftVariant> = {};
  const usedPreviousKeys = new Set<string>();

  nextCombinations.forEach((combination) => {
    if (current[combination.key] && !usedPreviousKeys.has(combination.key)) {
      const previousCombination = previousCombinations.find((item) => item.key === combination.key);
      const combinationChanged = previousCombination
        ? previousCombination.name !== combination.name || previousCombination.value !== combination.value
        : false;
      next[combination.key] = {
        ...current[combination.key],
        key: combination.key,
        changed: current[combination.key].changed || Boolean(current[combination.key].id && combinationChanged),
      };
      usedPreviousKeys.add(combination.key);
      return;
    }

    const candidates = previousCombinations.filter(
      (previous) => current[previous.key] && !usedPreviousKeys.has(previous.key) && combinationsAreCompatible(previous, combination),
    );
    const matched = candidates.find((candidate) => current[candidate.key].id) || candidates[0];
    if (matched) {
      next[combination.key] = {
        ...current[matched.key],
        key: combination.key,
        changed: current[matched.key].changed || Boolean(current[matched.key].id),
      };
      usedPreviousKeys.add(matched.key);
    } else {
      next[combination.key] = emptyDraft(combination.key);
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
  current: Record<string, DraftVariant>,
  affectedKeys?: Set<string>,
): Record<string, DraftVariant> {
  return Object.fromEntries(
    Object.entries(current).map(([key, detail]) => [
      key,
      detail.id && (!affectedKeys || affectedKeys.has(key)) ? { ...detail, changed: true } : detail,
    ]),
  );
}

// Burada varyantsız tek ürün ile en fazla üç seçenekli çapraz varyant akışını aynı bölümde yönetiyorum.
export function VariantEditor({
  variants,
  mode,
  initialHasVariants,
  initialMainSku,
  fieldErrors,
}: {
  variants: ProductVariant[];
  mode: "create" | "edit";
  initialHasVariants: boolean;
  initialMainSku: string;
  fieldErrors?: Record<string, string[]>;
}) {
  const initialGroups = initialHasVariants ? groupsFromVariants(variants) : [];
  const initialCombinations = buildVariantCombinations(initialGroups);
  const initialDetails = detailsFromVariants(variants, initialCombinations);
  const initialSimpleDraft = variants[0] ? toDraft(variants[0], "variant-new-0") : emptyDraft();

  const [hasVariants, setHasVariants] = useState(initialHasVariants);
  const [mainSku, setMainSku] = useState(initialMainSku);
  const [simpleDraft, setSimpleDraft] = useState<DraftVariant>(initialSimpleDraft);
  const [groups, setGroups] = useState<VariantOptionGroupDraft[]>(initialGroups);
  const [details, setDetails] = useState<Record<string, DraftVariant>>(initialDetails);
  const combinations = buildVariantCombinations(groups);
  const preventsDisabling = mode === "edit" && initialHasVariants && variants.length > 1;

  // Burada varyant modunu değiştirirken çoklu mevcut varyantların yanlışlıkla gizlenmesini engelliyorum.
  const changeVariantMode = (checked: boolean) => {
    if (!checked && preventsDisabling) return;
    if (!checked && combinations[0]) {
      const source = details[combinations[0].key] || emptyDraft();
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
  const updateDetail = (combinationKey: string, field: DraftField, value: string) => {
    setDetails((current) => ({
      ...current,
      [combinationKey]: {
        ...(current[combinationKey] || emptyDraft(combinationKey)),
        [field]: value,
        changed: true,
      },
    }));
  };

  return (
    <section aria-labelledby="variants-title" className="rounded-xl border border-border bg-surface-strong">
      <div className="flex flex-col gap-3 border-b border-border px-4 py-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h2 id="variants-title" className="text-base font-semibold text-foreground">Fiyatlandırma ve varyantlar</h2>
          <p className="mt-0.5 text-xs leading-5 text-muted">
            Tek ürün bilgilerini girin veya seçenek değerlerini çaprazlayarak varyantlar oluşturun.
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
            Birden fazla kayıtlı varyant bulunduğu için varyant modu kapatılamaz. Backend&apos;de birleştirme veya silme desteği eklendiğinde bu kilit kaldırılabilir.
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
      {draft.id && draft.changed ? <input type="hidden" name={`${prefix}.changed`} value="on" /> : null}
      {draft.isActive ? <input type="hidden" name={`${prefix}.isActive`} value="on" /> : null}

      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
        <Field label="Barkod" name={`${prefix}.barcode`} value={draft.barcode} onChange={(value) => update("barcode", value)} maxLength={100} error={fieldErrors?.[`${prefix}.barcode`]} />
        <Field label="Fiyat" name={`${prefix}.price`} value={draft.price} onChange={(value) => update("price", value)} type="number" step="0.01" min="0.01" required error={fieldErrors?.[`${prefix}.price`]} />
        <Field label="Karşılaştırma fiyatı" name={`${prefix}.compareAtPrice`} value={draft.compareAtPrice} onChange={(value) => update("compareAtPrice", value)} type="number" step="0.01" min="0" error={fieldErrors?.[`${prefix}.compareAtPrice`]} />
        <Field label="Stok" name={`${prefix}.stock`} value={draft.stock} onChange={(value) => update("stock", value)} type="number" step="1" min="0" required error={fieldErrors?.[`${prefix}.stock`]} />
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
}: {
  groups: VariantOptionGroupDraft[];
  combinations: VariantCombination[];
  details: Record<string, DraftVariant>;
  addGroup: () => void;
  updateGroupName: (groupKey: string, name: string) => void;
  removeGroup: (groupKey: string) => void;
  addValue: (groupKey: string) => void;
  updateValue: (groupKey: string, valueKey: string, value: string) => void;
  removeValue: (groupKey: string, valueKey: string) => void;
  updateDetail: (combinationKey: string, field: DraftField, value: string) => void;
  fieldErrors?: Record<string, string[]>;
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
          <span className="text-xs font-semibold text-muted">{combinations.length} varyant</span>
        </div>

        <input type="hidden" name="variantCount" value={combinations.length} />
        {combinations.length > 0 ? (
          <div className="divide-y divide-border overflow-hidden rounded-lg border border-border">
            {combinations.map((combination, index) => {
              const prefix = `variants.${index}`;
              const detail = details[combination.key] || emptyDraft(combination.key);
              const requiresSalesDetails = Boolean(detail.id) || !isBlankDraft(detail);
              return (
                <section key={combination.key} aria-labelledby={`variant-detail-${index}`} className="bg-surface-strong p-3">
                  <div id={`variant-detail-${index}`} className="mb-3 flex flex-wrap items-center gap-x-2 gap-y-1 text-sm font-normal leading-5 text-foreground">
                    {formatCombinationParts(combination).map((part, partIndex) => (
                      <span key={`${combination.key}-${part.name}`} className="inline-flex items-baseline gap-1">
                        {partIndex > 0 ? <span className="mr-1 text-border-strong" aria-hidden="true">·</span> : null}
                        <span className="font-bold text-foreground">{part.name}:</span>
                        <span className="font-semibold text-gray-500">{part.value}</span>
                      </span>
                    ))}
                  </div>
                  <input type="hidden" name={`${prefix}.name`} value={combination.name} />
                  <input type="hidden" name={`${prefix}.value`} value={combination.value} />
                  {detail.id ? <input type="hidden" name={`${prefix}.id`} value={detail.id} /> : null}
                  {detail.id && detail.changed ? <input type="hidden" name={`${prefix}.changed`} value="on" /> : null}
                  {detail.isActive ? <input type="hidden" name={`${prefix}.isActive`} value="on" /> : null}
                  <FieldError messages={mergeErrors(fieldErrors?.[`${prefix}.name`], fieldErrors?.[`${prefix}.value`])} />

                  <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
                    <Field label="SKU" name={`${prefix}.sku`} value={detail.sku} onChange={(value) => updateDetail(combination.key, "sku", value)} maxLength={100} required={requiresSalesDetails} error={fieldErrors?.[`${prefix}.sku`]} />
                    <Field label="Barkod" name={`${prefix}.barcode`} value={detail.barcode} onChange={(value) => updateDetail(combination.key, "barcode", value)} maxLength={100} error={fieldErrors?.[`${prefix}.barcode`]} />
                    <Field label="Fiyat" name={`${prefix}.price`} value={detail.price} onChange={(value) => updateDetail(combination.key, "price", value)} type="number" step="0.01" min="0.01" required={requiresSalesDetails} error={fieldErrors?.[`${prefix}.price`]} />
                    <Field label="Karşılaştırma fiyatı" name={`${prefix}.compareAtPrice`} value={detail.compareAtPrice} onChange={(value) => updateDetail(combination.key, "compareAtPrice", value)} type="number" step="0.01" min="0" error={fieldErrors?.[`${prefix}.compareAtPrice`]} />
                    <Field label="Stok" name={`${prefix}.stock`} value={detail.stock} onChange={(value) => updateDetail(combination.key, "stock", value)} type="number" step="1" min="0" required={requiresSalesDetails} error={fieldErrors?.[`${prefix}.stock`]} />
                    <Field label="Materyal" name={`${prefix}.material`} value={detail.material} onChange={(value) => updateDetail(combination.key, "material", value)} maxLength={120} error={fieldErrors?.[`${prefix}.material`]} />
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
  details: Record<string, DraftVariant>,
  groupKey: string,
  valueKey: string,
): boolean {
  return combinations.some(
    (combination) =>
      Boolean(details[combination.key]?.id) &&
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
        value={value}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        aria-invalid={Boolean(error)}
        aria-describedby={error ? errorId : help ? helpId : undefined}
        className={`${inputClass} mt-1.5`}
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
