// Burada sınıflandırma adlarını mevcut geçici URL sözleşmesi için Türkçe karakterleri kapsayan kararlı ASCII segmentlere dönüştürüyorum.
export function classificationSegmentFromName(name: string): string {
  return name
    .trim()
    .toLocaleLowerCase("tr-TR")
    .replaceAll("ı", "i")
    .replaceAll("ğ", "g")
    .replaceAll("ü", "u")
    .replaceAll("ş", "s")
    .replaceAll("ö", "o")
    .replaceAll("ç", "c")
    .normalize("NFKD")
    .replace(/[\u0300-\u036f]/g, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

// Burada teknik API URL alanını dilsel Türkçe büyük-küçük harf dönüşümüne sokmadan lowercase canonical segmente indiriyorum.
export function catalogSegmentFromApiUrl(value: string): string {
  return value.trim().toLowerCase();
}
