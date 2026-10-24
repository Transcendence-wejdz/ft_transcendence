export const SCORE_PER_GOLD = 1000;
export const GAME_MODES = new Set(['solo', 'coop']);
export const MAX_RUN_SECONDS = 4 * 60 * 60;
export const MAX_POINTS_PER_SECOND = 500;
export const MAX_FLAT_POINTS = 30_000;
export const CLOCK_SLACK_SECONDS = 15;

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

// Integer in [min, max], so floats, strings and NaN are all rejected.
export function isIntIn(value: unknown, min: number, max: number): value is number {
    return typeof value === 'number' && Number.isInteger(value) && value >= min && value <= max;
}

export function isRunToken(value: unknown): value is string {
    return typeof value === 'string' && UUID.test(value);
}

// Highest score a run can legitimately reach in that many seconds (kills and waves scale with time, boss and victory are flat).
export function maxScoreFor(seconds: number): number {
    return MAX_FLAT_POINTS + seconds * MAX_POINTS_PER_SECOND;
}
