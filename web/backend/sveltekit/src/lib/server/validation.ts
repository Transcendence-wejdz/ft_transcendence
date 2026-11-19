export const COLORS = new Set(['Black', 'Blue', 'Purple', 'Red', 'Yellow']);
export const CLASSES = new Set(['Warrior', 'Archer', 'Lancer', 'Monk', 'Pawn']);

const GAME_SETTING_FLAGS = ['showEnemyHealthBars', 'screenShake', 'showDamageNumbers', 'showFps', 'showPing'];
const MAX_KEYBINDS_LENGTH = 20_000;

export type GameSettings = Record<string, boolean | string>;

// Whitespace becomes underscores; canonical names contain 3-15 ASCII letters, digits, _ or -.
export function normalizeDisplayName(displayName: string): string | null {
    const cleanName = displayName.trim().replace(/\s+/g, '_');
    if (cleanName.length < 3 || cleanName.length > 15) return null;
    return /^[A-Za-z0-9_-]+$/.test(cleanName) ? cleanName : null;
}

// Whitelisted copy of the in-game settings object, or null when anything is malformed.
export function parseGameSettings(value: unknown): GameSettings | null {
    if (!value || typeof value !== 'object' || Array.isArray(value)) return null;
    const settings: GameSettings = {};
    for (const [key, entry] of Object.entries(value)) {
        if (GAME_SETTING_FLAGS.includes(key) && typeof entry === 'boolean') settings[key] = entry;
        else if (key === 'keybinds' && typeof entry === 'string' && entry.length <= MAX_KEYBINDS_LENGTH) settings[key] = entry;
        else return null;
    }
    return settings;
}

// True for Prisma's unique-constraint violation.
export function isUniqueViolation(error: unknown): boolean {
    return !!error && typeof error === 'object' && 'code' in error && error.code === 'P2002';
}

// Unity services player ids are short and URL-safe; anything else never reaches the database.
export function isUnityPlayerId(value: unknown): value is string {
    return typeof value === 'string' && /^[A-Za-z0-9_-]{1,64}$/.test(value);
}
