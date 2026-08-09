import type { Metadata } from "next";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { StockMovementForm } from "@/modules/inventory/components/stock-movement-form";

export const metadata: Metadata = { title: "Stok Hareketi Oluştur" };

// Burada stok hareketi oluşturma formunu Admin oturumunu yeniden doğrulayarak sunuyorum.
export default async function NewStockMovementPage() {
  await requireAdminPageSession("/inventory/stock-movements/new");
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Stok hareketi oluştur" description="Bir veya daha fazla varyant için stok girişini, çıkışını ya da sayım düzeltmesini tek atomik işlemde kaydedin." backHref="/inventory/stock-movements" /><StockMovementForm /></div>;
}
