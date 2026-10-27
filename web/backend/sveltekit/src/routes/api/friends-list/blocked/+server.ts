import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ locals}) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    const blocks = await prisma.block.findMany({
      where: { blockerId: locals.user.id },
      select: {
        blocked: {
          select: { id: true, displayName: true }
        }
      }
    });

    return json({
      blockedPlayers: blocks.map((block) => block.blocked)
    });
}