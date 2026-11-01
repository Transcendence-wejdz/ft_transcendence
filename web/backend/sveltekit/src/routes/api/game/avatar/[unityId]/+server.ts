import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db.js';
import { isUnityPlayerId } from '$lib/server/validation.js';

const AVATAR_DATA_URL = /^data:(image\/(?:png|jpeg|webp|gif));base64,([A-Za-z0-9+/]+=*)$/;

// Serves a player's site avatar as plain image bytes to signed-in players, looked up by the Unity id their friends lists know them by.
export const GET: RequestHandler = async ({ locals, params }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });
    if (!isUnityPlayerId(params.unityId)) return json({ success: false, error: 'Invalid Unity id.' }, { status: 400 });

    const user = await prisma.user.findUnique({ where: { unityPlayerId: params.unityId }, select: { avatarUrl: true } });
    const match = user?.avatarUrl?.match(AVATAR_DATA_URL);
    if (!match) return json({ success: false, error: 'No avatar.' }, { status: 404 });

    return new Response(Buffer.from(match[2], 'base64'), {
        headers: { 'Content-Type': match[1], 'Cache-Control': 'private, max-age=300', 'X-Content-Type-Options': 'nosniff' }
    });
};
