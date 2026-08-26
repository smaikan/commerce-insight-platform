import { existsSync, readFileSync, readdirSync, statSync } from "node:fs";
import { basename, dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const toolsDirectory = dirname(fileURLToPath(import.meta.url));
const docsRoot = resolve(toolsDirectory, "..");
const referenceRoot = join(docsRoot, "03-endpoint-referansi");
const openApi = JSON.parse(readFileSync(join(docsRoot, "openapi.json"), "utf8"));
const httpMethods = ["get", "post", "put", "patch", "delete"];
const utf8Decoder = new TextDecoder("utf-8", { fatal: true });
const referenceSections = new Set([
  "01-kimlik-ve-kullanicilar",
  "02-katalog",
  "03-satis-ve-siparis",
  "04-operasyon",
  "05-muhasebe",
  "06-magaza-ve-iletisim",
]);

function walk(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    return statSync(path).isDirectory() ? walk(path) : [path];
  });
}

function normalizedKey(method, path) {
  return `${method.toLowerCase()} ${path.toLowerCase().replace(/\{[^}]+\}/g, "{}")}`;
}

const allFiles = walk(docsRoot);
const markdownFiles = allFiles.filter((path) => path.endsWith(".md"));
const failures = [];

const referenceDirectories = allFiles
  .map(dirname)
  .filter((directory) => directory.startsWith(referenceRoot) && directory !== referenceRoot);
for (const directory of new Set(referenceDirectories)) {
  if (!existsSync(join(directory, "README.md"))) failures.push(`Görev klasöründe README yok: ${directory}`);
}

for (const path of [...markdownFiles, join(docsRoot, "openapi.json")]) {
  try {
    utf8Decoder.decode(readFileSync(path));
  } catch {
    failures.push(`UTF-8 değil: ${path}`);
  }
}

const documentedOperations = new Map();
let jsonExampleCount = 0;

for (const path of markdownFiles) {
  const content = readFileSync(path, "utf8");

  for (const match of content.matchAll(/\[[^\]]*\]\(([^)]+)\)/g)) {
    const href = match[1].trim().replace(/^<|>$/g, "").split("#")[0];
    if (!href || /^[a-z]+:/i.test(href)) continue;
    const target = resolve(dirname(path), decodeURIComponent(href));
    if (!existsSync(target)) failures.push(`Kırık bağlantı: ${path} -> ${match[1]}`);
  }

  for (const match of content.matchAll(/```json\s*\n([\s\S]*?)```/g)) {
    jsonExampleCount += 1;
    try {
      JSON.parse(match[1]);
    } catch (error) {
      failures.push(`Geçersiz JSON: ${path} -> ${error.message}`);
    }
  }

  const heading = content.replace(/^\uFEFF/, "").match(/^#\s+(GET|POST|PUT|PATCH|DELETE)\s+`?(\/api\/[^`\n ]+)`?/im);
  if (!heading) continue;
  const key = normalizedKey(heading[1], heading[2]);
  if (documentedOperations.has(key)) failures.push(`Çift endpoint dokümanı: ${key}`);
  documentedOperations.set(key, path);
  if (!/Yetki|Security|AdminOnly|AllowAnonymous/i.test(content)) failures.push(`Yetki bilgisi yok: ${path}`);
  if (!/^- Görev alanı: \*\*.+\*\*\.$/im.test(content)) failures.push(`Görev alanı yok: ${path}`);

  const parts = relative(referenceRoot, path).split("/");
  if (!referenceSections.has(parts[0])) failures.push(`Endpoint ana iş alanı dışında: ${path}`);
  if (parts.length < 3) failures.push(`Endpoint kaynak klasörüne ayrılmamış: ${path}`);
  if (/^(GET|POST|PUT|PATCH|DELETE)--|--api-|[{}]/i.test(basename(path))) failures.push(`Teknik veya anlaşılmaz dosya adı: ${path}`);

  const parentIndex = join(dirname(path), "README.md");
  if (existsSync(parentIndex) && !readFileSync(parentIndex, "utf8").includes(`./${basename(path)}`)) {
    failures.push(`Endpoint yerel görev indeksinde yok: ${path}`);
  }
}

const openApiOperations = [];
for (const [path, pathItem] of Object.entries(openApi.paths ?? {})) {
  for (const method of httpMethods) {
    if (pathItem[method]) openApiOperations.push(normalizedKey(method, path));
  }
}

if (openApiOperations.length !== 286) failures.push(`OpenAPI operasyon sayısı 286 değil: ${openApiOperations.length}`);
for (const key of openApiOperations) {
  if (!documentedOperations.has(key)) failures.push(`Endpoint dokümanı eksik: ${key}`);
}
for (const key of documentedOperations.keys()) {
  if (!openApiOperations.includes(key)) failures.push(`OpenAPI'de olmayan endpoint dokümanı: ${key}`);
}

if (failures.length) {
  console.error(failures.join("\n"));
  process.exit(1);
}

console.log(`${openApiOperations.length} endpoint, ${markdownFiles.length} Markdown dosyası ve ${jsonExampleCount} JSON örneği doğrulandı.`);
