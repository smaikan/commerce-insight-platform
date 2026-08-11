import type { MetadataRoute } from "next";
import { siteConfig } from "@/lib/site-config";

// Burada public içeriğin taranmasına izin verip yalnız iç BFF ve yönetim yollarını crawl dışında tutuyorum.
export default function robots(): MetadataRoute.Robots {
  return {
    rules: {
      userAgent: "*",
      allow: "/",
      disallow: ["/admin", "/api"],
    },
    sitemap: `${siteConfig.url}/sitemap.xml`,
    host: siteConfig.url,
  };
}
