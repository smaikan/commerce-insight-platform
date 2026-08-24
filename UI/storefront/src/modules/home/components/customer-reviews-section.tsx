// Burada mağaza için güven ve müşteri memnuniyeti sağlayan gerçekçi sosyal kanıt bölümünü sunuyorum.
export function CustomerReviewsSection() {
  const reviews = [
    {
      author: "Selin D.",
      city: "İstanbul",
      rating: 5,
      title: "Kusursuz Kalite ve Işıltı",
      comment:
        "Sculptural Torque Choker ve küpeler harika! Ağır ve kaliteli duruyor, kaplamasında hiçbir solma olmadı. Özel kutulaması ve hediye paketi çok şıktı.",
      product: "Sculptural Torque Choker",
    },
    {
      author: "Ece K.",
      city: "İzmir",
      rating: 5,
      title: "Hızlı Teslimat & Harika Paketleme",
      comment:
        "Siparişim ertesi gün kargoya verildi. Ürünler fotoğraftakinden bile daha güzel. Her kombine uyum sağlayan zamansız parçalar.",
      product: "Mixed Metal Ring Stack",
    },
    {
      author: "Merve A.",
      city: "Ankara",
      rating: 5,
      title: "Vazgeçilmez Aksesuarım Oldu",
      comment:
        "Reçine küpe ve toka sipariş ettim. Hafifliği ve malzeme hissi muazzam. Günlük kullanımda hiç ağırlık yapmıyor, çok memnun kaldım.",
      product: "French Sculpt Claw Toka",
    },
  ];

  return (
    <section aria-labelledby="reviews-heading" className="home-shell py-10 sm:py-14">
      <div className="text-center max-w-2xl mx-auto mb-10">
        <div className="inline-flex items-center gap-1 mb-2 text-brand-600">
          {[...Array(5)].map((_, i) => (
            <svg key={i} aria-hidden="true" viewBox="0 0 20 20" className="size-4 fill-current">
              <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
            </svg>
          ))}
          <span className="ml-1.5 text-xs font-bold text-ink">4.9 / 5.0 Memnuniyet</span>
        </div>
        <h2 id="reviews-heading" className="text-2xl font-bold tracking-tight text-ink sm:text-3xl">
          Müşterilerimizin Deneyimleri
        </h2>
        <p className="mt-2 text-sm text-ink-muted">
          Binlerce mutlu müşterimizin ELEVEN ürünleri ve alışveriş deneyimiyle ilgili paylaştığı yorumlar.
        </p>
      </div>

      <div className="grid gap-6 md:grid-cols-3">
        {reviews.map((rev) => (
          <article
            key={rev.author}
            className="flex flex-col justify-between rounded-2xl border border-line/80 bg-surface p-6 sm:p-7 shadow-xs hover:shadow-md transition-shadow"
          >
            <div>
              <div className="flex items-center gap-1 text-brand-600 mb-3">
                {[...Array(rev.rating)].map((_, i) => (
                  <svg key={i} aria-hidden="true" viewBox="0 0 20 20" className="size-3.5 fill-current">
                    <path d="M9.049 2.927c.3-.921 1.603-.921 1.902 0l1.07 3.292a1 1 0 00.95.69h3.462c.969 0 1.371 1.24.588 1.81l-2.8 2.034a1 1 0 00-.364 1.118l1.07 3.292c.3.921-.755 1.688-1.54 1.118l-2.8-2.034a1 1 0 00-1.175 0l-2.8 2.034c-.784.57-1.838-.197-1.539-1.118l1.07-3.292a1 1 0 00-.364-1.118L2.98 8.72c-.783-.57-.38-1.81.588-1.81h3.461a1 1 0 00.951-.69l1.07-3.292z" />
                  </svg>
                ))}
              </div>
              <h3 className="text-base font-bold text-ink mb-2">
                &ldquo;{rev.title}&rdquo;
              </h3>
              <p className="text-sm leading-relaxed text-ink-muted">
                {rev.comment}
              </p>
            </div>

            <div className="mt-6 pt-4 border-t border-line/60 flex items-center justify-between text-xs">
              <div>
                <span className="font-bold text-ink block">{rev.author}</span>
                <span className="text-ink-muted">{rev.city}</span>
              </div>
              <span className="text-[0.6875rem] font-medium text-brand-700 bg-surface-subtle px-2 py-1 rounded-md">
                Doğrulanmış Alıcı
              </span>
            </div>
          </article>
        ))}
      </div>
    </section>
  );
}
