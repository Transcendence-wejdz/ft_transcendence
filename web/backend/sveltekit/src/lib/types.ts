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
