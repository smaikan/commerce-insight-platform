import type { NextConfig } from "next";

// Burada API'nin yönettiği ürün görsellerini ortam bazlı kesin HTTPS host allowlist'iyle sınırlandırıyorum.
const imageHosts = (process.env.STOREFRONT_IMAGE_HOSTS || "res.cloudinary.com")
  .split(",")
  .map((host) => host.trim().toLowerCase())
  .filter(Boolean);

// Burada eski ürün yolunu HTML meta refresh yerine gerçek kalıcı HTTP yönlendirmesiyle canonical rotaya taşıyorum.
const redirects: NonNullable<NextConfig["redirects"]> = async () => [
  {
    source: "/product/:slug",
    destination: "/products/:slug",
    permanent: true,
  },
];

// Burada medya allowlist'ini, kalıcı yönlendirmeyi ve modern AVIF/WebP görsel pipeline'ını yapılandırıyorum.
const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  redirects,
  images: {
    formats: ["image/avif", "image/webp"],
    minimumCacheTTL: 2592000, // 30 gün önbellek (sunucu ve CDN yükünü hafifletir)
    deviceSizes: [640, 750, 828, 1080, 1200, 1920, 2048],
    imageSizes: [16, 32, 48, 64, 96, 128, 256, 384],
    remotePatterns: imageHosts.map((hostname) => ({
      protocol: "https" as const,
      hostname,
      pathname: "/**",
    })),
  },
};

export default nextConfig;
