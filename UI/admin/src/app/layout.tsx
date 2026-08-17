import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { getPublicStoreSettings } from "@/modules/settings/api";
import { buildAdminRootMetadata } from "@/modules/settings/store-settings/metadata";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

// Burada admin root metadata'sını public StoreSettings favicon'u ile tamamlayıp API hatasında yerel favicon davranışına dönüyorum.
export async function generateMetadata(): Promise<Metadata> {
  const settings = await getPublicStoreSettings().catch(() => null);
  return buildAdminRootMetadata(settings?.faviconUrl);
}

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="tr"
      className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
    >
      <body className="min-h-full bg-page text-foreground">{children}</body>
    </html>
  );
}
