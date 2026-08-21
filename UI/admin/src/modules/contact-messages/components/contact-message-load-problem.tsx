import Link from "next/link";
import type { ApiProblem } from "@/lib/api/problem";

// Burada Server Component okuma hatalarını status/code ayrımıyla güvenli ve kalıcı bir permission/limit/error durumuna dönüştürüyorum.
export function ContactMessageLoadProblem({ problem, retryHref }: { problem: ApiProblem; retryHref: string }) {
  const content = problem.status === 401
    ? { title: "Oturum sona erdi", description: "İletişim mesajlarına erişmek için yeniden giriş yapın.", action: "Giriş yap", href: `/login?returnTo=${encodeURIComponent(retryHref)}&reason=session_required` }
    : problem.status === 403
      ? { title: "Bu alan için yetkiniz yok", description: "İletişim mesajları yalnız aktif Admin yetkisiyle görüntülenebilir.", action: "Genel bakışa dön", href: "/dashboard" }
      : problem.status === 429
        ? { title: "İstek sınırına ulaşıldı", description: problem.retryAfter ? `API ${problem.retryAfter} sonra yeniden denemenizi istiyor.` : "Kısa bir süre sonra yeniden deneyin.", action: "Tekrar dene", href: retryHref }
        : { title: "İletişim mesajı verisi alınamadı", description: "API geçici olarak yanıt vermiyor veya güvenli olmayan bir yanıt döndürdü.", action: "Tekrar dene", href: retryHref };
  return (
    <section role="alert" className="rounded-xl border border-danger/30 bg-surface p-6">
      <h1 className="text-xl font-semibold text-foreground">{content.title}</h1>
      <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">{content.description}</p>
      {problem.traceId ? <p className="mt-2 font-mono text-xs text-muted">İz: {problem.traceId}</p> : null}
      <Link href={content.href} className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white hover:bg-primary-hover focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus">{content.action}</Link>
    </section>
  );
}
