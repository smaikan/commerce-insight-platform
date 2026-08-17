import type { Metadata } from "next";
import { Geist } from "next/font/google";

import { getPublicStoreSettings } from "@/modules/store-settings/api";
import { buildRootMetadata } from "@/modules/store-settings/metadata";

import "./globals.css";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

// Burada ortak metadata'yı public mağaza adı ve favicon'uyla tamamlayıp API hatasında güvenli yerel değerlere dönüyorum.
export async function generateMetadata(): Promise<Metadata> {
  const settings = await getPublicStoreSettings().catch(() => null);

  return buildRootMetadata(settings);
}

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
