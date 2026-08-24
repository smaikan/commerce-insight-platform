import type { TreasuryView } from "./types";
export function parseTreasuryView(params: Record<string, string | string[] | undefined>): TreasuryView { const raw = Array.isArray(params.view) ? params.view[0] : params.view; return raw === "manual" || raw === "transfer" ? raw : "accounts"; }
export function treasuryHref(view: TreasuryView): string { return view === "accounts" ? "/accounting/treasury" : `/accounting/treasury?view=${view}`; }
