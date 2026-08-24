import "server-only";

import { apiRequest } from "@/lib/api/client";
import { ApiError } from "@/lib/api/problem";
import type { AdminSession } from "@/lib/auth/contracts";
import type { AccountingQueue, AccountingReportPage } from "@/modules/accounting/core/types";

const QUEUES = [
  { key: "overdue-receivables", title: "Gecikmiş alacaklar", description: "Vadesi geçen müşteri alacaklarını inceleyin.", path: "/api/accounting/reports/overdue-receivables", href: "/accounting/reports/overdue-receivables" },
  { key: "overdue-debts", title: "Gecikmiş borçlar", description: "Vadesi geçen tedarikçi borçlarını inceleyin.", path: "/api/accounting/reports/overdue-debts", href: "/accounting/reports/overdue-debts" },
  { key: "uncosted-stock", title: "Maliyetsiz stok hareketleri", description: "FIFO maliyeti bekleyen hareketleri inceleyin.", path: "/api/accounting/reports/stock-movements/uncosted", href: "/accounting/reports/uncosted-stock" },
  { key: "partially-costed-stock", title: "Kısmi maliyetli hareketler", description: "Maliyetinin bir bölümü tamamlanmamış hareketleri inceleyin.", path: "/api/accounting/reports/stock-movements/partially-costed", href: "/accounting/reports/partially-costed-stock" },
] as const;

export async function getAccountingQueues(session: AdminSession): Promise<AccountingQueue[]> {
  return Promise.all(QUEUES.map(async (queue) => {
    try {
      const page = await apiRequest<AccountingReportPage>(`${queue.path}?PageNumber=1&PageSize=1`, { accessToken: session.accessToken });
      return { key: queue.key, title: queue.title, description: queue.description, href: queue.href, totalCount: page.totalCount, unavailable: false };
    } catch (error) {
      if (error instanceof ApiError && (error.problem.status === 401 || error.problem.status === 403)) throw error;
      return { key: queue.key, title: queue.title, description: queue.description, href: queue.href, totalCount: null, unavailable: true };
    }
  }));
}
