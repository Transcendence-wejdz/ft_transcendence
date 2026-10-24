import { json } from '@sveltejs/kit';
import type { RequestHandler } from './$types';
import { prisma } from '$lib/server/db.js';
import { MAX_UPGRADE_LEVEL, UPGRADE_TYPES, readUpgrades, upgradeCost } from '$lib/server/upgrades.js';

// Buys the next level of one permanent upgrade with the account's gold; the write only lands if gold is unchanged since the read.
export const POST: RequestHandler = async ({ locals, request }) => {
    if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

    let body: unknown;
    try {
        body = await request.json();
    } catch {
        return json({ success: false, error: 'Invalid request body' }, { status: 400 });
    }

    const type = body && typeof body === 'object' ? (body as Record<string, unknown>).type : undefined;
    if (typeof type !== 'string' || !UPGRADE_TYPES.has(type)) {
        return json({ success: false, error: 'Unknown upgrade.' }, { status: 400 });
    }

    const userId = locals.user.id;
    try {
        const user = await prisma.user.findUnique({ where: { id: userId }, select: { gold: true, upgrades: true } });
        if (!user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

        const upgrades = readUpgrades(user.upgrades);
        const level = upgrades[type] ?? 0;
        if (level >= MAX_UPGRADE_LEVEL) return json({ success: false, error: 'Upgrade already maxed.' }, { status: 409 });

        const cost = upgradeCost(level);
        if (user.gold < cost) return json({ success: false, error: 'Not enough gold.' }, { status: 402 });

        upgrades[type] = level + 1;
        const updated = await prisma.user.updateMany({
            where: { id: userId, gold: user.gold },
            data: { gold: { decrement: cost }, upgrades }
        });
        if (updated.count !== 1) return json({ success: false, error: 'Gold changed, try again.' }, { status: 409 });

        return json({ success: true, type, level: level + 1, gold: user.gold - cost });
    } catch (error) {
        console.error('Failed to buy upgrade:', error);
        return json({ success: false, error: 'Could not buy the upgrade.' }, { status: 500 });
    }
};
