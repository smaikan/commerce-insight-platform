"use client";

// Burada kupon listeleme hatasında sayfayı güvenle yeniden deneme seçeneği sunuyorum.
export default function CouponsError({ reset }: { error: Error; reset: () => void }) { return <div className="mx-auto max-w-2xl rounded-xl border border-danger/30 bg-danger/10 p-5 text-danger"><h1 className="text-lg font-semibold">İndirimler yüklenemedi</h1><p className="mt-2 text-sm">Kupon verilerine şu anda ulaşılamıyor. Lütfen bağlantıyı ve yetkinizi kontrol edip tekrar deneyin.</p><button type="button" onClick={reset} className="mt-4 inline-flex min-h-10 items-center rounded-lg bg-danger px-4 text-sm font-semibold text-white">Tekrar dene</button></div>; }
