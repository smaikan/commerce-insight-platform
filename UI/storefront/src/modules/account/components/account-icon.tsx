import type { AccountDestination } from "@/modules/account/navigation";

// Burada hesap hedeflerini tek renk, ortak ölçü ve çizgi kalınlığındaki hafif SVG simgelerle ayırt ediyorum.
export function AccountIcon({ icon, className = "size-5" }: { icon: AccountDestination["icon"]; className?: string }) {
  const common = {
    "aria-hidden": true,
    viewBox: "0 0 24 24",
    className,
    fill: "none",
    stroke: "currentColor",
    strokeWidth: 1.65,
    strokeLinecap: "round" as const,
    strokeLinejoin: "round" as const,
  };

  if (icon === "orders") return <svg {...common}><path d="M5 4h14v16H5zM8 8h8M8 12h8M8 16h5" /></svg>;
  if (icon === "returns") return <svg {...common}><path d="M8 7H4v-4M4 7a8 8 0 1 1-1 8" /><path d="m9 12 2 2 4-4" /></svg>;
  if (icon === "addresses") return <svg {...common}><path d="M12 21s6-5.4 6-11a6 6 0 1 0-12 0c0 5.6 6 11 6 11Z" /><circle cx="12" cy="10" r="2" /></svg>;
  if (icon === "favorites") return <svg {...common}><path d="M20.8 8.6c0 5.2-8.8 10.2-8.8 10.2S3.2 13.8 3.2 8.6A4.4 4.4 0 0 1 12 8a4.4 4.4 0 0 1 8.8.6Z" /></svg>;
  if (icon === "security") return <svg {...common}><path d="M12 3 5.5 5.8v5.1c0 4.2 2.5 7.8 6.5 10.1 4-2.3 6.5-5.9 6.5-10.1V5.8L12 3Z" /><path d="m9.3 12 1.8 1.8 3.8-4" /></svg>;
  return <svg {...common}><path d="M4 4h6v6H4zM14 4h6v6h-6zM4 14h6v6H4zM14 14h6v6h-6z" /></svg>;
}
