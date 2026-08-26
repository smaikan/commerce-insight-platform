import { readFileSync, readdirSync, statSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const toolsDirectory = dirname(fileURLToPath(import.meta.url));
const docsRoot = resolve(toolsDirectory, "..");
const openApi = JSON.parse(readFileSync(join(docsRoot, "openapi.json"), "utf8"));
const httpMethods = ["get", "post", "put", "patch", "delete"];

function walkMarkdown(directory) {
  return readdirSync(directory).flatMap((name) => {
    const path = join(directory, name);
    return statSync(path).isDirectory()
      ? walkMarkdown(path)
      : name.endsWith(".md")
        ? [path]
        : [];
  });
}

function normalizedKey(method, path) {
  return `${method.toLowerCase()} ${path.toLowerCase().replace(/\{[^}]+\}/g, "{}")}`;
}

function resolveSchema(schema) {
  if (!schema?.$ref) return schema;
  return openApi.components?.schemas?.[schema.$ref.split("/").at(-1)];
}

const arrayPropertyNames = new Set();
const propertyTypes = new Map();
for (const originalSchema of Object.values(openApi.components?.schemas ?? {})) {
  const schema = resolveSchema(originalSchema);
  for (const [name, originalProperty] of Object.entries(schema?.properties ?? {})) {
    const property = resolveSchema(originalProperty);
    const type = property?.type ?? (property?.properties ? "object" : "unknown");
    if (!propertyTypes.has(name)) propertyTypes.set(name, new Set());
    propertyTypes.get(name).add(type);
  }
}
for (const [name, types] of propertyTypes) {
  if (types.size === 1 && types.has("array")) arrayPropertyNames.add(name);
}

const operations = new Map();
for (const [path, pathItem] of Object.entries(openApi.paths ?? {})) {
  for (const method of httpMethods) {
    if (pathItem[method]) operations.set(normalizedKey(method, path), pathItem[method]);
  }
}

function requestSchema(operation) {
  return resolveSchema(operation?.requestBody?.content?.["application/json"]?.schema);
}

function responseSchema(operation, sectionTitle) {
  const explicitStatus = sectionTitle.match(/\b(200|201|202)\b/)?.[1];
  const responses = operation?.responses ?? {};
  const status = explicitStatus && responses[explicitStatus]
    ? explicitStatus
    : Object.keys(responses).find((code) => /^2\d\d$/.test(code) && code !== "204");
  return resolveSchema(responses[status]?.content?.["application/json"]?.schema);
}

function coerce(value, originalSchema, propertyName) {
  const schema = resolveSchema(originalSchema);
  if (value === null || value === undefined) return value;

  const expectsArray = schema?.type === "array" || (!schema && arrayPropertyNames.has(propertyName));
  if (expectsArray) {
    const values = Array.isArray(value) ? value : [value];
    return values.map((item) => coerce(item, schema?.items));
  }

  if (Array.isArray(value)) return value.map((item) => coerce(item));
  if (typeof value !== "object") return value;

  const properties = schema?.properties ?? {};
  return Object.fromEntries(
    Object.entries(value).map(([name, child]) => [name, coerce(child, properties[name], name)]),
  );
}

function nearestSectionTitle(content, offset) {
  const headings = [...content.slice(0, offset).matchAll(/^##+\s+(.+)$/gm)];
  return headings.at(-1)?.[1] ?? "";
}

let changedFiles = 0;
let changedBlocks = 0;

for (const path of walkMarkdown(docsRoot)) {
  const original = readFileSync(path, "utf8");
  const endpointHeading = original.match(/^#\s+(GET|POST|PUT|PATCH|DELETE)\s+`?(\/api\/[^`\n ]+)`?/im);
  const operation = endpointHeading
    ? operations.get(normalizedKey(endpointHeading[1], endpointHeading[2]))
    : undefined;

  let blockOffset = 0;
  const normalized = original.replace(/```json\s*\n([\s\S]*?)```/g, (full, json, offset) => {
    blockOffset = offset;
    const sectionTitle = nearestSectionTitle(original, blockOffset);
    let schema;
    if (/request|istek|gövde/i.test(sectionTitle)) schema = requestSchema(operation);
    else if (/response|başarılı|\b20[012]\b/i.test(sectionTitle)) schema = responseSchema(operation, sectionTitle);

    const parsed = JSON.parse(json);
    const coerced = coerce(parsed, schema);
    const next = `\`\`\`json\n${JSON.stringify(coerced, null, 2)}\n\`\`\``;
    if (next !== full) changedBlocks += 1;
    return next;
  });

  if (normalized !== original) {
    writeFileSync(path, normalized);
    changedFiles += 1;
  }
}

console.log(`${changedFiles} dosyada ${changedBlocks} JSON örneği normalize edildi.`);
