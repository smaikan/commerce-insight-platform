import type { Metadata } from "next";
import { Geist } from "next/font/google";

import { siteConfig } from "@/lib/site-config";

import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

// Burada tüm public rotalara ortak, host-bağımsız metadata temelini tanımlıyorum.
export const metadata: Metadata = {
  metadataBase: new URL(siteConfig.url),
  title: {
    default: siteConfig.name,
    template: `%s | ${siteConfig.name}`,
  },
  description: siteConfig.description,
  openGraph: {
    type: "website",
    locale: "tr_TR",
    siteName: siteConfig.name,
    title: siteConfig.name,
    description: siteConfig.description,
    url: "/",
  },
};

// Burada root layout'u yalnızca belge ve metadata sınırında tutup route gruplarının kendi görsel kabuğunu seçmesini sağlıyorum.
export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="tr" className={`${geistSans.variable} h-full antialiased`}>
      <body id="page-top" className="flex min-h-full flex-col">
        {children}
      </body>
    </html>
  );
}
