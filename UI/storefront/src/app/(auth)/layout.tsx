import type { Metadata } from "next";

// Burada kişisel işlem niteliğindeki auth sayfalarını arama sonuçlarından ve önbelleklenmiş snippet'lerden uzak tutuyorum.
export const metadata: Metadata = {
  robots: {
    index: false,
    follow: false,
    noarchive: true,
  },
};

// Burada auth rotalarını mağaza header/footer'ı olmadan, kendi skip linki ve ortak noindex politikasıyla sunuyorum.
export default function AuthLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <>
      <a className="skip-link" href="#main-content">Ana içeriğe geç</a>
      {children}
    </>
  );
}
