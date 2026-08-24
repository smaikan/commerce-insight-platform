export type VariantOptionValueDraft = {
  key: string;
  value: string;
  persisted: boolean;
};

export type VariantOptionGroupDraft = {
  key: string;
  name: string;
  persisted: boolean;
  values: VariantOptionValueDraft[];
};

export type VariantCombination = {
  key: string;
  name: string;
  value: string;
  selections: Array<{ groupKey: string; valueKey: string }>;
};

type VariantIdentity = { name: string; value: string };

// Burada karışık legacy şemalarda ilk kayda güvenmeden en zengin şemayı ve ad bazlı değerleri yeniden kuruyorum.
export function groupsFromVariants(variants: VariantIdentity[]): VariantOptionGroupDraft[] {
  if (variants.length === 0) return [];

  const schemas = variants.map((variant) => splitComposite(variant.name).filter(Boolean));
  const richestSchema = schemas.reduce<string[]>(
    (richest, schema) => schema.length > richest.length ? schema : richest,
    [],
  );
  const names = schemas.reduce<string[]>((ordered, schema) => {
    schema.forEach((name) => {
      if (ordered.length < 3 && !ordered.includes(name)) ordered.push(name);
    });
    return ordered;
  }, [...richestSchema.slice(0, 3)]);

  return names.map((name, groupIndex) => {
    const values = variants
      .map((variant) => {
        const variantNames = splitComposite(variant.name);
        const variantValues = splitComposite(variant.value);
        const matchingIndex = variantNames.indexOf(name);
        return matchingIndex >= 0 ? variantValues[matchingIndex] || "" : "";
      })
      .filter((value, index, all) => value && all.indexOf(value) === index)
      .map((value, valueIndex) => ({
        key: `group-${groupIndex}-value-${valueIndex}`,
        value,
        persisted: true,
      }));

    return {
      key: `group-${groupIndex}`,
      name,
      persisted: true,
      values,
    };
  });
}

// Burada seçenek gruplarının kartezyen çarpımını backend'in birleşik ad/değer sözleşmesine dönüştürüyorum.
export function buildVariantCombinations(groups: VariantOptionGroupDraft[]): VariantCombination[] {
  if (groups.length === 0 || groups.some((group) => group.values.length === 0)) return [];

  return groups.reduce<VariantCombination[]>(
    (current, group) =>
      current.flatMap((combination) =>
        group.values.map((optionValue) => {
          const names = combination.name ? [...splitComposite(combination.name), group.name] : [group.name];
          const values = combination.value ? [...splitComposite(combination.value), optionValue.value] : [optionValue.value];
          const selections = [
            ...combination.selections,
            { groupKey: group.key, valueKey: optionValue.key },
          ];
          return {
            key: selections.map((selection) => selection.valueKey).join("::"),
            name: names.join(" / "),
            value: values.join(" / "),
            selections,
          };
        }),
      ),
    [{ key: "", name: "", value: "", selections: [] }],
  );
}

// Burada birleşik varyant metnini backend ile aynı slash ayırıcı ve trim kuralıyla parçalıyorum.
export function splitComposite(value: string): string[] {
  return value.split("/").map((part) => part.trim());
}
