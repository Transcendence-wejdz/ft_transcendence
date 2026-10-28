import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import { isUniqueViolation } from '$lib/server/validation.js';
import type { RequestHandler } from './$types';

const userSummary = { id: true, displayName: true, color: true } as const;

export const GET: RequestHandler = async ({ locals }) => {
	if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

	const rows = await prisma.friendship.findMany({
		where: {
			OR: [{ userLowId: locals.user.id }, { userHighId: locals.user.id }]
		},
		select: {
			id: true,
			requesterId: true,
			userLowId: true,
			userHighId: true,
			relationStatus: true,
			userLow: { select: userSummary },
			userHigh: { select: userSummary }
		},
		orderBy: { createdAt: 'desc' }
	});

	const friends = rows
		.filter((row) => row.relationStatus === 'ACCEPTED')
		.map((row) => ({
			relationshipId: row.id,
			...(row.userLowId === locals.user!.id ? row.userHigh : row.userLow)
	}));
	const requests = rows
		.filter((row) => row.relationStatus === 'PENDING' && row.requesterId !== locals.user!.id)
		.map((row) => ({
			relationshipId: row.id,
			...(row.userLowId === locals.user!.id ? row.userHigh : row.userLow)
		}));

	const friendIds = friends.map((friend) => friend.id);
	const onlineSince = new Date(Date.now() - 15_000);

	const activeSessions = await prisma.session.findMany({
		where: {
			userId: { in: friendIds },
			expiresAt: { gt: new Date() },
			lastOnline: { gt: onlineSince }
		},
		select: { userId: true }
	});

	const onlineUserIds = new Set(activeSessions.map((session) => session.userId));
	return json(
		{
			success: true,
			friends: friends.map((friend) => ({
				...friend,
				online: onlineUserIds.has(friend.id)
			})),
			requests
		},
		{ headers: { 'Cache-Control': 'no-store' } }
	);
};

export const POST: RequestHandler = async ({ locals, request }) => {
	if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

	let body: unknown;
	try {
		body = await request.json();
	} catch {
		return json({ success: false, error: 'Invalid JSON' }, { status: 400 });
	}
	if (!body || typeof body !== 'object' || !('displayName' in body) || typeof body.displayName !== 'string' || !body.displayName.trim()) {
		return json({ success: false, error: 'Display name is required' }, { status: 400 });
	}

	const target = await prisma.user.findUnique({
		where: { displayName: body.displayName.trim() },
		select: { id: true }
	});
	if (!target) return json({ success: false, error: 'Player not found' }, { status: 404 });
	if (target.id === locals.user.id) return json({ success: false, error: 'You cannot add yourself' }, { status: 400 });
	const block = await prisma.block.findFirst({
		where: {
			OR: [
				{ blockerId: locals.user.id, blockedId: target.id },
				{ blockerId: target.id, blockedId: locals.user.id }
			]
		},
		select: { id: true }
	});
	if (block) return json({ success: false, error: 'Cannot send a friend request to this player' }, { status: 403 });

	const userLowId = locals.user.id < target.id ? locals.user.id : target.id;
	const userHighId = locals.user.id < target.id ? target.id : locals.user.id;
	try {
		await prisma.friendship.create({
			data: { requesterId: locals.user.id, userLowId, userHighId }
		});
	} catch (error) {
		if (isUniqueViolation(error)) {
			const existing = await prisma.friendship.findUnique({
				where: { userLowId_userHighId: { userLowId, userHighId } },
				select: { relationStatus: true, requesterId: true }
			});
        if (existing?.relationStatus === 'PENDING' && existing.requesterId !== locals.user.id) {
                await prisma.friendship.update({
                    where: { userLowId_userHighId: { userLowId, userHighId } },
                    data: { relationStatus: 'ACCEPTED' }
                });
					return json({ success: true }, { status: 200 });
			}
			return json({ success: false, error: existing?.relationStatus === 'ACCEPTED' ? 'Already friends' : 'Request already sent' }, { status: 409 });
		}
		throw error;
	}

	return json({ success: true }, { status: 201 });
};
