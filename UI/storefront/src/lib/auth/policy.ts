// Burada giriş sonrası dönüş hedefini yalnızca aynı origin içindeki güvenli ve döngü oluşturmayan göreli yollarla sınırlandırıyorum.
export function safeReturnTo(value: FormDataEntryValue | string | null | undefined): string {
  if (typeof value !== "string") return "/";

  const candidate = value.trim();
  if (
    !candidate.startsWith("/") ||
    candidate.startsWith("//") ||
    candidate.includes("\\") ||
    /[\u0000-\u001F\u007F]/.test(candidate)
  ) {
    return "/";
  }

  const pathname = candidate.split(/[?#]/, 1)[0].replace(/\/+$/, "") || "/";
  return pathname === "/login" || pathname === "/register" ? "/" : candidate;
}
