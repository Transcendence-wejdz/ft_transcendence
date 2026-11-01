import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db.js';
import { isUniqueViolation, isUnityPlayerId } from '$lib/server/validation.js';

// Links this account to its Unity player, so the game's friends lists can find its avatar; an id already linked to another account is refused.
export const POST: RequestHandler = async ({ locals, request }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    let body: unknown;
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }

    const input = body && typeof body === 'object' && !Array.isArray(body) ? (body as Record<string, unknown>) : {};
    if (!isUnityPlayerId(input.unityId)) return json({ success: false, error: 'Invalid Unity id.' }, { status: 400 });

    try {
        await prisma.user.update({ where: { id: locals.user.id }, data: { unityPlayerId: input.unityId } });
        return json({ success: true });
    } catch (error) {
        if (isUniqueViolation(error)) return json({ success: false, error: 'Unity id already linked.' }, { status: 409 });
        console.error('Failed to link Unity id:', error);
        return json({ success: false, error: 'Could not link the Unity id.' }, { status: 500 });
    }
};
