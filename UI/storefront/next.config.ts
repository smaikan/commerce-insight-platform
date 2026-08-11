import type { NextConfig } from "next";

// Burada API'nin yönettiği ürün görsellerini ortam bazlı kesin HTTPS host allowlist'iyle sınırlandırıyorum.
const imageHosts = (process.env.STOREFRONT_IMAGE_HOSTS || "res.cloudinary.com")
  .split(",")
  .map((host) => host.trim().toLowerCase())
  .filter(Boolean);

// Burada yapılandırılmış medya hostlarını Next.js görsel hattının kesin remote pattern listesine dönüştürüyorum.
const nextConfig: NextConfig = {
  images: {
    remotePatterns: imageHosts.map((hostname) => ({
      protocol: "https" as const,
      hostname,
      pathname: "/**",
    })),
  },
};

export default nextConfig;
