#!/usr/bin/env node

import fs from "node:fs";
import path from "node:path";

const args = process.argv.slice(2);
const jsonOutput = args.includes("--json");
const specArg =
  args.find((arg) => !arg.startsWith("--")) ??
  "../docs/api/api-project-docs/openapi-controller-contract.json";
const specPath = path.resolve(specArg);
const findings = [];

const add = (severity, rule, message) => findings.push({ severity, rule, message });

if (!fs.existsSync(specPath)) {
  add("critical", "spec-not-found", `OpenAPI document not found: ${specPath}`);
  finish(null);
  process.exit(process.exitCode ?? 0);
}

let spec;
try {
  const source = fs.readFileSync(specPath, "utf8").replace(/^\uFEFF/, "");
  spec = JSON.parse(source);
} catch (error) {
  add("critical", "invalid-json", `OpenAPI document is not valid JSON: ${error.message}`);
  finish(null);
  process.exit(process.exitCode ?? 0);
}

if (!/^3\./.test(spec.openapi ?? "")) {
  add("high", "openapi-version", `Expected OpenAPI 3.x but found ${spec.openapi ?? "missing"}.`);
}

const paths = spec.paths ?? {};
const schemas = spec.components?.schemas ?? {};
const securitySchemes = spec.components?.securitySchemes ?? {};

if (!securitySchemes.Bearer) {
  add("high", "bearer-scheme", "Bearer security scheme is missing.");
}

const requiredAuth = {
  "/api/auth/register": ["post", "201"],
  "/api/auth/login": ["post", "200"],
  "/api/auth/refresh-token": ["post", "200"],
  "/api/auth/logout": ["post", "204"],
  "/api/auth/forgot-password": ["post", "202"],
  "/api/auth/reset-password": ["post", "204"],
};

const missingErrorDocs = [];
for (const [route, [method, successStatus]] of Object.entries(requiredAuth)) {
  const operation = paths[route]?.[method];
  if (!operation) {
    add("critical", "auth-operation", `Missing ${method.toUpperCase()} ${route}.`);
    continue;
  }
  if (!operation.responses?.[successStatus]) {
    add("high", "success-response", `${method.toUpperCase()} ${route} is missing documented ${successStatus}.`);
  }
  const errorStatuses = ["400", "401", "403", "409", "429", "500"];
  if (!errorStatuses.some((status) => operation.responses?.[status])) {
    missingErrorDocs.push(`${method.toUpperCase()} ${route}`);
  }
}

if (missingErrorDocs.length) {
  add(
    "medium",
    "auth-error-contracts",
    `Auth operations without documented error responses: ${missingErrorDocs.join(", ")}.`,
  );
}

const requiredSchemas = [
  "LoginRequest",
  "RefreshTokenRequest",
  "LogoutRequest",
  "AuthResultDto",
  "AuthTokensDto",
  "UserDto",
];
for (const schema of requiredSchemas) {
  if (!schemas[schema]) add("critical", "auth-schema", `Missing schema: ${schema}.`);
}

const hasProblemSchema = Object.keys(schemas).some((name) => /ProblemDetails|ValidationProblem/i.test(name));
if (!hasProblemSchema) {
  add("medium", "problem-details-schema", "No ProblemDetails/ValidationProblemDetails schema is present.");
}

const hasGlobalSecurity = Array.isArray(spec.security) && spec.security.length > 0;
if (hasGlobalSecurity) {
  const authWithoutOverride = Object.keys(requiredAuth).filter((route) => {
    const operation = paths[route]?.post;
    return operation && !Array.isArray(operation.security);
  });
  if (authWithoutOverride.length) {
    add(
      "high",
      "public-auth-security",
      `Global security applies to runtime-public auth operations without security: [] overrides: ${authWithoutOverride.join(", ")}.`,
    );
  }
}

const numericEnums = Object.values(schemas).filter(
  (schema) => schema?.type === "integer" && Array.isArray(schema.enum),
).length;
const stringEnums = Object.values(schemas).filter(
  (schema) => schema?.type === "string" && Array.isArray(schema.enum),
).length;

finish({
  openapi: spec.openapi ?? null,
  pathCount: Object.keys(paths).length,
  schemaCount: Object.keys(schemas).length,
  numericEnums,
  stringEnums,
  securitySchemes: Object.keys(securitySchemes),
});

function finish(summary) {
  const order = { critical: 0, high: 1, medium: 2, low: 3, info: 4 };
  findings.sort((a, b) => order[a.severity] - order[b.severity] || a.rule.localeCompare(b.rule));

  if (jsonOutput) {
    console.log(JSON.stringify({ specPath, summary, findings }, null, 2));
  } else {
    if (summary) {
      console.log(
        `OpenAPI ${summary.openapi}: ${summary.pathCount} paths, ${summary.schemaCount} schemas, ` +
          `${summary.numericEnums} numeric enums, ${summary.stringEnums} string enums.`,
      );
    }
    if (!findings.length) {
      console.log("No contract leads found.");
    } else {
      for (const finding of findings) {
        console.log(`[${finding.severity.toUpperCase()}] ${finding.rule}`);
        console.log(`  ${finding.message}`);
      }
      console.log(`\n${findings.length} contract lead(s). Confirm against controllers and runtime behavior.`);
    }
  }

  process.exitCode = findings.some((finding) => finding.severity === "critical") ? 2 : 0;
}
