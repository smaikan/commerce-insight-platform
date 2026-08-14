import { AccountPlaceholder } from "@/modules/account/components/account-placeholder";

// Burada güvenlik sayfasında şifre ve oturum yönetimi için veri uydurmadan kontrollü frontend durumunu hazırlıyorum.
export default function AccountSecurityPage() {
  return <AccountPlaceholder eyebrow="Hesap güvenliği" title="Güvenlik" description="Şifre değişikliği ve aktif oturum yönetimi bu alanda yer alacak." emptyTitle="Güvenlik araçları hazırlanıyor" emptyDescription="Şifre ve oturum işlemleri güvenli API entegrasyonu tamamlandığında etkinleştirilecek." disabledAction="Şifreyi değiştir" />;
}
