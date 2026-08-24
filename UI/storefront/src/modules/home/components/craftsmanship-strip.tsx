// Burada mağazanın kalite, malzeme ve usta işçilik standartlarını anlatan 3 sütunlu lüks vitrin bandını sunuyorum.
export function CraftsmanshipStrip() {
  const pillars = [
    {
      badge: "DAYANIKLI & PARLAK",
      title: "18K Altın & Rodyum Kaplama",
      description:
        "Özel koruyucu kaplama teknolojisi ile kararmaya, solmaya ve günlük aşınmaya karşı üstün direnç.",
      icon: (
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-6 text-brand-700" fill="none" stroke="currentColor" strokeWidth="1.25" strokeLinecap="round" strokeLinejoin="round">
          <polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2" />
        </svg>
      ),
    },
    {
      badge: "GÜVENLİ & HASSAS",
      title: "Antialerjik & Cilt Dostu",
      description:
        "Nikel ve kurşun içermeyen hipoalerjenik 316L paslanmaz çelik ve pirinç alaşımları ile hassas ciltlere tam uyum.",
      icon: (
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-6 text-brand-700" fill="none" stroke="currentColor" strokeWidth="1.25" strokeLinecap="round" strokeLinejoin="round">
          <path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" />
          <path d="m9 12 2 2 4-4" />
        </svg>
      ),
    },
    {
      badge: "İMZALI SUNUM",
      title: "Özel Lüks Hediye Paketi",
      description:
        "Her sipariş; ELEVEN imzalı sert kutu, mikrofiber saklama kesesi ve garanti belgesiyle özenle hazırlanır.",
      icon: (
        <svg aria-hidden="true" viewBox="0 0 24 24" className="size-6 text-brand-700" fill="none" stroke="currentColor" strokeWidth="1.25" strokeLinecap="round" strokeLinejoin="round">
          <rect width="20" height="14" x="2" y="7" rx="2" ry="2" />
          <path d="M16 21V5a2 2 0 0 0-2-2h-4a2 2 0 0 0-2 2v16" />
        </svg>
      ),
    },
  ];

  return (
    <section
      aria-labelledby="craftsmanship-heading"
      className="home-shell my-10 sm:my-14"
    >
      <div className="rounded-3xl border border-line/80 bg-gradient-to-b from-surface via-surface to-surface-subtle/50 p-8 sm:p-12 shadow-sm">
        <div className="mx-auto max-w-2xl text-center mb-10">
          <span className="text-xs font-bold uppercase tracking-[0.2em] text-brand-700">
            ELEVEN KALİTE STANDARTLARI
          </span>
          <h2
            id="craftsmanship-heading"
            className="mt-2 text-2xl font-bold tracking-tight text-ink sm:text-3xl"
          >
            Ayrıntılarda Saklı Zarafet ve Dayanıklılık
          </h2>
          <p className="mt-2 text-sm text-ink-muted leading-relaxed">
            Tasarımdan son dokunuşa kadar her aşamada en kaliteli malzemeleri ve titiz işçiliği bir araya getiriyoruz.
          </p>
        </div>

        <div className="grid gap-6 md:grid-cols-3">
          {pillars.map((pillar) => (
            <div
              key={pillar.title}
              className="group relative flex flex-col justify-between rounded-2xl border border-line/60 bg-surface p-7 transition-all duration-300 hover:-translate-y-1 hover:border-brand-700/40 hover:shadow-md"
            >
              <div>
                <div className="flex size-12 items-center justify-center rounded-xl bg-surface-subtle border border-line/80 mb-5 transition-transform duration-300 group-hover:scale-110">
                  {pillar.icon}
                </div>
                <span className="text-[0.6875rem] font-bold tracking-widest text-brand-700 block mb-1">
                  {pillar.badge}
                </span>
                <h3 className="text-lg font-bold text-ink mb-2 tracking-tight">
                  {pillar.title}
                </h3>
                <p className="text-xs sm:text-sm text-ink-muted leading-relaxed">
                  {pillar.description}
                </p>
              </div>

              <div className="mt-6 pt-4 border-t border-line/40 flex items-center gap-1.5 text-xs font-semibold text-brand-700 group-hover:text-brand-950">
                <span>Standartları İncele</span>
                <span aria-hidden="true" className="transition-transform group-hover:translate-x-1">&rarr;</span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </section>
  );
}
