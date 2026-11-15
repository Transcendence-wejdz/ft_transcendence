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
    'wrong_password'       // 403: the current password, asked before changing email or password
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

// One page of a long list (messages, match history). Send nextCursor back for the next page; null means the end.
export type Page<T> = {
    items: T[];
    nextCursor: string | null;
};

// ---- Users ----

// Playable classes. Must match the game and CLASSES in $lib/server/validation.ts.
export const PLAYER_CLASSES = ['Warrior', 'Archer', 'Lancer', 'Monk', 'Pawn'] as const;

export type PlayerClass = (typeof PLAYER_CLASSES)[number];

// Player colors. Must match the game and COLORS in $lib/server/validation.ts.
export const PLAYER_COLORS = ['Black', 'Blue', 'Purple', 'Red', 'Yellow'] as const;

export type PlayerColor = (typeof PLAYER_COLORS)[number];

// Just enough to show any player: a friends list row, a chat header, a leaderboard line.
// Seen by other players: never add private fields (email, gold, settings) here.
// Avatar storage still open, see #96.
export type UserSummary = {
    id: Uuid;
    displayName: string;
    color: PlayerColor;
    avatarUrl: string | null; // a URL to load the picture from, never the picture itself
};

// The dot on a player's avatar. in_game: has the game page open. Worked out by the WebSocket server.
export type OnlineStatus = 'online' | 'in_game' | 'offline';

// The logged-in player, from /api/me: their summary plus private fields only they may see.
export type CurrentUser = UserSummary & {
    email: string;
    class: PlayerClass;
    gold: number;
};

// ---- Friends ----

// The link between the logged-in player and another one, seen from the logged-in side.
// The database only stores PENDING or ACCEPTED: the API works out sent or received from requesterId.
// blocked: either player blocked the other, never saying which (like the blocked error code).
export type RelationshipStatus = 'none' | 'friends' | 'request_sent' | 'request_received' | 'blocked';

// A row of the friends list. relationshipId (the Friendship row) is what accept and remove send back.
export type Friend = UserSummary & {
    relationshipId: Uuid;
    onlineStatus: OnlineStatus; // value at page load; the presence store keeps it live after that
    lastSeenAt: IsoDate | null;
};

// A pending friend request: incoming (accept or decline) or outgoing (cancel).
// No onlineStatus: presence is only shared between accepted friends.
export type FriendRequest = UserSummary & {
    relationshipId: Uuid;
    direction: 'incoming' | 'outgoing';
    sentAt: IsoDate;
};

// A player I blocked, in the list on /friends. Unblocking sends id, since the block route takes a player id.
// Only the blocker gets this list: a blocked player is never told who blocked them.
export type BlockedPlayer = UserSummary & {
    blockedAt: IsoDate;
};

// Response of GET /api/friends-list: everything the three /friends tabs need, loaded once.
// requests holds both directions: filter on direction to split Received and Sent.
export type FriendsList = {
    friends: Friend[];
    requests: FriendRequest[];
    blocked: BlockedPlayer[];
};

// Body of POST /api/friends-list: send a friend request.
// A name, not an id: it is what the player types in "Find a player"; the API looks up the id.
export type FriendRequestBody = {
    displayName: string;
};

// Body of PATCH /api/friends-list/relationships/[relationshipId]: accept an incoming request.
// Decline, cancel and remove send no body: they are a DELETE on the same route.
export type RelationshipActionBody = {
    action: 'accept';
};

// ---- Chat ----

// Longest message allowed, checked on both sides: the front stops typing, the API answers message_too_long.
export const MESSAGE_MAX_LENGTH = 1000;

// One chat message. Mine when senderId is the logged-in player; receiverId tells my other tabs which conversation it goes in.
// Match invites as messages still open, see #97.
export type ChatMessage = {
    id: Uuid;
    senderId: Uuid;
    receiverId: Uuid;
    text: string;
    sentAt: IsoDate;
};

// A row of /chat: the other player, their latest message (null if none yet) and the unread badge (0: hidden).
// unreadCount still open, see #97.
export type Conversation = {
    peer: UserSummary;
    lastMessage: ChatMessage | null;
    unreadCount: number;
};

// Body of POST /api/players/[playerId]/messages: only what the player typed.
export type SendMessageBody = {
    text: string;
};

// Response of POST /api/players/[playerId]/messages: the message as saved, with its id and sentAt.
// The sender's tab shows this copy and skips the same id when the WebSocket echo arrives.
export type SentMessage = {
    message: ChatMessage;
};

// Response of GET /api/players/[playerId]/messages?before=<cursor>: newest message first.
// nextCursor null: the start of the conversation is reached, nothing older to load.
export type MessagesPage = Page<ChatMessage>;

// Response of GET /api/conversations: every row of /chat, loaded once.
// Latest activity first; friends with no message yet (lastMessage null) come last.
// Not a Page: there is at most one row per friend, so the list stays short.
export type ConversationsList = {
    conversations: Conversation[];
};

// ---- Game ----

// ---- Achievements ----

// ---- Profile ----

// Modes the database accepts today (GAME_MODES in $lib/server/gameRuns.ts).
export type GameMode = 'solo' | 'coop';

// A player's totals in one mode, from their finished runs.
export type ModeStats = {
    bestScore: number;
    bestWave: number;
    kills: number;
    games: number;
};

// Place in a mode's leaderboard (best score per player). null when the player has no finished run in that mode.
export type ModeRank = {
    rank: number;
    players: number;
};

// One finished run in the profile's history. Only the player's own line: teammates are never listed.
export type ProfileRun = {
    id: Uuid;
    mode: GameMode;
    score: number;
    wave: number;
    kills: number;
    seconds: number;
    victory: boolean;
    finishedAt: IsoDate;
};

// The page /profile/[name]. No gold here: shown nowhere but the game (decided in #102).
export type PublicProfile = UserSummary & {
    class: PlayerClass;
    createdAt: IsoDate | null;
    online: boolean;
    lastSeenAt: IsoDate | null;
    relationship: RelationshipStatus | 'self'; // 'blocked' is never sent: blocking is dropped
    relationshipId: Uuid | null;
    stats: Record<GameMode, ModeStats>;
    ranks: Record<GameMode, ModeRank | null>;
    upgrades: Record<string, number>; // upgrade type → level 0 to 10
    lastRuns: ProfileRun[];
};

// ---- Leaderboard ----

// ---- Settings ----

// ---- Auth ----
