import { PageHeader } from "@/modules/admin-shell/components/page-header";

const workAreas = [
  {
    name: "Ürün yönetimi",
    description: "Ürün listesi, filtreler ve sayfa içindeki yeni ürün oluşturma aksiyonu.",
    status: "Sıradaki dilim",
    icon: "box",
  },
  {
    name: "Sipariş yönetimi",
    description: "E-ticaret sipariş listesi, durum ve detay görünümü.",
    status: "Sıradaki dilim",
    icon: "order",
  },
  {
    name: "Diğer operasyonlar",
    description: "Stok, müşteri, kupon ve muhasebe modülleri kapsam onayıyla açılacak.",
    status: "Planlandı",
    icon: "layers",
  },
];

function WorkAreaIcon({ name }: { name: string }) {
  const path =
    name === "box"
      ? "M4 7.5 12 3l8 4.5v9L12 21l-8-4.5v-9Zm0 0 8 4.5m8-4.5L12 12m0 9v-9"
      : name === "order"
        ? "M6 4h12v16H6zM9 8h6M9 12h6M9 16h4"
        : "M12 3 3 8l9 5 9-5-9-5Zm-9 9 9 5 9-5M3 16l9 5 9-5";

  return (
    <span className="flex size-10 shrink-0 items-center justify-center rounded-lg border border-primary/20 bg-primary-soft text-primary">
      <svg aria-hidden="true" viewBox="0 0 24 24" className="size-5 fill-none stroke-current stroke-[1.8]">
        <path d={path} strokeLinecap="round" strokeLinejoin="round" />
      </svg>
    </span>
  );
}

export function DashboardOverview() {
  return (
    <div className="mx-auto w-full max-w-screen-2xl">
      <PageHeader
        title="Genel Bakış"
        description="Yönetim panelinin operasyon alanlarına buradan erişeceksiniz. Gerçek veri sözleşmesi olmayan metrikler gösterilmez."
      />

      <section aria-labelledby="work-areas-title" className="overflow-hidden rounded-xl border border-border bg-surface shadow-sm">
        <div className="border-b border-border bg-primary-soft/70 px-4 py-4 sm:px-5">
          <h2 id="work-areas-title" className="text-base font-semibold text-foreground">
            Çalışma alanları
          </h2>
          <p className="mt-1 text-sm text-muted">Phase 1 kapsamındaki modüllerin uygulama sırası.</p>
        </div>

        <ul className="divide-y divide-border">
          {workAreas.map((area) => (
            <li key={area.name} className="flex flex-col gap-3 px-4 py-4 sm:flex-row sm:items-center sm:gap-4 sm:px-5">
              <WorkAreaIcon name={area.icon} />
              <div className="min-w-0 flex-1">
                <h3 className="text-sm font-semibold text-foreground">{area.name}</h3>
                <p className="mt-1 text-sm leading-5 text-muted">{area.description}</p>
              </div>
              <span className="w-fit shrink-0 rounded-full border border-border bg-surface-subtle px-2.5 py-1 text-xs font-medium text-muted">
                {area.status}
              </span>
            </li>
          ))}
        </ul>
      </section>

      <section aria-labelledby="metrics-title" className="mt-6">
        <h2 id="metrics-title" className="text-base font-semibold text-foreground">
          Operasyon özeti
        </h2>
        <div className="mt-3 flex items-start gap-3 rounded-xl border border-primary/25 bg-primary-soft px-4 py-4 sm:px-5">
          <span className="flex size-8 shrink-0 items-center justify-center rounded-lg bg-primary text-sm font-semibold text-white" aria-hidden="true">
            i
          </span>
          <div>
            <p className="text-sm font-semibold text-foreground">Henüz genel dashboard veri sözleşmesi bulunmuyor.</p>
            <p className="mt-1 max-w-3xl text-sm leading-6 text-muted">
              Satış, sipariş veya stok rakamları yalnızca kapsamı ve dönemi tanımlı bir API yanıtı sağlandığında burada gösterilecek.
            </p>
          </div>
        </div>
      </section>
    </div>
  );
}
