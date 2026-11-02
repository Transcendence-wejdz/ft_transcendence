export const COLORS = new Set(['Black', 'Blue', 'Purple', 'Red', 'Yellow']);
export const CLASSES = new Set(['Warrior', 'Archer', 'Lancer', 'Monk', 'Pawn']);

const GAME_SETTING_FLAGS = ['showEnemyHealthBars', 'screenShake', 'showDamageNumbers', 'showFps', 'showPing'];
const MAX_KEYBINDS_LENGTH = 20_000;

export type GameSettings = Record<string, boolean | string>;

// Error message for an invalid player name, or null when it's valid.
export function displayNameError(displayName: string): string | null {
    if (displayName.length < 2 || displayName.length > 24) return 'Name must be between 2 and 24 characters.';
    if (!/^[\p{L}\p{N}_ -]+$/u.test(displayName)) return 'Name can use letters, numbers, spaces, _ and -.';
    return null;
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
