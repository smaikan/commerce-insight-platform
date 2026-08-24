import Image from "next/image";

// Burada mağazanın sosyal medya ve stil topluluğu ilham galerisini sunuyorum.
export function InstagramGallerySection() {
  const posts = [
    {
      id: "ig-1",
      imageUrl: "https://res.cloudinary.com/zqnbecc5/image/upload/v1787215070/products/P0001C/hka1vzbbinmscakblnnm.webp",
      alt: "Geo Reçine Küpe Kombini",
      handle: "@elevenaccessory",
    },
    {
      id: "ig-2",
      imageUrl: "https://res.cloudinary.com/zqnbecc5/image/upload/v1787214947/products/P0001E/zueebvhrd2utlohys6lh.jpg",
      alt: "Mixed Metal Yüzük Kombini",
      handle: "@elevenaccessory",
    },
    {
      id: "ig-3",
      imageUrl: "https://res.cloudinary.com/zqnbecc5/image/upload/v1787215227/products/P0001B/ecn2o2pwu3eytyfuvilt.jpg",
      alt: "Sculptural Choker Şıklığı",
      handle: "@elevenaccessory",
    },
    {
      id: "ig-4",
      imageUrl: "https://res.cloudinary.com/zqnbecc5/image/upload/v1787215478/products/P00019/xwdsvomjgvm9lxhk6qv9.jpg",
      alt: "Futurist Güneş Gözlüğü Stili",
      handle: "@elevenaccessory",
    },
  ];

  return (
    <section
      aria-labelledby="instagram-gallery-heading"
      className="home-shell py-10 sm:py-14"
    >
      <div className="flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between border-b border-line pb-4 mb-8">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.2em] text-brand-700">
            STİL İLHAMI &bull; INSTAGRAM
          </p>
          <h2
            id="instagram-gallery-heading"
            className="mt-1 text-2xl font-bold tracking-tight text-ink sm:text-3xl"
          >
            #ElevenWomen ile Kendi Tarzını Yarat
          </h2>
          <p className="mt-1 text-sm text-ink-muted">
            ELEVEN parçalarıyla oluşturulan en ilham verici görünümleri ve günlük kombinleri keşfedin.
          </p>
        </div>
        <a
          href="https://instagram.com"
          target="_blank"
          rel="noopener noreferrer"
          className="focus-ring inline-flex items-center gap-2 rounded-xl border border-line bg-surface px-4 py-2.5 text-xs sm:text-sm font-semibold text-brand-700 hover:border-brand-700 hover:bg-surface-subtle transition-all self-start sm:self-auto shrink-0"
        >
          <svg aria-hidden="true" viewBox="0 0 24 24" className="size-4 fill-current">
            <path d="M12 2.163c3.204 0 3.584.012 4.85.07 3.252.148 4.771 1.691 4.919 4.919.058 1.265.069 1.645.069 4.849 0 3.205-.012 3.584-.069 4.849-.149 3.225-1.664 4.771-4.919 4.919-1.266.058-1.644.07-4.85.07-3.204 0-3.584-.012-4.849-.07-3.26-.149-4.771-1.699-4.919-4.92-.058-1.265-.07-1.644-.07-4.849 0-3.204.013-3.583.07-4.849.149-3.227 1.664-4.771 4.919-4.919 1.266-.057 1.645-.069 4.849-.069zm0-2.163c-3.259 0-3.667.014-4.947.072-4.358.2-6.78 2.618-6.98 6.98-.059 1.281-.073 1.689-.073 4.948 0 3.259.014 3.668.072 4.948.2 4.358 2.618 6.78 6.98 6.98 1.281.058 1.689.072 4.948.072 3.259 0 3.668-.014 4.948-.072 4.354-.2 6.782-2.618 6.979-6.98.059-1.28.073-1.689.073-4.948 0-3.259-.014-3.667-.072-4.947-.196-4.354-2.617-6.78-6.979-6.98-1.281-.059-1.69-.073-4.949-.073zm0 5.838c-3.403 0-6.162 2.759-6.162 6.162s2.759 6.163 6.162 6.163 6.162-2.759 6.162-6.163c0-3.403-2.759-6.162-6.162-6.162zm0 10.162c-2.209 0-4-1.79-4-4 0-2.209 1.791-4 4-4s4 1.791 4 4c0 2.21-1.791 4-4 4zm6.406-11.845c-.796 0-1.441.645-1.441 1.44s.645 1.44 1.441 1.44c.795 0 1.439-.645 1.439-1.44s-.644-1.44-1.439-1.44z" />
          </svg>
          <span>@elevenaccessory</span>
        </a>
      </div>

      <div className="grid grid-cols-2 gap-4 sm:gap-6 lg:grid-cols-4">
        {posts.map((post) => (
          <div
            key={post.id}
            className="group relative aspect-square overflow-hidden rounded-2xl border border-line/60 bg-surface-subtle shadow-xs transition-all duration-300 hover:shadow-md"
          >
            <Image
              src={post.imageUrl}
              alt={post.alt}
              fill
              loading="lazy"
              className="object-cover transition-transform duration-700 motion-reduce:transition-none group-hover:scale-110"
              sizes="(min-width: 1024px) 25vw, 50vw"
            />
            {/* Lüks Hover Katmanı */}
            <div className="absolute inset-0 bg-gradient-to-t from-brand-950/80 via-brand-950/20 to-transparent opacity-0 transition-opacity duration-300 group-hover:opacity-100 flex flex-col justify-end p-4 text-white">
              <span className="text-xs font-bold text-white tracking-wide">
                {post.handle}
              </span>
              <p className="text-[0.6875rem] text-white/80 mt-0.5 truncate">
                {post.alt}
              </p>
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}
