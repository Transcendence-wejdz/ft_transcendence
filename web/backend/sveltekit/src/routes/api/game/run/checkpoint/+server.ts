import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db.js';
import { MAX_RUN_KILLS, MAX_RUN_SCORE, MAX_RUN_SECONDS, MAX_RUN_WAVE, RUN_PROGRESS_SELECT, isIntIn, isRunToken, progressError } from '$lib/server/gameRuns.js';

type RunReport = { runToken: string; wave: number; kills: number; score: number };

function parseCheckpoint(body: unknown): RunReport | null {
    if (!body || typeof body !== 'object' || Array.isArray(body)) return null;
    const run = body as Record<string, unknown>;
    if (!isRunToken(run.runToken) || !isIntIn(run.wave, 1, MAX_RUN_WAVE)) return null;
    if (!isIntIn(run.kills, 0, MAX_RUN_KILLS) || !isIntIn(run.score, 0, MAX_RUN_SCORE)) return null;
    return { runToken: run.runToken, wave: run.wave, kills: run.kills, score: run.score };
}

// Timestamps the run's progress mid-game; a refused checkpoint changes nothing, so the reward is judged from the last accepted one.
export const POST: RequestHandler = async ({ locals, request }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    let body: unknown;
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }

    const report = parseCheckpoint(body);
    if (!report) return json({ success: false, error: 'Invalid checkpoint.' }, { status: 400 });

    const userId = locals.user.id;
    const run = await prisma.gameRun.findFirst({ where: { id: report.runToken, userId }, select: RUN_PROGRESS_SELECT });
    if (!run) return json({ success: false, error: 'Unknown run.' }, { status: 404 });
    if (run.finishedAt) return json({ success: false, error: 'Run already rewarded.' }, { status: 409 });

    const now = Date.now();
    if ((now - run.startedAt.getTime()) / 1000 > MAX_RUN_SECONDS) return json({ success: false, error: 'Run expired.' }, { status: 410 });
    const error = progressError(run, report, now);
    if (error) return json({ success: false, error }, { status: 400 });

    try {
        const saved = await prisma.gameRun.updateMany({
            where: { id: report.runToken, userId, finishedAt: null, checkpointWave: { lt: report.wave } },
            data: { checkpointWave: report.wave, checkpointKills: report.kills, checkpointScore: report.score }
        });
        if (saved.count !== 1) return json({ success: false, error: 'Checkpoint out of date.' }, { status: 409 });
        return json({ success: true });
    } catch (err) {
        console.error('Failed to save run checkpoint:', err);
        return json({ success: false, error: 'Could not save the checkpoint.' }, { status: 500 });
    }
};
