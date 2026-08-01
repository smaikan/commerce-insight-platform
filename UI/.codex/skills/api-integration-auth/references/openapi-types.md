# OpenAPI Type Generation

## Tooling

Use `openapi-typescript` as a dev dependency. Preserve the repository package manager and lockfile.

Recommended scripts:

```json
{
  "scripts": {
    "api:types": "openapi-typescript ../docs/api/api-project-docs/openapi-controller-contract.json -o src/generated/api.ts",
    "api:types:check": "openapi-typescript ../docs/api/api-project-docs/openapi-controller-contract.json -o src/generated/api.ts --check"
  }
}
```

Run generation intentionally. The CLI overwrites the output file. Run `api:types:check` in CI.

Enable strict TypeScript and preferably `noUncheckedIndexedAccess`.

## Usage

Prefer generated aliases:

```ts
import type { components, paths } from "@/generated/api";

export type LoginRequest = components["schemas"]["LoginRequest"];
export type AuthResult = components["schemas"]["AuthResultDto"];
export type LoginOperation = paths["/api/auth/login"]["post"];
```

Do not edit `src/generated/api.ts`.

## Boundaries

- Generated types describe the wire contract; they do not parse runtime data.
- Add runtime validation for environment/configuration, user input, and security-sensitive or unreliable external responses when needed.
- Keep form schemas beside the feature and map them to generated request types.
- Do not duplicate every generated DTO in a handwritten `types.ts`.
- Add readable aliases for deeply nested path types.
- Keep dates as wire strings at the HTTP boundary; parse only in owned mappers.
- Preserve nullable/optional differences.
- Do not convert numeric enums to strings in requests.

## Contract gaps

- If an operation or error schema is absent, report it instead of inventing generated types.
- Keep a small handwritten `ApiProblem` type only while ProblemDetails is missing from OpenAPI.
- Verify status codes from controller/endpoint contracts; generic “create means 201” assumptions are unsafe.
- Regenerate after backend contract changes and review the diff before accepting it.
