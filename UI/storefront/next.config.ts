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

// Burada medya allowlist'ini, kalıcı yönlendirmeyi ve gereksiz framework başlığını tek Next.js yapılandırmasında tutuyorum.
const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  redirects,
  images: {
    remotePatterns: imageHosts.map((hostname) => ({
      protocol: "https" as const,
      hostname,
      pathname: "/**",
    })),
  },
};

export default nextConfig;
