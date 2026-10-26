// Types shared by pages, the API layer and API routes: the front/back contract.
// - Fields are camelCase, like the Prisma models.
// - Dates are ISO strings (IsoDate), since JSON has no Date type.
// - Must stay browser-safe: never import from $lib/server here.
// - WebSocket messages belong in $lib/realtime/protocol.ts, not here.

// ---- Base ----

// Postgres uuid, the id of every database row.
export type Uuid = string;

// A date sent as JSON text, e.g. "2026-10-26T20:15:00.000Z". Read it with new Date(value).
export type IsoDate = string;

// ---- API responses ----

// Codes any route can return. The front translates each one with the i18n key error_<code>.
export const GENERIC_ERROR_CODES = [
    'not_authenticated', // 401: no session, or it expired
    'invalid_json',      // 400: the body is not JSON
    'invalid_body',      // 400: valid JSON, but fields are missing or have the wrong type
    'rate_limited',      // 429: too many requests
    'unknown'            // 500, or a code this front does not know yet
] as const;

// ---- Users ----

// ---- Friends ----

// ---- Chat ----

// ---- Notifications ----

// ---- Game ----

// ---- Achievements ----

// ---- Profile ----

// ---- Leaderboard ----

// ---- Settings ----

// ---- Auth ----
