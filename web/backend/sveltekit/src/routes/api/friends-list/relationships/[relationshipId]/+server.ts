import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import type { RequestHandler } from './$types';

export const PATCH: RequestHandler = async ({ locals, params, request }) => {
	if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });
	let body: unknown;
	try {
		body = await request.json();
	} catch {
		return json({ success: false, error: 'Invalid JSON' }, { status: 400 });
	}
	if (!body || typeof body !== 'object' || !('action' in body) || body.action !== 'accept') {
		return json({ success: false, error: 'Unsupported action' }, { status: 400 });
	}

	const outcome = await prisma.$transaction(async (tx) => {
		const friendship = await tx.friendship.findFirst({
			where: {
				id: params.relationshipId,
				relationStatus: 'PENDING',
				requesterId: { not: locals.user!.id },
				OR: [{ userLowId: locals.user!.id }, { userHighId: locals.user!.id }]
			},
			select: { userLowId: true, userHighId: true }
		});
		if (!friendship) return 'missing';

		const block = await tx.block.findFirst({
			where: {
				OR: [
					{ blockerId: friendship.userLowId, blockedId: friendship.userHighId },
					{ blockerId: friendship.userHighId, blockedId: friendship.userLowId }
				]
			},
			select: { id: true }
		});
		if (block) return 'blocked';

		const result = await tx.friendship.updateMany({
			where: {
				id: params.relationshipId,
				relationStatus: 'PENDING',
				requesterId: { not: locals.user!.id },
				OR: [{ userLowId: locals.user!.id }, { userHighId: locals.user!.id }]
			},
			data: { relationStatus: 'ACCEPTED' }
		});
		return result.count ? 'accepted' : 'missing';
	});
	if (outcome === 'blocked') return json({ success: false, error: 'Cannot accept a request involving a blocked player' }, { status: 403 });
	if (outcome === 'missing') return json({ success: false, error: 'Request not found or cannot be accepted' }, { status: 404 });
	return json({ success: true });
};

export const DELETE: RequestHandler = async ({ locals, params }) => {
    if (!locals.user) {
        return json({ success: false, error: 'Not authenticated' }, { status: 401 });
    }

    const result = await prisma.friendship.deleteMany({
        where: {
            id: params.relationshipId,
            AND: [
                { OR: [{ userLowId: locals.user.id }, { userHighId: locals.user.id }] },
                {
                    OR: [
                        { relationStatus: 'ACCEPTED' },
                        { relationStatus: 'PENDING', requesterId: { not: locals.user.id } }
                    ]
                }
            ]
        }
    });

    if (!result.count) {
        return json({ success: false, error: 'Relationship not found or cannot be removed' }, { status: 404 });
    }
    return json({ success: true });
};