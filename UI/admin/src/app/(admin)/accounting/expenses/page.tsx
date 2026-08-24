import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { ApiError } from "@/lib/api/problem";
import { requireAdminPageSession } from "@/lib/auth/session";
import { PageHeader } from "@/modules/admin-shell/components/page-header";
import { AccountingLoadProblem } from "@/modules/accounting/core/components/accounting-load-problem";
import { getExpenseCategories, getExpenses } from "@/modules/accounting/purchases/api";
import { ExpensesWorkspace } from "@/modules/accounting/purchases/components/expenses-workspace";
import { buildExpenseListHref, canonicalPageNumber, parseExpenseListQuery } from "@/modules/accounting/purchases/query";

export const metadata: Metadata = { title: "Giderler" };

export default async function ExpensesPage({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  const query = parseExpenseListQuery(await searchParams);
  const session = await requireAdminPageSession(buildExpenseListHref(query));
  let expenses;
  let categoryPage;
  let categoryLookup;
  try { [expenses, categoryPage, categoryLookup] = await Promise.all([getExpenses(query, session), getExpenseCategories(query.categoryPageNumber, query.pageSize, session), getExpenseCategories(1, 100, session)]); }
  catch (error) { if (error instanceof ApiError) return <AccountingLoadProblem problem={error.problem} retryHref={buildExpenseListHref(query)} />; throw error; }
  const expenseCanonical = canonicalPageNumber(query.expensePageNumber, expenses.totalPages);
  const categoryCanonical = canonicalPageNumber(query.categoryPageNumber, categoryPage.totalPages);
  if (expenseCanonical || categoryCanonical) redirect(buildExpenseListHref(query, { expensePageNumber: expenseCanonical ?? query.expensePageNumber, categoryPageNumber: categoryCanonical ?? query.categoryPageNumber }));
  return <div className="mx-auto w-full max-w-screen-2xl"><PageHeader title="Giderler" description="Genel giderleri ve alış faturası maliyet dağıtımlarında kullanılan kategorileri yönetin." backHref="/accounting" /><ExpensesWorkspace expenses={expenses} categoryPage={categoryPage} categoryLookup={categoryLookup.items} query={query} /></div>;
}
