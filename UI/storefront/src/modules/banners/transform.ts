import { BANNER_SECTION_CONFIGS } from "./section-config";
import type { BannerSection, BannerSectionItem, BannerSectionKey } from "./types";

export const MAX_PUBLIC_BANNER_ITEMS = 5;

// Burada halka açık cevabı aktif kayıtlarla, sözleşme sırasıyla ve en fazla beş öğeyle sınırlandırıyorum.
export function normalizePublicBannerSection(
  sectionKey: BannerSectionKey,
  section: BannerSection,
): BannerSection {
  const config = BANNER_SECTION_CONFIGS[sectionKey];
  const activeItems = section.items.filter((item) => item.isActive);
  const normalizedItems = activeItems.map((item) => ({
    ...item,
    isMain: config.isMain ? item.isMain : false,
  }));

  return {
    ...section,
    items: sortPublicBannerItems(sectionKey, normalizedItems).slice(0, MAX_PUBLIC_BANNER_ITEMS),
  };
}

// Burada ana bölümün seçili kaydını önce, diğer bütün kayıtları displayOrder ve kararlı anahtar sırasıyla diziyorum.
export function sortPublicBannerItems(
  sectionKey: BannerSectionKey,
  items: readonly BannerSectionItem[],
): BannerSectionItem[] {
  const isMainSection = BANNER_SECTION_CONFIGS[sectionKey].isMain;

  return [...items].sort((left, right) => {
    const mainDifference = isMainSection ? Number(right.isMain) - Number(left.isMain) : 0;
    return mainDifference || left.displayOrder - right.displayOrder || left.key.localeCompare(right.key);
  });
}
