import type { Metadata } from "next";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { ManagerForm } from "@/modules/managers/components/manager-form";
export const metadata: Metadata = { title: "Yönetici ekle" };
// Burada yeni Admin hesabı için sınırlandırılmış formu açıyorum.
export default function NewManagerPage() { return <div className="mx-auto w-full max-w-3xl"><PageHeader title="Yönetici ekle" description="Yeni kullanıcı oluşturulur ve ardından Admin rolüne atanır." backHref="/managers" /><ManagerForm /></div>; }
