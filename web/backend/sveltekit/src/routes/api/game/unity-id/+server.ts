import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db.js';
import { unityPlayerIdFromToken } from '$lib/server/unityToken.js';
import { isUnityPlayerId } from '$lib/server/validation.js';

const MAX_TOKEN_LENGTH = 8192;

// Links this account to the Unity player it proves to be with an access token, so the game's friends lists can find its avatar; a proven link replaces any older one on that id.
export const POST: RequestHandler = async ({ locals, request }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    let body: unknown;
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }

    const input = body && typeof body === 'object' && !Array.isArray(body) ? (body as Record<string, unknown>) : {};
    if (typeof input.accessToken !== 'string' || input.accessToken.length > MAX_TOKEN_LENGTH) {
        return json({ success: false, error: 'Invalid Unity token.' }, { status: 400 });
    }

    const userId = locals.user.id;
    try {
        const unityPlayerId = await unityPlayerIdFromToken(input.accessToken);
        if (!isUnityPlayerId(unityPlayerId)) return json({ success: false, error: 'Invalid Unity token.' }, { status: 400 });

        const nameResponse = await fetch(`https://social.services.api.unity.com/v1/names/${encodeURIComponent(unityPlayerId)}`, {
            headers: { Authorization: `Bearer ${input.accessToken}` }
        });
        if (!nameResponse.ok) {
            console.error(`Unity Player Names returned HTTP ${nameResponse.status}.`);
            return json({ success: false, error: 'Could not retrieve the Unity player name.' }, { status: 502 });
        }

        const nameResult: unknown = await nameResponse.json();
        const unityPlayerName = nameResult && typeof nameResult === 'object' && 'name' in nameResult
            && typeof nameResult.name === 'string' && nameResult.name.trim()
            ? nameResult.name.trim()
            : null;
        if (!unityPlayerName) {
            return json({ success: false, error: 'Unity returned an invalid player name.' }, { status: 502 });
        }

        await prisma.$transaction([
            prisma.user.updateMany({ where: { unityPlayerId, NOT: { id: userId } }, data: { unityPlayerId: null, unityPlayerName: null } }),
            prisma.user.update({ where: { id: userId }, data: { unityPlayerId, unityPlayerName } })
        ]);
        return json({ success: true });
    } catch (error) {
        console.error('Failed to link Unity id:', error);
        return json({ success: false, error: 'Could not link the Unity id.' }, { status: 500 });
    }
};
