export const SCORE_PER_GOLD = 1000;
export const GAME_MODES = new Set(['solo', 'coop']);
export const MAX_RUN_SECONDS = 4 * 60 * 60;
export const MAX_RUN_SCORE = 2_147_483_647;
export const MAX_RUN_KILLS = 1_000_000;
export const MAX_RUN_WAVE = 10_000;
export const CLOCK_SLACK_SECONDS = 15;
export const MAX_CHECKPOINT_WAVE_GAP = 15;
export const RUN_PROGRESS_SELECT = { startedAt: true, finishedAt: true, checkpointWave: true, checkpointKills: true, checkpointScore: true };

const MAX_KILLS_PER_SECOND = 20;
const KILL_BURST_SECONDS = 20;
const SECONDS_BETWEEN_WAVES = 20;
const MAX_POINTS_PER_KILL = 300;
const FLAT_POINTS = 15_000;
const POINTS_PER_WAVE = 100;
const SPAWNS_PER_WAVE_STEP = 5;
const SPAWNS_FOR_FULL_TEAM = 15;

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export type RunProgress = { wave: number; kills: number; score: number };
export type RunCheckpoint = { startedAt: Date; checkpointWave: number; checkpointKills: number; checkpointScore: number };

// Integer in [min, max], so floats, strings and NaN are all rejected.
export function isIntIn(value: unknown, min: number, max: number): value is number {
    return typeof value === 'number' && Number.isInteger(value) && value >= min && value <= max;
}

export function isRunToken(value: unknown): value is string {
    return typeof value === 'string' && UUID.test(value);
}

// Most enemies a full team can have met by that wave (spawn tuning must match ModeManager.prefab).
function maxSpawnedBy(wave: number): number {
    return (SPAWNS_PER_WAVE_STEP * wave * (wave + 1)) / 2 + SPAWNS_FOR_FULL_TEAM * wave;
}

// Points granted by every wave start up to that wave.
function wavePointsUpTo(wave: number): number {
    return (POINTS_PER_WAVE * wave * (wave + 1)) / 2;
}

// Earliest a wave can start: a wave only ends early once its enemies are all dead, or at worst when its timer runs out.
function minSecondsToReach(wave: number): number {
    let seconds = -CLOCK_SLACK_SECONDS;

    for (let n = 1; n < wave; n++) seconds += Math.min(SECONDS_BETWEEN_WAVES, (SPAWNS_PER_WAVE_STEP * n) / MAX_KILLS_PER_SECOND);
    return seconds;
}

// Why a report can't be true on the server's clock, or null when it can; limits cover the whole run, so sending more reports earns nothing extra.
export function progressError(run: RunCheckpoint, report: RunProgress, now: number): string | null {
    const seconds = (now - run.startedAt.getTime()) / 1000;

    if (report.wave < run.checkpointWave || report.kills < run.checkpointKills || report.score < run.checkpointScore) {
        return 'Run progress went backwards.';
    }
    if (seconds < minSecondsToReach(report.wave)) return 'Waves too fast for the time elapsed.';
    if (report.kills > maxSpawnedBy(report.wave)) return 'More kills than enemies spawned.';
    if (report.kills > (seconds + KILL_BURST_SECONDS) * MAX_KILLS_PER_SECOND) return 'Kills too fast for the time elapsed.';
    if (report.score > FLAT_POINTS + wavePointsUpTo(report.wave) + report.kills * MAX_POINTS_PER_KILL) {
        return 'Score too high for the kills and waves reported.';
    }
    return null;
}
