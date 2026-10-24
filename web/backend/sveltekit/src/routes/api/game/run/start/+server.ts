import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db.js';
import { isUniqueViolation } from '$lib/server/validation.js';
import { GAME_MODES, isRunToken } from '$lib/server/gameRuns.js';

// Opens a run under a client-made token and timestamps it server-side; any unfinished run of this account is abandoned, so only one can ever pay out.
export const POST: RequestHandler = async ({ locals, request }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    let body: unknown;
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }

    const input = body && typeof body === 'object' && !Array.isArray(body) ? (body as Record<string, unknown>) : {};
    if (!isRunToken(input.runToken) || typeof input.mode !== 'string' || !GAME_MODES.has(input.mode)) {
        return json({ success: false, error: 'Invalid run start.' }, { status: 400 });
    }

    const userId = locals.user.id;
    try {
        await prisma.$transaction([
            prisma.gameRun.deleteMany({ where: { userId, finishedAt: null } }),
            prisma.gameRun.create({ data: { id: input.runToken, userId, mode: input.mode } })
        ]);
        return json({ success: true });
    } catch (error) {
        if (isUniqueViolation(error)) return json({ success: false, error: 'Run already started.' }, { status: 409 });
        console.error('Failed to start game run:', error);
        return json({ success: false, error: 'Could not start the run.' }, { status: 500 });
    }
};
