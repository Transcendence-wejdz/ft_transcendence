import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import { hashToken, SESSION_COOKIE } from '$lib/server/session.js';
import type { RequestHandler } from './$types';

export const POST: RequestHandler = async ({ locals, cookies }) => {
	const token = cookies.get(SESSION_COOKIE);
	if (!locals.user || !token) {
		return json({ success: false, error: 'Not authenticated' }, { status: 401 });
	}

	const updated = await prisma.session.updateMany({
		where: {
			id: hashToken(token),
			userId: locals.user.id,
			expiresAt: { gt: new Date() }
		},
		data: { lastOnline: new Date() }
	});

	if (updated.count === 0) {
		return json({ success: false, error: 'Invalid session' }, { status: 401 });
	}

	return json({ success: true }, { headers: { 'Cache-Control': 'no-store' } });
};
