import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db.js';
import { CLOCK_SLACK_SECONDS, MAX_CHECKPOINT_WAVE_GAP, MAX_RUN_KILLS, MAX_RUN_SCORE, MAX_RUN_SECONDS, MAX_RUN_WAVE, RUN_PROGRESS_SELECT, SCORE_PER_GOLD, isIntIn, isRunToken, progressError } from '$lib/server/gameRuns.js';

type RunReport = { runToken: string; runId: number; score: number; wave: number; seconds: number; kills: number; victory: boolean };

// Shape and range checks only; coherence with the server's own clock is checked against the stored start.
function parseReport(body: unknown): RunReport | null {
    if (!body || typeof body !== 'object' || Array.isArray(body)) return null;
    const run = body as Record<string, unknown>;
    if (!isRunToken(run.runToken) || !isIntIn(run.runId, 1, 2_147_483_647)) return null;
    if (!isIntIn(run.seconds, 1, MAX_RUN_SECONDS) || !isIntIn(run.wave, 1, MAX_RUN_WAVE)) return null;
    if (!isIntIn(run.kills, 0, MAX_RUN_KILLS) || !isIntIn(run.score, 0, MAX_RUN_SCORE)) return null;
    if (typeof run.victory !== 'boolean') return null;
    return { runToken: run.runToken, runId: run.runId, score: run.score, wave: run.wave, seconds: run.seconds, kills: run.kills, victory: run.victory };
}

// Closes a run opened by /api/game/run/start and pays its gold once (SCORE_PER_GOLD must match GoldWallet.ScorePerGold in Unity).
export const POST: RequestHandler = async ({ locals, request }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    let body: unknown;
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }

    const report = parseReport(body);
    if (!report) return json({ success: false, error: 'Invalid run report.' }, { status: 400 });

    const userId = locals.user.id;
    const run = await prisma.gameRun.findFirst({ where: { id: report.runToken, userId }, select: RUN_PROGRESS_SELECT });
    if (!run) return json({ success: false, error: 'Unknown run.' }, { status: 404 });
    if (run.finishedAt) return json({ success: false, error: 'Run already rewarded.' }, { status: 409 });

    const now = Date.now();
    const elapsed = (now - run.startedAt.getTime()) / 1000;
    if (elapsed > MAX_RUN_SECONDS) return json({ success: false, error: 'Run expired.' }, { status: 410 });
    if (report.seconds > elapsed + CLOCK_SLACK_SECONDS) {
        return json({ success: false, error: 'Run longer than the time since it started.' }, { status: 400 });
    }
    if (report.wave - run.checkpointWave > MAX_CHECKPOINT_WAVE_GAP) {
        return json({ success: false, error: 'Too many waves without a checkpoint.' }, { status: 400 });
    }
    const progress = progressError(run, report, now);
    if (progress) return json({ success: false, error: progress }, { status: 400 });

    const earned = Math.floor(report.score / SCORE_PER_GOLD);
    try {
        const gold = await prisma.$transaction(async (tx) => {
            const closed = await tx.gameRun.updateMany({
                where: { id: report.runToken, userId, finishedAt: null },
                data: {
                    finishedAt: new Date(),
                    runId: report.runId,
                    score: report.score,
                    wave: report.wave,
                    seconds: report.seconds,
                    kills: report.kills,
                    victory: report.victory,
                    gold: earned
                }
            });
            if (closed.count !== 1) return null;
            const user = await tx.user.update({ where: { id: userId }, data: { gold: { increment: earned } }, select: { gold: true } });
            return user.gold;
        });
        if (gold === null) return json({ success: false, error: 'Run already rewarded.' }, { status: 409 });
        return json({ success: true, earned, gold });
    } catch (error) {
        console.error('Failed to grant run gold:', error);
        return json({ success: false, error: 'Could not save your gold.' }, { status: 500 });
    }
};
