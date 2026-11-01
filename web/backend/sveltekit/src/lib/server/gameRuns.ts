export const SCORE_PER_GOLD = 1000;
export const GAME_MODES = new Set(['solo', 'coop']);
export const MAX_RUN_SECONDS = 4 * 60 * 60;
export const MAX_RUN_SCORE = 2_147_483_647;
export const MAX_RUN_KILLS = 1_000_000;
export const MAX_RUN_WAVE = 10_000;
export const CLOCK_SLACK_SECONDS = 15;
export const MAX_CHECKPOINT_WAVE_GAP = 15;
export const RUN_PROGRESS_SELECT = { startedAt: true, finishedAt: true, checkpointAt: true, checkpointWave: true, checkpointKills: true, checkpointScore: true };

const MIN_SECONDS_PER_WAVE = 2;
const MAX_KILLS_PER_SECOND = 40;
const KILL_BURST_SECONDS = 5;
const MAX_POINTS_PER_KILL = 300;
const SEGMENT_FLAT_POINTS = 15_000;
const POINTS_PER_WAVE = 100;
const SPAWNS_PER_WAVE_STEP = 5;
const SPAWNS_FOR_FULL_TEAM = 15;

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export type RunProgress = { wave: number; kills: number; score: number };
export type RunCheckpoint = { startedAt: Date; checkpointAt: Date | null; checkpointWave: number; checkpointKills: number; checkpointScore: number };

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

// Points granted by the wave starts from one wave to another, both included since a client's score can lag its wave counter.
function wavePointsBetween(from: number, to: number): number {
    return (POINTS_PER_WAVE * (to * (to + 1) - from * (from - 1))) / 2;
}

// Why a report can't follow the run's last accepted one on the server's clock, or null when it can.
export function progressError(run: RunCheckpoint, report: RunProgress, now: number): string | null {
    const seconds = (now - (run.checkpointAt ?? run.startedAt).getTime()) / 1000;
    const waves = report.wave - run.checkpointWave;
    const kills = report.kills - run.checkpointKills;
    const points = report.score - run.checkpointScore;

    if (waves < 0 || kills < 0 || points < 0) return 'Run progress went backwards.';
    if (waves > 1 + seconds / MIN_SECONDS_PER_WAVE) return 'Waves too fast for the time elapsed.';
    if (report.kills > maxSpawnedBy(report.wave)) return 'More kills than enemies spawned.';
    if (kills > (seconds + KILL_BURST_SECONDS) * MAX_KILLS_PER_SECOND) return 'Kills too fast for the time elapsed.';
    if (points > wavePointsBetween(run.checkpointWave, report.wave) + kills * MAX_POINTS_PER_KILL + SEGMENT_FLAT_POINTS) {
        return 'Score too high for the kills and waves reported.';
    }
    return null;
}
