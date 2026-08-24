import Link from "next/link";

export default function CurrentAccountNotFound() {
  return (
    <div className="mx-auto max-w-lg rounded-xl border border-border bg-surface p-6 text-center">
      <h1 className="text-lg font-semibold">Cari hesap bulunamadı</h1>
      <p className="mt-2 text-sm text-muted">Kayıt silinmiş veya bağlantı güncelliğini yitirmiş olabilir.</p>
      <Link href="/accounting/current-accounts" className="mt-5 inline-flex min-h-10 items-center rounded-lg bg-primary px-4 text-sm font-semibold text-white">Cari hesaplara dön</Link>
    </div>
  );
}
