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

	const result = await prisma.friendship.updateMany({
		where: {
			id: params.relationshipId,
			relationStatus: 'PENDING',
			requesterId: { not: locals.user.id },
			OR: [{ userLowId: locals.user.id }, { userHighId: locals.user.id }]
		},
		data: { relationStatus: 'ACCEPTED' }
	});
	if (!result.count) return json({ success: false, error: 'Request not found or cannot be accepted' }, { status: 404 });
	return json({ success: true });
};

export const DELETE: RequestHandler = async ({ locals, params }) => {
	if (!locals.user) return json({ success: false, error: 'Not authenticated' }, { status: 401 });

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
	if (!result.count) return json({ success: false, error: 'Relationship not found or cannot be removed' }, { status: 404 });
	return json({ success: true });
};
