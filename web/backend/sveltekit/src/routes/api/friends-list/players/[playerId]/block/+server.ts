import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import type { RequestHandler } from './$types';

const isUuid = (value: string) => /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i.test(value);

async function getTarget(playerId: string) {
	if (!isUuid(playerId)) return null;
	return prisma.user.findUnique({ where: { id: playerId }, select: { id: true } });
}

export const GET: RequestHandler = async ({ locals, params }) => {
	if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });
	const target = await getTarget(params.playerId);
	if (!target) return json({ success: false, error: 'Player not found' }, { status: 404 });
	if (target.id === locals.user.id) return json({ success: false, error: 'Invalid player' }, { status: 400 });

	const block = await prisma.block.findUnique({
		where: { blockerId_blockedId: { blockerId: locals.user.id, blockedId: target.id } },
		select: { id: true }
	});
	return json({ success: true, blocked: !!block }, { headers: { 'Cache-Control': 'no-store' } });
};

export const POST: RequestHandler = async ({ locals, params }) => {
	if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });
	const target = await getTarget(params.playerId);
	if (!target) return json({ success: false, error: 'Player not found' }, { status: 404 });
	if (target.id === locals.user.id) return json({ success: false, error: 'You cannot block yourself' }, { status: 400 });

	const userLowId = locals.user.id < target.id ? locals.user.id : target.id;
	const userHighId = locals.user.id < target.id ? target.id : locals.user.id;
	await prisma.$transaction(async (tx) => {
		await tx.block.upsert({
			where: { blockerId_blockedId: { blockerId: locals.user!.id, blockedId: target.id } },
			create: { blockerId: locals.user!.id, blockedId: target.id },
			update: {}
		});
		await tx.friendship.deleteMany({ where: { userLowId, userHighId } });
	});

	return json({ success: true });
};

export const DELETE: RequestHandler = async ({ locals, params }) => {
	if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });
	const target = await getTarget(params.playerId);
	if (!target) return json({ success: false, error: 'Player not found' }, { status: 404 });
	if (target.id === locals.user.id) return json({ success: false, error: 'Invalid player' }, { status: 400 });

	await prisma.block.deleteMany({ where: { blockerId: locals.user.id, blockedId: target.id } });
	return json({ success: true });
};