import http from "node:http";

const accountId = "11111111-1111-4111-8111-111111111111";
const secondAccountId = "22222222-2222-4222-8222-222222222222";
const supplierAccountId = "23232323-2323-4232-8232-232323232323";
const baseAccount = {
  id: accountId,
  code: "CR-001",
  type: 3,
  name: "Çok Uzun Türkçe Unvanlı Kontrollü Cari Hesap Fixture Kaydı",
  tradeName: "Fixture Ticaret A.Ş.",
  nationalIdentityNumber: null,
  taxNumber: "1234567890",
  taxOffice: "Kadıköy",
  phoneNumber: "+90 555 000 00 00",
  email: "cari@example.test",
  country: "Türkiye",
  city: "İstanbul",
  district: "Kadıköy",
  neighborhood: "Caferağa",
  addressLine: "Kontrollü test adresi",
  postalCode: "34710",
  isActive: true,
  userId: null,
};
const accounts = [
  baseAccount,
  {
    ...baseAccount,
    id: secondAccountId,
    code: "TZ-002",
    type: 2,
    name: "Tedarikçi Fixture",
    isActive: false,
  },
  { ...baseAccount, id: supplierAccountId, code: "TZ-003", type: 2, name: "Aktif Tedarikçi Fixture", tradeName: "Tedarikçi A.Ş.", isActive: true },
];
const invoiceId = "55555555-5555-4555-8555-555555555555";
const lineId = "66666666-6666-4666-8666-666666666666";
const productVariantId = "77777777-7777-4777-8777-777777777777";
const movementId = "88888888-8888-4888-8888-888888888888";
const categoryId = "99999999-9999-4999-8999-999999999999";
const categories = [
  { id: categoryId, code: "NAKLIYE", name: "Nakliye Gideri", isActive: true },
];
const product = {
  id: "P00001",
  title: "Fixture Muhasebe Ürünü",
  isActive: true,
  variants: [
    {
      id: productVariantId,
      productId: "P00001",
      name: "Boyut",
      value: "Standart",
      sku: "FIX-001",
      barcode: null,
      material: null,
      price: 100,
      netPrice: 83.33,
      compareAtPrice: null,
      stock: 10,
      addToCartCount: 0,
      purchaseCount: 0,
      isActive: true,
      concurrencyToken: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
    },
  ],
  tags: [],
  collections: [],
  images: [],
  clickCount: 0,
  totalAddToCartCount: 0,
  totalPurchaseCount: 0,
  favoriteCount: 0,
  popularityScore: 0,
  averageRating: 0,
  ratingCount: 0,
  reviewCount: 0,
  displayOrder: 0,
  isFeatured: false,
  hasVariants: true,
  status: 1,
  mainSku: "FIX-001",
  slug: "fixture",
  type: null,
  url: null,
  brand: null,
  description: null,
  seoTitle: null,
  seoDescription: null,
  summary: null,
};
const statements = Array.from({ length: 21 }, (_, index) => ({
  id: `33333333-3333-4333-8333-${String(index + 1).padStart(12, "0")}`,
  relatedId: accountId,
  number: null,
  name: baseAccount.name,
  date: `2026-08-${String(21 - Math.min(index, 20)).padStart(2, "0")}T10:00:00Z`,
  dueDate: null,
  amount: index % 2 ? 0 : 1234567.5,
  secondaryAmount: index % 2 ? 250 : 0,
  tertiaryAmount: index % 2 ? 250 : 1234567.5,
  quantity: 0,
  rate: null,
  hasSalesInvoice: null,
  currencyCode: "TRY",
}));
let createRequests = 0;
let emptyList = false;
let delayNextList = false;
let invoice;
let purchaseExpenses;
let generalExpenses;
let purchaseCreateRequests;
let purchasePostRequests;
let purchaseExpenseRequests;
const salesOrderId = "12121212-1212-4121-8121-121212121212";
const salesInvoiceId = "13131313-1313-4131-8131-131313131313";
const salesLineId = "14141414-1414-4141-8141-141414141414";
let salesOrder;
let salesInvoice;
let salesCreateRequests;
let salesInvoiceCreateRequests;
let salesPostRequests;
let salesCancelRequests;
const cashAccountId = "15151515-1515-4151-8151-151515151515";
const bankAccountId = "16161616-1616-4161-8161-161616161616";
const secondBankAccountId = "17171717-1717-4171-8171-171717171717";
const receivableId = "18181818-1818-4181-8181-181818181818";
const debtId = "19191919-1919-4191-8191-191919191919";
const paymentId = "20202020-2020-4202-8202-202020202020";
let cashAccounts;
let bankAccounts;
let financialStatements;
let payments;
let paymentCreateRequests;
let paymentCancelRequests;
let financialCreateRequests;
let transferCreateRequests;
const openingLayerId = "34343434-3434-4343-8343-343434343434";
let openingLayer;
let openingCostPatchRequests;
let forceOpeningCostConflict;

function resetPurchases() {
  invoice = makeInvoice();
  purchaseExpenses = [];
  generalExpenses = [];
  categories.splice(1);
  purchaseCreateRequests = 0;
  purchasePostRequests = 0;
  purchaseExpenseRequests = 0;
}
function resetSales() {
  salesOrder = makeSalesOrder();
  salesInvoice = null;
  salesCreateRequests = 0;
  salesInvoiceCreateRequests = 0;
  salesPostRequests = 0;
  salesCancelRequests = 0;
}
function resetTreasury() {
  cashAccounts = [{ id: cashAccountId, code: "KASA-01", name: "Merkez Kasa", currencyCode: "TRY", isActive: true, balance: 1250 }];
  bankAccounts = [
    { id: bankAccountId, code: "BNK-01", name: "Operasyon Hesabı", bankName: "Fixture Bank", iban: "TR000000000000000000000001", currencyCode: "TRY", isActive: true, balance: 8500 },
    { id: secondBankAccountId, code: "BNK-02", name: "Yedek Hesap", bankName: "Fixture Bank", iban: "TR000000000000000000000002", currencyCode: "TRY", isActive: true, balance: 2500 },
  ];
  financialStatements = {
    [cashAccountId]: [{ id: "21212121-2121-4212-8212-212121212121", cashAccountId, bankAccountId: null, type: 10, direction: 1, amount: 1250, balanceAfter: 1250, currencyCode: "TRY", transactionDate: "2026-08-20T00:00:00Z", sourceType: 5, sourceId: "22212121-2121-4212-8212-212121212121", description: "Açılış sonrası manuel giriş", reversesTransactionId: null, createdBy: 1, createdAt: "2026-08-20T10:00:00Z" }],
    [bankAccountId]: [{ id: "23212121-2121-4212-8212-212121212121", cashAccountId: null, bankAccountId, type: 10, direction: 1, amount: 8500, balanceAfter: 8500, currencyCode: "TRY", transactionDate: "2026-08-20T00:00:00Z", sourceType: 5, sourceId: "24212121-2121-4212-8212-212121212121", description: "Banka başlangıç hareketi", reversesTransactionId: null, createdBy: 1, createdAt: "2026-08-20T10:00:00Z" }],
    [secondBankAccountId]: [],
  };
  payments = [];
  paymentCreateRequests = 0; paymentCancelRequests = 0; financialCreateRequests = 0; transferCreateRequests = 0;
}
// Burada her tarayıcı senaryosuna aynı açılış katmanı ve concurrency başlangıcını veriyorum.
function resetCosting() {
  openingLayer = { id: openingLayerId, productVariantId, stockMovementId: movementId, sourceType: 1, originalQuantity: 10, remainingQuantity: 8, unitCostExcludingVat: 80, unitCostIncludingVat: 96, totalCostExcludingVat: 800, totalCostIncludingVat: 960, costDate: "2026-08-01T00:00:00Z", status: 1, concurrencyToken: "35353535-3535-4353-8353-353535353535" };
  openingCostPatchRequests = 0;
  forceOpeningCostConflict = false;
}
resetPurchases();
resetSales();
resetTreasury();
resetCosting();

const server = http.createServer(async (request, response) => {
  const url = new URL(request.url || "/", "http://127.0.0.1:4320");
  if (url.pathname === "/__test/reset" && request.method === "POST") {
    createRequests = 0;
    emptyList = false;
    delayNextList = false;
    resetPurchases();
    resetSales();
    resetTreasury();
    resetCosting();
    return json(response, 204);
  }
  if (url.pathname === "/__test/state")
    return json(response, 200, {
      createRequests,
      purchaseCreateRequests,
      purchasePostRequests,
      purchaseExpenseRequests,
      salesCreateRequests,
      salesInvoiceCreateRequests,
      salesPostRequests,
      salesCancelRequests,
      paymentCreateRequests,
      paymentCancelRequests,
      financialCreateRequests,
      transferCreateRequests,
      openingCostPatchRequests,
    });
  if (url.pathname === "/__test/empty" && request.method === "POST") {
    emptyList = true;
    return json(response, 204);
  }
  if (url.pathname === "/__test/delay" && request.method === "POST") {
    delayNextList = true;
    return json(response, 204);
  }
  // Burada yalnız test kontrol düzleminden bir sonraki maliyet yazımını stale conflict'e çeviriyorum.
  if (url.pathname === "/__test/stale-cost" && request.method === "POST") {
    forceOpeningCostConflict = true;
    return json(response, 204);
  }
  if (url.pathname === "/api/users/me")
    return json(response, 200, {
      id: "U00001",
      email: "admin.fixture@example.test",
      firstName: "Ada",
      lastName: "Yönetici",
      phoneNumber: null,
      role: 2,
      status: 1,
      createdAt: "2026-08-24T00:00:00Z",
    });
  if (url.pathname === "/api/store-settings/admin")
    return json(response, 200, { displayName: "ELEVEN Test Mağazası" });
  if (url.pathname === "/api/accounting/reports/receivables" && request.method === "GET") {
    const rows = url.searchParams.get("Id") === accountId ? [{ id: receivableId, relatedId: accountId, number: "SAT-FIX-001", name: baseAccount.name, date: "2026-08-20T00:00:00Z", dueDate: "2026-08-23T00:00:00Z", amount: 100, secondaryAmount: 0, tertiaryAmount: 100, quantity: 0, rate: null, hasSalesInvoice: null, currencyCode: "TRY" }] : [];
    return json(response, 200, page(rows, 1, 100, rows.length));
  }
  if (url.pathname === "/api/accounting/reports/debts" && request.method === "GET") {
    const rows = url.searchParams.get("Id") === supplierAccountId ? [{ id: debtId, relatedId: supplierAccountId, number: "ALIŞ-FIX-001", name: "Aktif Tedarikçi Fixture", date: "2026-08-19T00:00:00Z", dueDate: "2026-08-22T00:00:00Z", amount: 0, secondaryAmount: 200, tertiaryAmount: 200, quantity: 0, rate: null, hasSalesInvoice: null, currencyCode: "TRY" }] : [];
    return json(response, 200, page(rows, 1, 100, rows.length));
  }
  if (url.pathname === "/api/accounting/payments" && request.method === "GET") return json(response, 200, page(payments.map((payment) => ({ id: payment.id, currentAccountId: payment.currentAccountId, type: payment.type, direction: payment.direction, status: payment.status, amount: payment.amount, currencyCode: payment.currencyCode, paymentDate: payment.paymentDate })), Number(url.searchParams.get("PageNumber") || "1"), 20, payments.length));
  if (url.pathname === "/api/accounting/payments" && request.method === "POST") {
    paymentCreateRequests += 1; const input = await body(request); const allocatedAmount = input.allocations.reduce((sum, item) => sum + item.amount, 0);
    const payment = { id: paymentId, currentAccountId: input.currentAccountId, type: input.type, direction: input.type === 1 ? 1 : 2, status: 1, amount: input.amount, allocatedAmount, unallocatedAmount: input.amount - allocatedAmount, currencyCode: "TRY", paymentDate: input.paymentDate, cashAccountId: input.cashAccountId, bankAccountId: input.bankAccountId, referenceNumber: input.referenceNumber, description: input.description, createdAt: "2026-08-24T12:00:00Z", cancelledBy: null, cancelledAt: null, cancellationReason: null, allocations: input.allocations.map((item, index) => ({ id: `25252525-2525-4252-8252-${String(index + 1).padStart(12, "0")}`, currentAccountTransactionId: item.currentAccountTransactionId, sourceType: input.type === 1 ? 3 : 1, sourceId: input.type === 1 ? salesOrderId : invoiceId, allocatedAmount: item.amount, isReversed: false, reversedAt: null })) };
    payments = [payment]; const targetId = input.cashAccountId || input.bankAccountId; const direction = input.type === 1 ? 1 : 2; const previous = financialStatements[targetId]?.at(-1)?.balanceAfter ?? 0; const tx = { id: "26262626-2626-4262-8262-262626262626", cashAccountId: input.cashAccountId, bankAccountId: input.bankAccountId, type: input.type, direction, amount: input.amount, balanceAfter: previous + (direction === 1 ? input.amount : -input.amount), currencyCode: "TRY", transactionDate: input.paymentDate, sourceType: 4, sourceId: paymentId, description: input.description, reversesTransactionId: null, createdBy: 1, createdAt: "2026-08-24T12:00:00Z" }; (financialStatements[targetId] ||= []).push(tx);
    response.setHeader("Location", `/api/accounting/payments/${paymentId}`); return json(response, 201, payment);
  }
  if (url.pathname === `/api/accounting/payments/${paymentId}` && request.method === "GET") return payments[0] ? json(response, 200, payments[0]) : problem(response, 404, "not_found", "Ödeme bulunamadı.");
  if (url.pathname === `/api/accounting/payments/${paymentId}/cancel` && request.method === "POST") { paymentCancelRequests += 1; const input = await body(request); const payment = payments[0]; if (!payment) return problem(response, 404, "not_found", "Ödeme bulunamadı."); if (payment.status === 2) return json(response, 200, { id: paymentId, status: "Cancelled", alreadyProcessed: true }); payment.status = 2; payment.cancelledAt = "2026-08-24T13:00:00Z"; payment.cancellationReason = input.reason; payment.allocations = []; payment.allocatedAmount = 0; payment.unallocatedAmount = payment.amount; return json(response, 200, { id: paymentId, status: "Cancelled", alreadyProcessed: false }); }
  if (url.pathname === "/api/accounting/cash-accounts" && request.method === "GET") return json(response, 200, cashAccounts);
  if (url.pathname === "/api/accounting/cash-accounts" && request.method === "POST") { const input = await body(request); const created = { id: "27272727-2727-4272-8272-272727272727", ...input, isActive: true, balance: 0 }; cashAccounts.push(created); financialStatements[created.id] = []; response.setHeader("Location", `/api/accounting/cash-accounts/${created.id}/statement`); return json(response, 201, created); }
  if (url.pathname === "/api/accounting/bank-accounts" && request.method === "GET") return json(response, 200, bankAccounts);
  if (url.pathname === "/api/accounting/bank-accounts" && request.method === "POST") { const input = await body(request); const created = { id: "28282828-2828-4282-8282-282828282828", ...input, isActive: true, balance: 0 }; bankAccounts.push(created); financialStatements[created.id] = []; response.setHeader("Location", `/api/accounting/bank-accounts/${created.id}/statement`); return json(response, 201, created); }
  const cashStatement = url.pathname.match(/^\/api\/accounting\/cash-accounts\/([^/]+)\/statement$/); if (cashStatement && request.method === "GET") return json(response, 200, financialStatements[decodeURIComponent(cashStatement[1])] || []);
  const bankStatement = url.pathname.match(/^\/api\/accounting\/bank-accounts\/([^/]+)\/statement$/); if (bankStatement && request.method === "GET") return json(response, 200, financialStatements[decodeURIComponent(bankStatement[1])] || []);
  if (url.pathname === "/api/accounting/financial-transactions" && request.method === "POST") { financialCreateRequests += 1; const input = await body(request); const targetId = input.cashAccountId || input.bankAccountId; const direction = input.type === 10 ? 1 : 2; const previous = financialStatements[targetId]?.at(-1)?.balanceAfter ?? 0; const tx = { id: "29292929-2929-4292-8292-292929292929", cashAccountId: input.cashAccountId, bankAccountId: input.bankAccountId, type: input.type, direction, amount: input.amount, balanceAfter: previous + (direction === 1 ? input.amount : -input.amount), currencyCode: "TRY", transactionDate: input.transactionDate, sourceType: 5, sourceId: request.headers["idempotency-key"], description: input.description, reversesTransactionId: null, createdBy: 1, createdAt: "2026-08-24T14:00:00Z" }; (financialStatements[targetId] ||= []).push(tx); return json(response, 200, tx); }
  if (url.pathname === "/api/accounting/financial-transactions/bank-transfers" && request.method === "POST") { transferCreateRequests += 1; const input = await body(request); const sourceId = request.headers["idempotency-key"]; const outPrevious = financialStatements[input.fromBankAccountId]?.at(-1)?.balanceAfter ?? 0; const inPrevious = financialStatements[input.toBankAccountId]?.at(-1)?.balanceAfter ?? 0; const transferOut = { id: "30303030-3030-4303-8303-303030303030", cashAccountId: null, bankAccountId: input.fromBankAccountId, type: 21, direction: 2, amount: input.amount, balanceAfter: outPrevious - input.amount, currencyCode: "TRY", transactionDate: input.transactionDate, sourceType: 5, sourceId, description: input.description, reversesTransactionId: null, createdBy: 1, createdAt: "2026-08-24T15:00:00Z" }; const transferIn = { ...transferOut, id: "31313131-3131-4313-8313-313131313131", bankAccountId: input.toBankAccountId, type: 20, direction: 1, balanceAfter: inPrevious + input.amount }; financialStatements[input.fromBankAccountId].push(transferOut); financialStatements[input.toBankAccountId].push(transferIn); return json(response, 200, { transferOut, transferIn }); }
  const reverseMatch = url.pathname.match(/^\/api\/accounting\/financial-transactions\/([^/]+)\/reverse$/); if (reverseMatch && request.method === "POST") { const originalId = decodeURIComponent(reverseMatch[1]); const entry = Object.entries(financialStatements).find(([, rows]) => rows.some((row) => row.id === originalId)); if (!entry) return problem(response, 404, "not_found", "Hareket bulunamadı."); const [, rows] = entry; const original = rows.find((row) => row.id === originalId); const already = rows.some((row) => row.reversesTransactionId === originalId); if (!already) rows.push({ ...original, id: "32323232-3232-4323-8323-323232323232", type: original.direction === 1 ? 61 : 60, direction: original.direction === 1 ? 2 : 1, balanceAfter: (rows.at(-1)?.balanceAfter ?? 0) + (original.direction === 1 ? -original.amount : original.amount), sourceId: original.id, reversesTransactionId: original.id, description: "Kontrollü ters kayıt", createdAt: "2026-08-24T16:00:00Z" }); return json(response, 200, { id: originalId, status: "Reversed", alreadyProcessed: already }); }
  // Burada rapor sayfalarının ortak taşıma şemasını semantik kolon testleri için kontrollü bir satırla besliyorum.
  if (
    url.pathname.startsWith("/api/accounting/reports/") &&
    !url.pathname.includes("/current-accounts/")
  ) {
    const pageNumber = Number(url.searchParams.get("PageNumber") || "1");
    const pageSize = Number(url.searchParams.get("PageSize") || "20");
    const row = { id: "36363636-3636-4363-8363-363636363636", relatedId: accountId, number: "RPR-2026-001", name: "Fixture finansal rapor kaydı", date: "2026-08-20T00:00:00Z", dueDate: "2026-08-23T00:00:00Z", amount: 1250.5, secondaryAmount: 800.25, tertiaryAmount: 450.25, quantity: 10, rate: 36.01, hasSalesInvoice: true, currencyCode: "TRY" };
    return json(response, 200, page(pageNumber === 1 ? [row] : [], pageNumber, pageSize, 1));
  }
  if (url.pathname === `/api/accounting/inventory-cost-layers/opening-balance/by-variant/${productVariantId}` && request.method === "GET") return json(response, 200, openingLayer);
  if (url.pathname === `/api/accounting/inventory-cost-layers/${openingLayerId}/opening-balance-cost` && request.method === "PATCH") {
    openingCostPatchRequests += 1;
    const input = await body(request);
    if (forceOpeningCostConflict) {
      forceOpeningCostConflict = false;
      openingLayer = { ...openingLayer, unitCostExcludingVat: 91.25, unitCostIncludingVat: 109.5, totalCostExcludingVat: 912.5, totalCostIncludingVat: 1095, concurrencyToken: "37373737-3737-4373-8373-373737373737" };
      return problem(response, 409, "concurrency_conflict", "Katman başka bir yönetici tarafından güncellendi.");
    }
    if (input.expectedConcurrencyToken !== openingLayer.concurrencyToken) return problem(response, 409, "concurrency_conflict", "Katman sürümü güncel değil.");
    openingLayer = { ...openingLayer, unitCostExcludingVat: input.unitCostExcludingVat, unitCostIncludingVat: input.unitCostIncludingVat ?? input.unitCostExcludingVat, totalCostExcludingVat: input.unitCostExcludingVat * openingLayer.originalQuantity, totalCostIncludingVat: (input.unitCostIncludingVat ?? input.unitCostExcludingVat) * openingLayer.originalQuantity, concurrencyToken: "38383838-3838-4383-8383-383838383838" };
    return json(response, 200, openingLayer);
  }
  if (url.pathname === `/api/product-variants/${productVariantId}` && request.method === "GET") return json(response, 200, product.variants[0]);
  if (url.pathname === "/api/products" && request.method === "GET")
    return json(response, 200, page([product], 1, 100, 1));
  if (
    url.pathname === "/api/accounting/sales-orders" &&
    request.method === "GET"
  )
    return json(response, 200, page([salesOrderSummary(salesOrder)], 1, 20, 1));
  if (
    url.pathname === "/api/accounting/sales-orders" &&
    request.method === "POST"
  ) {
    salesCreateRequests += 1;
    if (!request.headers["idempotency-key"])
      return problem(
        response,
        400,
        "validation_error",
        "Idempotency-Key zorunludur.",
      );
    const input = await body(request);
    salesOrder = makeSalesOrder(input);
    salesInvoice = input.createInvoice
      ? makeSalesInvoice(input.invoice, salesOrder)
      : null;
    if (salesInvoice) salesOrder.salesInvoiceId = salesInvoice.id;
    response.setHeader(
      "Location",
      `/api/accounting/sales-orders/${salesOrderId}`,
    );
    return json(response, 201, salesOrder);
  }
  if (
    url.pathname === `/api/accounting/sales-orders/${salesOrderId}` &&
    request.method === "GET"
  )
    return json(response, 200, salesOrder);
  if (
    url.pathname === `/api/accounting/sales-orders/${salesOrderId}` &&
    request.method === "PUT"
  ) {
    const input = await body(request);
    salesOrder = makeSalesOrder(input);
    if (salesInvoice)
      salesInvoice = makeSalesInvoice(
        {
          invoiceNumber: salesInvoice.invoiceNumber,
          invoiceDate: salesInvoice.invoiceDate,
          dueDate: salesInvoice.dueDate,
          description: salesInvoice.description,
        },
        salesOrder,
      );
    return json(response, 200, salesOrder);
  }
  if (
    url.pathname === `/api/accounting/sales-orders/${salesOrderId}/post` &&
    request.method === "POST"
  ) {
    salesPostRequests += 1;
    postSalesFixture();
    return json(response, 200, salesOrder);
  }
  if (
    url.pathname === `/api/accounting/sales-orders/${salesOrderId}/cancel` &&
    request.method === "POST"
  ) {
    if (salesOrder.status !== 2)
      return problem(
        response,
        409,
        "conflict",
        "Only posted order can be cancelled.",
      );
    salesCancelRequests += 1;
    const input = await body(request);
    salesOrder.status = 3;
    salesOrder.cancelledAt = "2026-08-24T14:00:00Z";
    salesOrder.cancellationReason = input.reason;
    if (salesInvoice) {
      salesInvoice.status = 3;
      salesInvoice.cancelledAt = salesOrder.cancelledAt;
      salesInvoice.cancellationReason = input.reason;
    }
    return json(response, 200, {
      id: salesOrderId,
      status: 3,
      alreadyProcessed: false,
    });
  }
  if (
    url.pathname === "/api/accounting/sales-invoices" &&
    request.method === "GET"
  )
    return json(
      response,
      200,
      page(
        salesInvoice ? [salesInvoiceSummary(salesInvoice)] : [],
        1,
        20,
        salesInvoice ? 1 : 0,
      ),
    );
  if (
    url.pathname === "/api/accounting/sales-invoices" &&
    request.method === "POST"
  ) {
    salesInvoiceCreateRequests += 1;
    if (!request.headers["idempotency-key"])
      return problem(
        response,
        400,
        "validation_error",
        "Idempotency-Key zorunludur.",
      );
    const input = await body(request);
    salesOrder = makeSalesOrder({
      header: input.orderHeader,
      lines: input.lines,
    });
    salesInvoice = makeSalesInvoice(input.invoiceHeader, salesOrder);
    salesOrder.salesInvoiceId = salesInvoice.id;
    response.setHeader(
      "Location",
      `/api/accounting/sales-invoices/${salesInvoiceId}`,
    );
    return json(response, 201, salesInvoice);
  }
  if (
    url.pathname ===
      `/api/accounting/sales-invoices/from-order/${salesOrderId}` &&
    request.method === "POST"
  ) {
    salesInvoiceCreateRequests += 1;
    const input = await body(request);
    salesInvoice = makeSalesInvoice(input.header, salesOrder);
    salesOrder.salesInvoiceId = salesInvoice.id;
    if (salesOrder.status === 2) salesInvoice.status = 2;
    response.setHeader(
      "Location",
      `/api/accounting/sales-invoices/${salesInvoiceId}`,
    );
    return json(response, 201, salesInvoice);
  }
  if (
    url.pathname === `/api/accounting/sales-invoices/${salesInvoiceId}` &&
    request.method === "GET"
  )
    return salesInvoice
      ? json(response, 200, salesInvoice)
      : problem(response, 404, "not_found", "Fatura bulunamadı.");
  if (
    url.pathname === `/api/accounting/sales-invoices/${salesInvoiceId}` &&
    request.method === "PUT"
  ) {
    const input = await body(request);
    salesOrder = makeSalesOrder({
      header: orderHeaderFrom(salesOrder),
      lines: input.lines,
    });
    salesInvoice = makeSalesInvoice(input.header, salesOrder);
    salesOrder.salesInvoiceId = salesInvoice.id;
    return json(response, 200, salesInvoice);
  }
  if (
    url.pathname === `/api/accounting/sales-invoices/${salesInvoiceId}/post` &&
    request.method === "POST"
  ) {
    salesPostRequests += 1;
    postSalesFixture();
    return json(response, 200, salesInvoice);
  }
  if (
    url.pathname === "/api/accounting/purchase-invoices" &&
    request.method === "GET"
  )
    return json(response, 200, page([summary(invoice)], 1, 20, 1));
  if (
    url.pathname === "/api/accounting/purchase-invoices" &&
    request.method === "POST"
  ) {
    purchaseCreateRequests += 1;
    const input = await body(request);
    invoice = makeInvoice(input);
    response.setHeader(
      "Location",
      `/api/accounting/purchase-invoices/${invoiceId}`,
    );
    return json(response, 201, invoice);
  }
  if (
    url.pathname === `/api/accounting/purchase-invoices/${invoiceId}` &&
    request.method === "GET"
  )
    return json(response, 200, invoice);
  if (
    url.pathname ===
      "/api/accounting/purchase-invoices/available-stock-movements" &&
    request.method === "GET"
  )
    return json(
      response,
      200,
      invoice.lines[0].allocations.length
        ? []
        : [
            {
              id: movementId,
              productVariantId,
              quantity: 10,
              allocatedQuantity: 0,
              availableQuantity: 10,
              createdAt: "2026-08-20T10:00:00Z",
            },
          ],
    );
  if (
    url.pathname ===
      `/api/accounting/purchase-invoices/${invoiceId}/lines/${lineId}/allocations` &&
    request.method === "PUT"
  ) {
    const input = await body(request);
    invoice.lines[0].allocations = input.map((item, index) => ({
      id: `aaaaaaaa-aaaa-4aaa-8aaa-${String(index + 1).padStart(12, "0")}`,
      stockMovementId: item.stockMovementId,
      allocatedQuantity: item.quantity,
    }));
    return json(response, 200, invoice);
  }
  if (
    url.pathname === `/api/accounting/purchase-invoices/${invoiceId}/post` &&
    request.method === "POST"
  ) {
    purchasePostRequests += 1;
    if (
      invoice.lines[0].allocations.reduce(
        (sum, item) => sum + item.allocatedQuantity,
        0,
      ) !== invoice.lines[0].stockQuantity
    )
      return problem(
        response,
        409,
        "conflict",
        "Every line must be allocated.",
      );
    invoice.status = 2;
    invoice.postedAt = "2026-08-24T12:00:00Z";
    invoice.remainingAmount = invoice.grandTotalIncludingVat;
    return json(response, 200, { ...invoice, remainingAmount: 0 });
  }
  if (
    url.pathname === `/api/accounting/purchase-invoices/${invoiceId}/cancel` &&
    request.method === "POST"
  ) {
    if (invoice.status === 3)
      return json(response, 200, {
        id: invoiceId,
        status: "Cancelled",
        alreadyProcessed: true,
      });
    if (invoice.status !== 2)
      return problem(response, 409, "conflict", "Only posted.");
    const input = await body(request);
    invoice.status = 3;
    invoice.cancelledAt = "2026-08-24T13:00:00Z";
    invoice.cancellationReason = input.reason;
    return json(response, 200, {
      id: invoiceId,
      status: "Cancelled",
      alreadyProcessed: false,
    });
  }
  if (
    url.pathname ===
      `/api/accounting/purchase-invoices/${invoiceId}/expenses` &&
    request.method === "GET"
  )
    return json(response, 200, purchaseExpenses);
  if (
    url.pathname ===
      `/api/accounting/purchase-invoices/${invoiceId}/expenses` &&
    request.method === "POST"
  ) {
    purchaseExpenseRequests += 1;
    const input = await body(request);
    const expense = {
      id: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
      purchaseInvoiceId: invoiceId,
      categoryId: input.categoryId,
      allocationMethod: input.allocationMethod,
      amountExcludingVat: input.amountExcludingVat,
      amountIncludingVat: input.amountExcludingVat * (1 + input.vatRate / 100),
      allocations: [
        {
          lineId,
          amountExcludingVat: input.amountExcludingVat,
          amountIncludingVat:
            input.amountExcludingVat * (1 + input.vatRate / 100),
        },
      ],
    };
    purchaseExpenses.push(expense);
    invoice.totalFinalCostExcludingVat += input.amountExcludingVat;
    invoice.totalFinalCostIncludingVat += expense.amountIncludingVat;
    invoice.lines[0].finalUnitCostExcludingVat =
      invoice.totalFinalCostExcludingVat / invoice.lines[0].stockQuantity;
    invoice.lines[0].finalUnitCostIncludingVat =
      invoice.totalFinalCostIncludingVat / invoice.lines[0].stockQuantity;
    return json(response, 200, expense);
  }
  if (
    url.pathname ===
      `/api/accounting/product-variants/${productVariantId}/cost-history` &&
    request.method === "GET"
  )
    return json(
      response,
      200,
      [
        {
          id: "39393939-3939-4393-8393-393939393939",
          productVariantId,
          sourceType: 2,
          sourceId: openingLayerId,
          previousCostExcludingVat: 75,
          newCostExcludingVat: openingLayer.unitCostExcludingVat,
          previousCostIncludingVat: 90,
          newCostIncludingVat: openingLayer.unitCostIncludingVat,
          validFrom: "2026-08-01T00:00:00Z",
          validTo: null,
          openingStockQuantity: 10,
          closingStockQuantity: null,
          createdAt: "2026-08-01T12:00:00Z",
        },
        ...(invoice.status === 1 ? [] : [
            {
              id: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
              productVariantId,
              sourceType: 1,
              sourceId: invoiceId,
              previousCostExcludingVat: 80,
              newCostExcludingVat: 100,
              previousCostIncludingVat: 96,
              newCostIncludingVat: 120,
              validFrom: "2026-08-24T00:00:00Z",
              validTo: null,
              openingStockQuantity: 10,
              closingStockQuantity: null,
              createdAt: "2026-08-24T12:00:00Z",
            },
          ]),
      ],
    );
  if (
    url.pathname === "/api/accounting/expenses/categories" &&
    request.method === "GET"
  )
    return json(
      response,
      200,
      page(
        categories,
        Number(url.searchParams.get("PageNumber") || "1"),
        Number(url.searchParams.get("PageSize") || "20"),
        categories.length,
      ),
    );
  if (
    url.pathname === "/api/accounting/expenses/categories" &&
    request.method === "POST"
  ) {
    const input = await body(request);
    const category = {
      id: "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
      code: input.code.toUpperCase(),
      name: input.name,
      isActive: true,
    };
    categories.push(category);
    return json(response, 200, category);
  }
  if (url.pathname === "/api/accounting/expenses" && request.method === "GET")
    return json(
      response,
      200,
      page(
        generalExpenses,
        Number(url.searchParams.get("PageNumber") || "1"),
        Number(url.searchParams.get("PageSize") || "20"),
        generalExpenses.length,
      ),
    );
  if (
    url.pathname === "/api/accounting/expenses" &&
    request.method === "POST"
  ) {
    const input = await body(request);
    const expense = {
      id: "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
      categoryId: input.categoryId,
      type: 1,
      amountExcludingVat: input.amountExcludingVat,
      vatRate: input.vatRate,
      vatAmount: (input.amountExcludingVat * input.vatRate) / 100,
      totalAmountIncludingVat:
        input.amountExcludingVat * (1 + input.vatRate / 100),
      expenseDate: input.expenseDate,
      description: input.description,
    };
    generalExpenses.push(expense);
    return json(response, 200, expense);
  }
  if (
    url.pathname === "/api/accounting/current-accounts" &&
    request.method === "GET"
  ) {
    const pageNumber = Number(url.searchParams.get("PageNumber") || "1");
    if (delayNextList) {
      delayNextList = false;
      await new Promise((resolve) => setTimeout(resolve, 1_200));
    }
    if (pageNumber === 403)
      return problem(response, 403, "forbidden", "Kontrollü yetki hatası.");
    if (pageNumber === 429) {
      response.setHeader("Retry-After", "60 saniye");
      return problem(response, 429, "rate_limited", "Kontrollü limit hatası.");
    }
    if (pageNumber === 500)
      return problem(
        response,
        500,
        "internal_error",
        "Kontrollü servis hatası.",
      );
    const source = emptyList ? [] : accounts;
    return json(
      response,
      200,
      page(pageNumber === 1 ? source : [], pageNumber, 20, source.length),
    );
  }
  if (
    url.pathname === "/api/accounting/current-accounts" &&
    request.method === "POST"
  ) {
    createRequests += 1;
    const input = await body(request);
    if (input.code === "DUPLICATE")
      return problem(
        response,
        409,
        "conflict",
        "Cari kodu zaten kullanılıyor.",
      );
    if (input.code === "RATE") {
      response.setHeader("Retry-After", "45 saniye");
      return problem(response, 429, "rate_limited", "Çok fazla istek.");
    }
    response.setHeader(
      "Location",
      `/api/accounting/current-accounts/${accountId}`,
    );
    return json(response, 201, {
      ...baseAccount,
      ...input,
      id: accountId,
      code: input.code.toUpperCase(),
      email: input.email?.toLowerCase() ?? null,
      isActive: true,
    });
  }
  if (
    url.pathname === `/api/accounting/current-accounts/${accountId}` &&
    request.method === "GET"
  )
    return json(response, 200, baseAccount);
  if (
    url.pathname === `/api/accounting/current-accounts/${accountId}` &&
    request.method === "PUT"
  ) {
    const input = await body(request);
    return json(response, 200, {
      ...baseAccount,
      ...input.account,
      isActive: input.isActive,
    });
  }
  if (
    url.pathname ===
    `/api/accounting/reports/current-accounts/${accountId}/statement`
  ) {
    const pageNumber = Number(url.searchParams.get("PageNumber") || "1");
    const pageSize = Number(url.searchParams.get("PageSize") || "20");
    const start = (pageNumber - 1) * pageSize;
    return json(
      response,
      200,
      page(
        statements.slice(start, start + pageSize),
        pageNumber,
        pageSize,
        statements.length,
      ),
    );
  }
  return problem(response, 404, "not_found", "Fixture route bulunamadı.");
});

server.listen(4320, "127.0.0.1");

function page(items, pageNumber, pageSize, totalCount) {
  const totalPages = totalCount ? Math.ceil(totalCount / pageSize) : 0;
  return {
    items,
    pageNumber,
    pageSize,
    totalCount,
    totalPages,
    hasPreviousPage: pageNumber > 1,
    hasNextPage: pageNumber < totalPages,
  };
}
async function body(request) {
  const chunks = [];
  for await (const chunk of request) chunks.push(chunk);
  return JSON.parse(Buffer.concat(chunks).toString("utf8") || "{}");
}
function json(response, status, value) {
  response.writeHead(status, { "Content-Type": "application/json" });
  response.end(status === 204 ? undefined : JSON.stringify(value));
}
function problem(response, status, code, detail) {
  return json(response, status, {
    title: "Kontrollü test hatası",
    status,
    code,
    detail,
    traceId: "accounting-fixture-trace",
  });
}

function makeInvoice(input) {
  const header = input?.header ?? {
    currentAccountId: accountId,
    invoiceNumber: "ALIŞ-FIX-001",
    invoiceDate: "2026-08-24T00:00:00Z",
    dueDate: "2026-09-24T00:00:00Z",
    currencyCode: "TRY",
    exchangeRate: 1,
    description: "Fixture alış faturası",
  };
  const lineInput = input?.lines?.[0] ?? {
    lineNumber: 1,
    productVariantId,
    purchaseQuantity: 10,
    unitOfMeasure: "Adet",
    unitsPerPurchaseUnit: 1,
    priceEntryMode: 1,
    vatRate: 20,
    enteredUnitPrice: 100,
  };
  const net = lineInput.purchaseQuantity * lineInput.enteredUnitPrice;
  const vat = (net * lineInput.vatRate) / 100;
  const total = net + vat;
  const stockQuantity =
    lineInput.purchaseQuantity * lineInput.unitsPerPurchaseUnit;
  return {
    id: invoiceId,
    currentAccountId: header.currentAccountId,
    currentAccountName: baseAccount.name,
    taxNumberSnapshot: baseAccount.taxNumber,
    taxOfficeSnapshot: baseAccount.taxOffice,
    phoneNumberSnapshot: baseAccount.phoneNumber,
    emailSnapshot: baseAccount.email,
    addressSnapshot: baseAccount.addressLine,
    invoiceNumber: header.invoiceNumber,
    invoiceDate: header.invoiceDate,
    dueDate: header.dueDate ?? null,
    currencyCode: "TRY",
    exchangeRate: 1,
    status: 1,
    description: header.description ?? null,
    subtotalExcludingVat: net,
    subtotalIncludingVat: total,
    lineDiscountTotalExcludingVat: 0,
    lineDiscountTotalIncludingVat: 0,
    invoiceDiscountTotalExcludingVat: 0,
    invoiceDiscountTotalIncludingVat: 0,
    totalDiscountExcludingVat: 0,
    totalDiscountIncludingVat: 0,
    netAmountExcludingVat: net,
    vatTotal: vat,
    grandTotalIncludingVat: total,
    totalFinalCostExcludingVat: net,
    totalFinalCostIncludingVat: total,
    paidAmount: 0,
    remainingAmount: total,
    createdAt: "2026-08-24T10:00:00Z",
    updatedAt: null,
    postedAt: null,
    cancelledBy: null,
    cancelledAt: null,
    cancellationReason: null,
    lines: [
      {
        id: lineId,
        lineNumber: lineInput.lineNumber,
        productId: "P00001",
        productVariantId: lineInput.productVariantId,
        productName: product.title,
        variantName: "Boyut: Standart",
        sku: "FIX-001",
        barcode: null,
        purchaseQuantity: lineInput.purchaseQuantity,
        unitOfMeasure: lineInput.unitOfMeasure,
        unitsPerPurchaseUnit: lineInput.unitsPerPurchaseUnit,
        stockQuantity,
        enteredUnitPrice: lineInput.enteredUnitPrice,
        priceEntryMode: lineInput.priceEntryMode,
        unitPriceExcludingVat: lineInput.enteredUnitPrice,
        unitPriceIncludingVat: lineInput.enteredUnitPrice * 1.2,
        vatRate: lineInput.vatRate,
        grossAmountExcludingVat: net,
        grossAmountIncludingVat: total,
        totalDiscountAmountExcludingVat: 0,
        totalDiscountAmountIncludingVat: 0,
        netAmountExcludingVat: net,
        vatAmount: vat,
        totalAmountIncludingVat: total,
        finalUnitCostExcludingVat: net / stockQuantity,
        finalUnitCostIncludingVat: total / stockQuantity,
        allocations: [],
      },
    ],
  };
}
function summary(value) {
  return {
    id: value.id,
    currentAccountId: value.currentAccountId,
    currentAccountName: value.currentAccountName,
    invoiceNumber: value.invoiceNumber,
    invoiceDate: value.invoiceDate,
    currencyCode: value.currencyCode,
    status: value.status,
    grandTotalIncludingVat: value.grandTotalIncludingVat,
  };
}

function makeSalesOrder(input) {
  const header = input?.header ?? {
    currentAccountId: accountId,
    orderNumber: "SAT-FIX-001",
    orderDate: "2026-08-24T00:00:00Z",
    dueDate: "2026-09-24T00:00:00Z",
    currencyCode: "TRY",
    exchangeRate: 1,
    shippingTotal: 0,
    shippingPayer: 0,
    description: "Fixture muhasebe satışı",
  };
  const lineInput = input?.lines?.[0] ?? {
    lineNumber: 1,
    productVariantId,
    quantity: 2,
    unitOfMeasure: "Adet",
    unitsPerSaleUnit: 1,
    priceEntryMode: 1,
    vatRate: 20,
    enteredUnitPrice: 100,
    isInvoiceDiscountEligible: true,
  };
  const net = lineInput.quantity * lineInput.enteredUnitPrice;
  const vat = (net * lineInput.vatRate) / 100;
  const total = net + vat;
  const stockQuantity = lineInput.quantity * lineInput.unitsPerSaleUnit;
  return {
    id: salesOrderId,
    orderNumber: header.orderNumber,
    currentAccountId: header.currentAccountId,
    currentAccountName: baseAccount.name,
    taxNumberSnapshot: baseAccount.taxNumber,
    taxOfficeSnapshot: baseAccount.taxOffice,
    phoneNumberSnapshot: baseAccount.phoneNumber,
    emailSnapshot: baseAccount.email,
    addressSnapshot: baseAccount.addressLine,
    orderDate: header.orderDate,
    dueDate: header.dueDate ?? null,
    currencyCode: "TRY",
    exchangeRate: 1,
    status: 1,
    description: header.description ?? null,
    invoiceDiscountType: header.invoiceDiscountType ?? null,
    invoiceDiscountValue: header.invoiceDiscountValue ?? null,
    invoiceDiscountTaxBasis: header.invoiceDiscountTaxBasis ?? null,
    subtotalExcludingVat: net,
    subtotalIncludingVat: total,
    lineDiscountTotalExcludingVat: 0,
    lineDiscountTotalIncludingVat: 0,
    invoiceDiscountTotalExcludingVat: 0,
    invoiceDiscountTotalIncludingVat: 0,
    totalDiscountExcludingVat: 0,
    totalDiscountIncludingVat: 0,
    netAmountExcludingVat: net,
    shippingTotal: header.shippingTotal ?? 0,
    shippingPayer: header.shippingPayer ?? 0,
    vatTotal: vat,
    grandTotalIncludingVat:
      total + (header.shippingPayer === 2 ? header.shippingTotal : 0),
    paidAmount: 0,
    remainingAmount: 0,
    totalCostOfGoodsSold: 0,
    grossProfitExcludingVat: 0,
    grossProfitMargin: 0,
    salesInvoiceId: null,
    createdAt: "2026-08-24T10:00:00Z",
    updatedAt: null,
    postedAt: null,
    cancelledBy: null,
    cancelledAt: null,
    cancellationReason: null,
    items: [makeSalesLine(lineInput, net, vat, total, stockQuantity)],
  };
}
function makeSalesInvoice(header, order) {
  return {
    id: salesInvoiceId,
    accountingSalesOrderId: order.id,
    currentAccountId: order.currentAccountId,
    currentAccountName: order.currentAccountName,
    taxNumberSnapshot: order.taxNumberSnapshot,
    taxOfficeSnapshot: order.taxOfficeSnapshot,
    phoneNumberSnapshot: order.phoneNumberSnapshot,
    emailSnapshot: order.emailSnapshot,
    addressSnapshot: order.addressSnapshot,
    invoiceNumber: header?.invoiceNumber ?? "F-SAT-FIX-001",
    invoiceDate: header?.invoiceDate ?? order.orderDate,
    dueDate: header?.dueDate ?? order.dueDate,
    currencyCode: order.currencyCode,
    exchangeRate: 1,
    status: order.status,
    description: header?.description ?? null,
    invoiceDiscountType: order.invoiceDiscountType,
    invoiceDiscountValue: order.invoiceDiscountValue,
    invoiceDiscountTaxBasis: order.invoiceDiscountTaxBasis,
    subtotalExcludingVat: order.subtotalExcludingVat,
    subtotalIncludingVat: order.subtotalIncludingVat,
    lineDiscountTotalExcludingVat: 0,
    lineDiscountTotalIncludingVat: 0,
    invoiceDiscountTotalExcludingVat: 0,
    invoiceDiscountTotalIncludingVat: 0,
    totalDiscountExcludingVat: 0,
    totalDiscountIncludingVat: 0,
    netAmountExcludingVat: order.netAmountExcludingVat,
    shippingTotal: order.shippingTotal,
    shippingPayer: order.shippingPayer,
    vatTotal: order.vatTotal,
    grandTotalIncludingVat: order.grandTotalIncludingVat,
    paidAmount: order.paidAmount,
    remainingAmount: order.remainingAmount,
    totalCostOfGoodsSold: order.totalCostOfGoodsSold,
    grossProfitExcludingVat: order.grossProfitExcludingVat,
    grossProfitMargin: order.grossProfitMargin,
    createdAt: "2026-08-24T10:05:00Z",
    postedAt: order.postedAt,
    cancelledBy: order.cancelledBy,
    cancelledAt: order.cancelledAt,
    cancellationReason: order.cancellationReason,
    lines: order.items.map((line) => ({
      ...line,
      stockMovements: undefined,
      accountingSalesOrderItemId: line.id,
      id: salesLineId,
    })),
  };
}
function makeSalesLine(line, net, vat, total, stockQuantity) {
  return {
    id: salesLineId,
    lineNumber: line.lineNumber,
    productId: product.id,
    productVariantId: line.productVariantId,
    productName: product.title,
    variantName: "Boyut: Standart",
    sku: "FIX-001",
    barcode: null,
    quantity: line.quantity,
    unitOfMeasure: line.unitOfMeasure,
    unitsPerSaleUnit: line.unitsPerSaleUnit,
    stockQuantity,
    enteredUnitPrice: line.enteredUnitPrice,
    priceEntryMode: line.priceEntryMode,
    unitPriceExcludingVat: line.enteredUnitPrice,
    unitPriceIncludingVat: line.enteredUnitPrice * 1.2,
    vatRate: line.vatRate,
    lineDiscountType: line.lineDiscountType ?? null,
    lineDiscountValue: line.lineDiscountValue ?? null,
    lineDiscountTaxBasis: line.lineDiscountTaxBasis ?? null,
    lineDiscountUnitBasis: line.lineDiscountUnitBasis ?? null,
    isInvoiceDiscountEligible: line.isInvoiceDiscountEligible ?? true,
    grossAmountExcludingVat: net,
    grossAmountIncludingVat: total,
    lineDiscountAmountExcludingVat: 0,
    lineDiscountAmountIncludingVat: 0,
    invoiceDiscountShareExcludingVat: 0,
    invoiceDiscountShareIncludingVat: 0,
    totalDiscountAmountExcludingVat: 0,
    totalDiscountAmountIncludingVat: 0,
    netAmountExcludingVat: net,
    vatAmount: vat,
    totalAmountIncludingVat: total,
    costOfGoodsSold: 0,
    grossProfitExcludingVat: 0,
    grossProfitMargin: 0,
    stockMovements: [],
    costLayerConsumptions: [],
  };
}
function postSalesFixture() {
  const movement = {
    id: "15151515-1515-4151-8151-151515151515",
    stockMovementId: "16161616-1616-4161-8161-161616161616",
    quantity: -salesOrder.items[0].stockQuantity,
  };
  const consumption = {
    id: "17171717-1717-4171-8171-171717171717",
    inventoryCostLayerId: "18181818-1818-4181-8181-181818181818",
    stockMovementId: movement.stockMovementId,
    quantity: salesOrder.items[0].stockQuantity,
    unitCostExcludingVat: 60,
    totalCostExcludingVat: salesOrder.items[0].stockQuantity * 60,
    createdAt: "2026-08-24T12:00:00Z",
  };
  salesOrder.status = 2;
  salesOrder.postedAt = "2026-08-24T12:00:00Z";
  salesOrder.remainingAmount = salesOrder.grandTotalIncludingVat;
  salesOrder.totalCostOfGoodsSold = consumption.totalCostExcludingVat;
  salesOrder.grossProfitExcludingVat =
    salesOrder.netAmountExcludingVat - consumption.totalCostExcludingVat;
  salesOrder.grossProfitMargin =
    (salesOrder.grossProfitExcludingVat / salesOrder.netAmountExcludingVat) *
    100;
  salesOrder.items[0].stockMovements = [movement];
  salesOrder.items[0].costLayerConsumptions = [consumption];
  salesOrder.items[0].costOfGoodsSold = consumption.totalCostExcludingVat;
  salesOrder.items[0].grossProfitExcludingVat =
    salesOrder.grossProfitExcludingVat;
  salesOrder.items[0].grossProfitMargin = salesOrder.grossProfitMargin;
  if (salesInvoice) {
    const header = {
      invoiceNumber: salesInvoice.invoiceNumber,
      invoiceDate: salesInvoice.invoiceDate,
      dueDate: salesInvoice.dueDate,
      description: salesInvoice.description,
    };
    salesInvoice = makeSalesInvoice(header, salesOrder);
  }
}
function orderHeaderFrom(order) {
  return {
    currentAccountId: order.currentAccountId,
    orderNumber: order.orderNumber,
    orderDate: order.orderDate,
    dueDate: order.dueDate,
    currencyCode: order.currencyCode,
    exchangeRate: order.exchangeRate,
    shippingTotal: order.shippingTotal,
    shippingPayer: order.shippingPayer,
    description: order.description,
    invoiceDiscountType: order.invoiceDiscountType,
    invoiceDiscountValue: order.invoiceDiscountValue,
    invoiceDiscountTaxBasis: order.invoiceDiscountTaxBasis,
  };
}
function salesOrderSummary(value) {
  return {
    id: value.id,
    orderNumber: value.orderNumber,
    currentAccountId: value.currentAccountId,
    currentAccountName: value.currentAccountName,
    orderDate: value.orderDate,
    status: value.status,
    grandTotalIncludingVat: value.grandTotalIncludingVat,
    salesInvoiceId: value.salesInvoiceId,
  };
}
function salesInvoiceSummary(value) {
  return {
    id: value.id,
    accountingSalesOrderId: value.accountingSalesOrderId,
    invoiceNumber: value.invoiceNumber,
    currentAccountId: value.currentAccountId,
    currentAccountName: value.currentAccountName,
    invoiceDate: value.invoiceDate,
    status: value.status,
    grandTotalIncludingVat: value.grandTotalIncludingVat,
  };
}
