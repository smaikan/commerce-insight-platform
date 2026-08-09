import http from "k6/http";
import { check, sleep } from "k6";
import { Counter, Rate, Trend } from "k6/metrics";

const apiBaseUrl = (__ENV.API_BASE_URL || "http://localhost:3300").replace(/\/$/, "");
const adminEmail = __ENV.API_ADMIN_EMAIL;
const adminPassword = __ENV.API_ADMIN_PASSWORD;
const p95LimitMs = Number(__ENV.ORDER_P95_MS || "1000");

const orderDuration = new Trend("orders_list_duration", true);
const orderFailures = new Rate("orders_list_failures");
const orderResponses = new Counter("orders_list_responses");

export const options = {
  scenarios: {
    orders_list: {
      executor: "ramping-vus",
      startVUs: 1,
      stages: [
        { duration: "15s", target: 5 },
        { duration: "30s", target: 5 },
        { duration: "15s", target: 0 },
      ],
      gracefulRampDown: "10s",
    },
  },
  thresholds: {
    orders_list_duration: [`p(95)<${p95LimitMs}`],
    orders_list_failures: ["rate<0.01"],
  },
};

export function setup() {
  if (!adminEmail || !adminPassword) {
    throw new Error("API_ADMIN_EMAIL and API_ADMIN_PASSWORD must be supplied as environment variables.");
  }

  const response = http.post(
    `${apiBaseUrl}/api/auth/login`,
    JSON.stringify({ email: adminEmail, password: adminPassword, deviceName: "orders-k6-benchmark" }),
    { headers: { "Content-Type": "application/json", Accept: "application/json" } },
  );
  const ok = check(response, { "benchmark login succeeds": (result) => result.status === 200 });
  if (!ok) throw new Error("Benchmark login failed. Verify the isolated admin test account and API_BASE_URL.");

  const token = response.json("tokens.accessToken");
  if (!token || typeof token !== "string") throw new Error("Login response did not contain an access token.");
  return { token };
}

export default function ordersListScenario({ token }) {
  const response = http.get(`${apiBaseUrl}/api/orders?PageNumber=1&PageSize=20`, {
    headers: { Authorization: `Bearer ${token}`, Accept: "application/json" },
    tags: { endpoint: "GET /api/orders" },
  });

  orderDuration.add(response.timings.duration);
  orderResponses.add(1);
  orderFailures.add(response.status !== 200);
  check(response, {
    "orders list returns 200": (result) => result.status === 200,
    "orders list returns a paged payload": (result) => Array.isArray(result.json("items")),
  });
  sleep(1);
}
