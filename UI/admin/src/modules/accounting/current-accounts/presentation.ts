export function currentAccountTypeLabel(type: number): string {
  if (type === 1) return "Müşteri";
  if (type === 2) return "Tedarikçi";
  if (type === 3) return "Müşteri ve tedarikçi";
  return "Bilinmeyen cari türü";
}

export function currentAccountTypeClass(type: number): string {
  return type === 1
    ? "border-blue-200 bg-blue-50 text-blue-800"
    : type === 2
      ? "border-amber-200 bg-amber-50 text-amber-900"
      : type === 3
        ? "border-violet-200 bg-violet-50 text-violet-900"
        : "border-border bg-surface-subtle text-muted";
}
