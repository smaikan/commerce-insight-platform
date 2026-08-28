import { readFileSync, readdirSync, statSync, writeFileSync } from "node:fs";
import { basename, dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const toolsDirectory = dirname(fileURLToPath(import.meta.url));
const docsRoot = resolve(toolsDirectory, "..");
const referenceRoot = join(docsRoot, "03-endpoint-referansi");
const openApi = JSON.parse(readFileSync(join(docsRoot, "openapi.json"), "utf8"));

const httpMethods = ["get", "post", "put", "patch", "delete"];
const methodOrder = new Map(httpMethods.map((method, index) => [method, index]));

const sections = {
  "01-kimlik-ve-kullanicilar": "Kimlik ve kullanıcılar",
  "02-katalog": "Katalog",
  "03-satis-ve-siparis": "Satış ve sipariş",
  "04-operasyon": "Operasyon",
  "05-muhasebe": "Muhasebe",
  "06-magaza-ve-iletisim": "Mağaza ve iletişim",
};

const adminTags = new Set([
  "AccountingReports",
  "AccountingSalesOrders",
  "BankAccounts",
  "CashAccounts",
  "Coupons",
  "CurrentAccounts",
  "Dashboard",
  "Expenses",
  "FinancialTransactions",
  "InventoryCostLayers",
  "Payments",
  "ProductVariantCostHistory",
  "PurchaseInvoices",
  "SalesInvoices",
  "StockMovements",
  "StockReservationMaintenance",
]);

function walkMarkdown(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    return statSync(path).isDirectory()
      ? walkMarkdown(path)
      : name.endsWith(".md") && name !== "README.md"
        ? [path]
        : [];
  });
}

function normalizedOperationKey(method, path) {
  return `${method.toLowerCase()} ${path.toLowerCase().replace(/\{[^}]+\}/g, "{}")}`;
}

function parseEndpointDocument(path) {
  const content = readFileSync(path, "utf8").replace(/^\uFEFF/, "");
  const heading = content.match(/^#\s+(GET|POST|PUT|PATCH|DELETE)\s+`?(\/api\/[^`\n ]+)`?/im);
  if (!heading) return null;

  const purposeLine = content.match(/^- İşlev:\s*(.+)$/im);
  const prose = content
    .slice(heading.index + heading[0].length)
    .split(/\r?\n/)
    .map((line) => line.trim())
    .find((line) =>
      line &&
      !line.startsWith("#") &&
      !line.startsWith("-") &&
      !line.startsWith("|") &&
      !line.startsWith("```") &&
      !line.startsWith(">") &&
      !line.startsWith("{") &&
      !line.startsWith("[") &&
      !/^(GET|POST|PUT|PATCH|DELETE)\s+\/api\//.test(line),
    );

  return {
    method: heading[1].toLowerCase(),
    documentedPath: heading[2],
    filePath: path,
    purpose: (purposeLine?.[1] ?? prose ?? "Ayrıntılı endpoint sözleşmesi.")
      .replace(/\|/g, "\\|")
      .replace(/\s+/g, " ")
      .trim(),
  };
}

function accessLabel(method, path, operation) {
  const tag = operation.tags?.[0] ?? "";
  const isAnonymous = Array.isArray(operation.security) && operation.security.length === 0;

  if (isAnonymous) {
    if (path.startsWith("/api/payments/iyzico/")) return "Provider";
    if (path === "/api/contact-messages" && method === "post") return "Public + form koruması";
    if (path.startsWith("/api/guest-orders")) {
      if (path.endsWith("/access-links") || path.endsWith("/access/exchange")) return "Public + Origin";
      return "Guest session";
    }
    if (path === "/api/cart/checkout/guest") return "Guest checkout";
    if (path.startsWith("/api/cart")) return "Public / guest cart";
    if (path.includes("/favorites")) return "User / guest session";
    return "Public";
  }

  if (adminTags.has(tag)) return "Admin";
  if (tag === "Addresses" || tag === "GuestSession") return "User";
  if (tag === "Users") return path.startsWith("/api/users/me") ? "User" : "Admin";
  if (tag === "ContactMessages" || tag === "StoreSettings") return "Admin";
  if (tag === "GuestOrders") return path === "/api/guest-orders/claim" ? "User" : "Guest session";
  if (tag === "Cart") return path === "/api/cart/merge-guest" ? "User" : "Public / guest cart";
  if (tag === "Orders") {
    if (path.includes("/import") || path.includes("/admin/") || path.includes("/reservations/") || (path === "/api/orders" && method === "get") || path.endsWith("/status")) return "Admin";
    return "User";
  }
  if (tag === "Returns") {
    if ((path === "/api/returns" && method === "get") || path.includes("/admin/") || /\/(approve|reject|receive|complete)$/.test(path)) return "Admin";
    return "User";
  }
  if (tag === "ProductEngagement") {
    if (path.includes("/metrics") || path.includes("/approval")) return "Admin";
    return "User";
  }

  return "Admin";
}

const documents = walkMarkdown(referenceRoot)
  .map(parseEndpointDocument)
  .filter(Boolean);
const documentsByOperation = new Map();

for (const document of documents) {
  const key = normalizedOperationKey(document.method, document.documentedPath);
  if (documentsByOperation.has(key)) {
    throw new Error(`Aynı endpoint için birden fazla doküman var: ${key}`);
  }
  documentsByOperation.set(key, document);
}

const operations = [];
for (const [path, pathItem] of Object.entries(openApi.paths ?? {})) {
  for (const method of httpMethods) {
    const operation = pathItem[method];
    if (!operation) continue;
    const key = normalizedOperationKey(method, path);
    const document = documentsByOperation.get(key);
    if (!document) throw new Error(`Endpoint dokümanı bulunamadı: ${method.toUpperCase()} ${path}`);

    const section = relative(referenceRoot, document.filePath).split("/")[0];
    if (!sections[section]) throw new Error(`Bilinmeyen doküman bölümü: ${section}`);
    operations.push({ method, path, operation, document, section });
  }
}

if (operations.length !== 287) {
  throw new Error(`Beklenen 287 operasyon yerine ${operations.length} operasyon bulundu.`);
}

const knownKeys = new Set(operations.map(({ method, path }) => normalizedOperationKey(method, path)));
const staleDocuments = documents.filter(({ method, documentedPath }) => !knownKeys.has(normalizedOperationKey(method, documentedPath)));
if (staleDocuments.length) {
  throw new Error(`OpenAPI'de bulunmayan endpoint dokümanları: ${staleDocuments.map((item) => item.filePath).join(", ")}`);
}

function operationSort(left, right) {
  return left.path.localeCompare(right.path, "en") || methodOrder.get(left.method) - methodOrder.get(right.method);
}

const directoryTitles = {
  "01-kimlik-ve-kullanicilar": "Kimlik ve kullanıcılar",
  "02-katalog": "Katalog",
  "03-satis-ve-siparis": "Satış ve sipariş",
  "04-operasyon": "Operasyon",
  "05-muhasebe": "Muhasebe",
  "06-magaza-ve-iletisim": "Mağaza ve iletişim",
  "kimlik-dogrulama": "Kimlik doğrulama",
  "hesabim": "Hesabım",
  "adreslerim": "Adreslerim",
  "musteri-yonetimi": "Müşteri yönetimi",
  "misafir-oturumu": "Misafir oturumu",
  "urunler": "Ürünler",
  "storefront": "Storefront",
  "yonetim": "Yönetim",
  "filtre-secenekleri": "Filtre seçenekleri",
  "varyantlar": "Varyantlar",
  "urun-gorselleri": "Ürün görselleri",
  "markalar": "Markalar",
  "koleksiyonlar": "Koleksiyonlar",
  "kategoriler": "Kategoriler",
  "etiketler": "Etiketler",
  "urun-etkilesimleri": "Ürün etkileşimleri",
  "favoriler": "Favoriler",
  "puanlama": "Puanlama",
  "yorumlar": "Yorumlar",
  "metrikler": "Metrikler",
  "aktiviteler": "Aktiviteler",
  "sepet": "Sepet",
  "kalemler": "Sepet kalemleri",
  "kupon": "Sepet kuponu",
  "oturum-devri": "Sepet oturum devri",
  "checkout": "Checkout",
  "misafir": "Misafir işlemleri",
  "siparisler": "Siparişler",
  "uye": "Üye işlemleri",
  "erisim": "Misafir sipariş erişimi",
  "hesaba-devret": "Hesaba devretme",
  "dis-sistem-aktarimi": "Dış sistem aktarımı",
  "odemeler": "Ödemeler",
  "iyzico": "iyzico",
  "provider-bildirimleri": "Provider bildirimleri",
  "iadeler": "İade ve değişim",
  "dashboard": "Dashboard",
  "stok-hareketleri": "Stok hareketleri",
  "stok-rezervasyonlari": "Stok rezervasyonları",
  "kargo-yontemleri": "Kargo yöntemleri",
  "vergi-oranlari": "Vergi oranları",
  "kuponlar": "Kuponlar",
  "satis-siparisleri": "Satış siparişleri",
  "satis-faturalari": "Satış faturaları",
  "alis-faturalari": "Alış faturaları",
  "giderler": "Giderler",
  "odemeler": "Ödemeler",
  "kasa-hesaplari": "Kasa hesapları",
  "banka-hesaplari": "Banka hesapları",
  "finansal-hareketler": "Finansal hareketler",
  "cari-hesaplar": "Cari hesaplar",
  "maliyet": "Maliyet",
  "acilis-maliyetleri": "Açılış maliyetleri",
  "varyant-maliyet-gecmisi": "Varyant maliyet geçmişi",
  "raporlar": "Raporlar",
  "satis": "Satış raporları",
  "alis": "Alış raporları",
  "stok-ve-maliyet": "Stok ve maliyet raporları",
  "karlilik": "Kârlılık raporları",
  "cari-ve-tahsilat": "Cari ve tahsilat raporları",
  "kasa-ve-banka": "Kasa ve banka raporları",
  "vergi": "Vergi raporları",
  "magaza-ayarlari": "Mağaza ayarları",
  "bannerlar": "Bannerlar",
  "ana-banner": "Ana banner",
  "mobil-ana-banner": "Mobil ana banner",
  "alt-banner-1": "Alt banner 1",
  "alt-banner-2": "Alt banner 2",
  "alt-banner-3": "Alt banner 3",
  "alt-banner-4": "Alt banner 4",
  "alt-banner-5": "Alt banner 5",
  "iletisim-formu": "İletişim formu",
  "iletisim-yonetimi": "İletişim yönetimi",
};

function titleForDirectory(directory) {
  const name = basename(directory);
  return directoryTitles[name] ?? name.replace(/^\d{2}-/, "").replaceAll("-", " ");
}

function childDirectories(directory) {
  return readdirSync(directory)
    .map((name) => join(directory, name))
    .filter((path) => statSync(path).isDirectory())
    .sort((left, right) => basename(left).localeCompare(basename(right), "tr"));
}

function directSharedDocuments(directory) {
  return readdirSync(directory)
    .filter((name) => name.endsWith(".md") && name !== "README.md")
    .map((name) => join(directory, name))
    .filter((path) => !parseEndpointDocument(path))
    .sort((left, right) => basename(left).localeCompare(basename(right), "tr"));
}

function countOperations(directory) {
  const prefix = `${directory}/`;
  return operations.filter(({ document }) => document.filePath.startsWith(prefix)).length;
}

function firstHeading(file) {
  return readFileSync(file, "utf8").replace(/^\uFEFF/, "").match(/^#\s+(.+)$/m)?.[1]
    ?? basename(file, ".md").replaceAll("-", " ");
}

const taskWords = {
  acilis: "açılış", adres: "adres", adresimi: "adresimi", aktifligi: "aktifliği", aktiflik: "aktiflik", ayarlarini: "ayarlarını",
  alis: "alış", analitigini: "analitiğini", arama: "arama", atama: "atama", bagla: "bağla", baglantisi: "bağlantısı",
  bakiyesini: "bakiyesini", baslat: "başlat", banka: "banka", banneri: "bannerı", birlestir: "birleştir",
  bolum: "bölüm", cikar: "çıkar", cikarma: "çıkarma", cikis: "çıkış", degistir: "değiştir", detayini: "detayını", devret: "devret",
  dis: "dış", donustur: "dönüştür", e: "e", ekle: "ekle", eposta: "e-posta", erisim: "erişim",
  dagitimlarini: "dağıtımlarını", degerlemesini: "değerlemesini", eslestir: "eşleştir", faturanin: "faturanın",
  faturasi: "faturası", faturalarini: "faturalarını", faturaya: "faturaya",
  gecen: "geçen", gecmisini: "geçmişini", gonder: "gönder", gorsel: "görsel", gorsellerini: "görsellerini",
  guncelle: "güncelle", hesabi: "hesabı", hesabimi: "hesabımı", hesap: "hesap", ice: "içe", iade: "iade", iletisim: "iletişim",
  iliskilerini: "ilişkilerini", iptal: "iptal", isle: "işle", islemi: "işlemi", islemlerini: "işlemlerini",
  kalem: "kalem", kalemini: "kalemini", kalemlerini: "kalemlerini", kalan: "kalan", kapat: "kapat", karliligini: "kârlılığını",
  katmani: "katmanı", katmanlarini: "katmanlarını", kismi: "kısmi",
  kategori: "kategori", kategoriye: "kategoriye", kaydet: "kaydet", kayit: "kayıt", kesinlestir: "kesinleştir",
  kimligini: "kimliğini", koleksiyon: "koleksiyon", koleksiyona: "koleksiyona", kullanicilar: "kullanıcılar",
  kullanimini: "kullanımını", kuponu: "kuponu", maliyet: "maliyet", maliyetini: "maliyetini", markaya: "markaya",
  magaza: "mağaza", mesaji: "mesajı", metriklerini: "metriklerini", misafir: "misafir", musteri: "müşteri", musterinin: "müşterinin",
  musteriye: "müşteriye", yanit: "yanıt",
  not: "not", odeme: "ödeme", odemeyi: "ödemeyi", odemeler: "ödemeler", olustur: "oluştur", onayla: "onayla",
  onerilerini: "önerilerini", onizle: "önizle", one: "öne", oturumu: "oturumu", oturumlari: "oturumları",
  parolami: "parolamı", parolayi: "parolayı", puanla: "puanla", reddet: "reddet", rezervasyonlari: "rezervasyonları",
  rol: "rol", rolu: "rolü", satis: "satış", sepeti: "sepeti", sepetten: "sepetten", sifirla: "sıfırla",
  seo: "SEO", sifirlama: "sıfırlama", siparis: "sipariş", siparisi: "siparişi", siparisin: "siparişin",
  siparisinden: "siparişinden", siparisini: "siparişini", siparislerini: "siparişlerini",
  sil: "sil", stok: "stok", suresi: "süresi", tahsilatlari: "tahsilatları", talebi: "talebi", talebini: "talebini",
  taleplerimi: "taleplerimi", taleplerini: "taleplerini", tamamla: "tamamla", teslim: "teslim", toplu: "toplu",
  tuketimlerini: "tüketimlerini", urun: "ürün", urune: "ürüne", urunu: "ürünü", urununu: "ürününü",
  urunun: "ürünün", urunleri: "ürünleri", urunlerini: "ürünlerini",
  uye: "üye", varyant: "varyant", varyanti: "varyantı", varyantin: "varyantın", varyantlarini: "varyantlarını",
  varsayilan: "varsayılan", vadesi: "vadesi", verisini: "verisini", yayin: "yayın", yayinlanan: "yayınlanan",
  yonetim: "yönetim", yorumu: "yorumu", yorumlarini: "yorumlarını", yaz: "yaz",
};

function taskTitle(file) {
  const slug = basename(file, ".md").replaceAll("e-posta", "eposta");
  const words = slug.split("-").map((word) => taskWords[word] ?? word);
  const title = words.join(" ");
  return title.charAt(0).toLocaleUpperCase("tr-TR") + title.slice(1);
}

const genericPurposes = new Set([
  "Kaynağı veya filtrelenmiş kaynak listesini okur.",
  "Yeni kaynak veya iş akışı adımı oluşturur/başlatır.",
  "Kaynağı ya da ilişkisini kaldırır.",
  "Kaynağın güncellenebilir alanlarını değiştirir.",
  "Kaynağı iptal eder veya muhasebe ters kaydını oluşturur.",
  "Kaynağın belirli durum veya alanlarını değiştirir.",
]);

const taskVerbs = {
  bagla: "bağlar",
  birlestir: "birleştirir",
  cikar: "çıkarır",
  degistir: "değiştirir",
  ekle: "ekler",
  et: "eder",
  getir: "getirir",
  guncelle: "günceller",
  kapat: "kapatır",
  kaydet: "kaydeder",
  listele: "listeler",
  olustur: "oluşturur",
  puanla: "puanlar",
  reddet: "reddeder",
  sil: "siler",
  temizle: "temizler",
  yap: "yapar",
  yaz: "yazar",
};

function taskPurpose(file) {
  const slug = basename(file, ".md");
  const lastSlug = slug.split("-").at(-1);
  const verb = taskVerbs[lastSlug];
  if (!verb) throw new Error(`Görev cümlesi fiili tanımlı değil: ${file}`);
  const title = taskTitle(file);
  const lastSpace = title.lastIndexOf(" ");
  return `${lastSpace < 0 ? "" : `${title.slice(0, lastSpace)} `}${verb}.`;
}

// Her ayrıntı sayfasını gerçek klasör yolundan türetilen görev alanı ve doğrulanmış erişim seviyesiyle etiketle.
for (const item of operations) {
  const access = accessLabel(item.method, item.path, item.operation);
  const breadcrumb = relative(referenceRoot, dirname(item.document.filePath))
    .split("/")
    .map((part) => directoryTitles[part] ?? part.replace(/^\d{2}-/, "").replaceAll("-", " "))
    .join(" → ");
  let content = readFileSync(item.document.filePath, "utf8").replace(/^\uFEFF/, "");

  content = content.replace(/^- İşlev:\s*(.+)$/im, (line, purpose) =>
    genericPurposes.has(purpose.trim()) ? `- İşlev: ${taskPurpose(item.document.filePath)}` : line,
  );

  if (/^- (?:İşlev|Görev) alanı:.*$/im.test(content)) {
    content = content.replace(/^- (?:İşlev|Görev) alanı:.*$/im, `- Görev alanı: **${breadcrumb}**.`);
  } else {
    content = content.replace(
      /^(#\s+(?:GET|POST|PUT|PATCH|DELETE)\s+`?\/api\/[^\n]+`?)/im,
      `$1\n\n- Görev alanı: **${breadcrumb}**.`,
    );
  }

  if (/^- Yetki:.*$/im.test(content)) {
    content = content.replace(/^- Yetki:.*$/im, `- Yetki: **${access}**.`);
  } else if (!/^## Yetki/im.test(content) && !/^- Security:/im.test(content)) {
    content = content.replace(/^- Görev alanı:.*$/im, `$&\n- Yetki: **${access}**.`);
  }
  writeFileSync(item.document.filePath, content);
}

const indexDirectories = [];
function collectDirectories(directory) {
  for (const child of childDirectories(directory)) {
    indexDirectories.push(child);
    collectDirectories(child);
  }
}
collectDirectories(referenceRoot);

for (const directory of indexDirectories.sort((left, right) => right.split("/").length - left.split("/").length)) {
  const localOperations = operations
    .filter(({ document }) => dirname(document.filePath) === directory)
    .sort(operationSort);
  const children = childDirectories(directory);
  const shared = directSharedDocuments(directory);
  const total = countOperations(directory);
  const rootLink = relative(directory, join(docsRoot, "README.md")).replaceAll("\\", "/");
  const referenceLink = relative(directory, join(referenceRoot, "README.md")).replaceAll("\\", "/");
  const lines = [
    `# ${titleForDirectory(directory)}`,
    "",
    `[API dokümantasyonuna dön](${rootLink}) · [Tam endpoint referansına dön](${referenceLink})`,
    "",
    `Bu bölüm **${total} operasyon** içerir. Aradığınız işlemi aşağıdaki görev başlıklarından seçin.`,
  ];

  if (children.length) {
    lines.push("", "## Görev alanları", "", "| Alan | Operasyon |", "| --- | ---: |");
    for (const child of children) {
      lines.push(`| [${titleForDirectory(child)}](./${basename(child)}/README.md) | ${countOperations(child)} |`);
    }
  }

  if (localOperations.length) {
    lines.push("", "## İşlemler", "", "| Görev | Metot ve endpoint | Yetki |", "| --- | --- | --- |");
    for (const item of localOperations) {
      const label = taskTitle(item.document.filePath);
      lines.push(`| [${label}](./${basename(item.document.filePath)}) | **${item.method.toUpperCase()}** \`${item.path}\` | ${accessLabel(item.method, item.path, item.operation)} |`);
    }
  }

  if (shared.length) {
    lines.push("", "## Ortak sözleşmeler", "");
    for (const file of shared) lines.push(`- [${firstHeading(file)}](./${basename(file)})`);
  }

  lines.push("");
  writeFileSync(join(directory, "README.md"), lines.join("\n"));
}

const rootLines = [
  "# Tam Endpoint Referansı",
  "",
  "[API dokümantasyonuna dön](../README.md)",
  "",
  "Bu referans güncel OpenAPI sözleşmesindeki **287 operasyonun tamamını** kapsar. Her bölümde endpoint, erişim seviyesi, kısa amaç ve ayrıntılı sözleşme bağlantısı bulunur.",
  "",
  "| Bölüm | Operasyon |",
  "| --- | ---: |",
];

for (const [section, title] of Object.entries(sections)) {
  const count = operations.filter((operation) => operation.section === section).length;
  rootLines.push(`| [${title}](./${section}/README.md) | ${count} |`);
}

rootLines.push(
  "",
  "## Sık aranan görevler",
  "",
  "| Yapmak istediğiniz iş | Doğrudan bağlantı |",
  "| --- | --- |",
  "| Kullanıcı kaydı veya giriş | [Kimlik doğrulama](./01-kimlik-ve-kullanicilar/kimlik-dogrulama/README.md) |",
  "| Storefront ürünlerini listeleme ve arama | [Storefront ürünleri](./02-katalog/urunler/storefront/README.md) |",
  "| Ürün oluşturma, güncelleme veya yayımlama | [Ürün yönetimi](./02-katalog/urunler/yonetim/README.md) |",
  "| Varyant ve stok/fiyat işlemleri | [Varyantlar](./02-katalog/varyantlar/README.md) |",
  "| Sepete ürün ekleme veya sepeti güncelleme | [Sepet](./03-satis-ve-siparis/sepet/README.md) |",
  "| Üye siparişi oluşturma veya iptal etme | [Üye siparişleri](./03-satis-ve-siparis/siparisler/uye/README.md) |",
  "| Misafir siparişini görüntüleme veya iptal etme | [Misafir siparişleri](./03-satis-ve-siparis/siparisler/misafir/README.md) |",
  "| iyzico ödeme formunu başlatma | [iyzico ödemeleri](./03-satis-ve-siparis/odemeler/iyzico/README.md) |",
  "| İade/değişim talebi ve yönetimi | [İade ve değişim](./03-satis-ve-siparis/iadeler/README.md) |",
  "| Stok, rezervasyon, kargo, vergi veya kupon | [Operasyon](./04-operasyon/README.md) |",
  "| Fatura, cari, kasa/banka veya finansal rapor | [Muhasebe](./05-muhasebe/README.md) |",
  "| Banner veya mağaza ayarlarını yönetme | [Mağaza ve iletişim](./06-magaza-ve-iletisim/README.md) |",
  "| İletişim formundan mesaj gönderme | [İletişim formu](./06-magaza-ve-iletisim/iletisim-formu/README.md) |",
  "| Gelen iletişim mesajını yanıtlama | [İletişim yönetimi](./06-magaza-ve-iletisim/iletisim-yonetimi/README.md) |",
  "",
  "## Yetki etiketleri",
  "",
  "- **Public:** Bearer token gerekmez.",
  "- **Guest session / Guest checkout:** HttpOnly guest cookie ve endpoint bazında CSRF, Origin veya idempotency koruması gerekir.",
  "- **User:** Bearer token ve kaynak sahipliği gerekir.",
  "- **Admin:** Bearer token ve `AdminOnly` policy gerekir.",
  "- **Provider:** Kullanıcı tokenı yerine sağlayıcı imzası/tokenı doğrulanır.",
  "",
  "Ayrıntılar için [kimlik doğrulama ve yetkilendirme rehberini](../01-baslangic/02-kimlik-dogrulama-ve-yetkilendirme.md) okuyun.",
  "",
);

writeFileSync(join(referenceRoot, "README.md"), rootLines.join("\n"));

console.log(`287 operasyon için ${indexDirectories.length} görev indeksi üretildi.`);
