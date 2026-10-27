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

// Codes of the auth, account and settings routes.
export const ACCOUNT_ERROR_CODES = [
    'invalid_credentials', // 401: unknown email or name, or wrong password, never saying which
    'invalid_email',       // 400
    'invalid_name',        // 400: breaks the display name rules
    'invalid_password',    // 400: not 8 to 128 characters
    'invalid_avatar',      // 400: not a PNG, JPG, WebP or GIF under 500 KB
    'email_taken',         // 409
    'name_taken',          // 409
    'wrong_password',      // 403: the current password, asked before changing email or password
    'invalid_totp_code',   // 401: wrong or expired 2FA code
    'oauth_denied',        // sent as /login?error=oauth_denied: the player refused on the 42 page
    'oauth_failed'         // sent as /login?error=oauth_failed: 42 did not answer, or the link expired
] as const;

// Every error code the API can send.
export const API_ERROR_CODES = [
    ...GENERIC_ERROR_CODES,
    ...SOCIAL_ERROR_CODES,
    ...CHAT_ERROR_CODES,
    ...ACCOUNT_ERROR_CODES
] as const;

export type ApiErrorCode = (typeof API_ERROR_CODES)[number];

// Body of every failed response: the HTTP status says how bad it is, the code says what happened.
export type ApiFailure = {
    success: false;
    code: ApiErrorCode;
};

// Body of every successful response: success, then the route's data at the top level.
export type ApiSuccess<T extends object = object> = { success: true } & T;

// What a route sends back: check success before reading the data.
export type ApiResponse<T extends object = object> = ApiSuccess<T> | ApiFailure;

// ---- Users ----

// TODO:    Rules of login differ depending of the route, we need to choose between:
// TODO:    - /api/register that allow 3 to 20 char (letters, numbers, '_', '-'))
// TODO:    - settings (in validation.ts) that allow 2 to 24 char and autorize space

// ---- Friends ----

// ---- Chat ----

// ---- Notifications ----

// ---- Game ----

// ---- Achievements ----

// ---- Profile ----

// ---- Leaderboard ----

// ---- Settings ----

// ---- Auth ----
