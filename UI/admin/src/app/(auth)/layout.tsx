import type { Metadata } from "next";

export const metadata: Metadata = {
  robots: { index: false, follow: false, nocache: true },
};

// Burada auth sayfalarını admin shell'den ayırıp sade ve noindex bir route grubu olarak sunuyorum.
export default function AuthLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return children;
}
