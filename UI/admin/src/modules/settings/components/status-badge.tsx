// Burada ayar kaynaklarının aktiflik durumunu metin ve semantik renkle birlikte gösteriyorum.
export function SettingsStatusBadge({ active }: { active: boolean }) {
  return <span className={`inline-flex rounded-md border px-2 py-1 text-xs font-semibold ${active ? "border-success/25 bg-success/10 text-success" : "border-border-strong bg-surface-subtle text-muted"}`}>{active ? "Aktif" : "Pasif"}</span>;
}
