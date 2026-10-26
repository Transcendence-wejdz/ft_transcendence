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

// Codes of the friends and block routes.
export const SOCIAL_ERROR_CODES = [
    'player_not_found',       // 404: no player with this name or id
    'cannot_add_yourself',    // 400
    'cannot_block_yourself',  // 400
    'already_friends',        // 409
    'request_already_sent',   // 409: you already have a pending request to this player
    'relationship_not_found', // 404: no such request or friendship, or not one of yours
    'not_friends',            // 403: the action needs a friendship, e.g. sending a message
    'blocked'                 // 403: one player blocked the other, never saying which
] as const;

// Codes of the chat routes.
export const CHAT_ERROR_CODES = [
    'message_empty',   // 400: nothing left once spaces are trimmed
    'message_too_long' // 400: longer than MESSAGE_MAX_LENGTH
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
