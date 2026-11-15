import { json } from '@sveltejs/kit';
import { SESSION_COOKIE, hashToken } from '$lib/server/session.js';
import { prisma } from '$lib/server/db.js';
import type { RequestHandler } from './$types';

export const POST: RequestHandler = async ({ cookies }) => {
    try {
        const token = cookies.get(SESSION_COOKIE);

        if (token) {
            const sessionId = hashToken(token);
            const session = await prisma.session.findUnique({ where: { id: sessionId }, select: { userId: true } });
            await prisma.session.deleteMany({ where: { id: sessionId } });
            if (session) {
                const activeSessions = await prisma.session.count({
                    where: { userId: session.userId, expiresAt: { gt: new Date() } }
                });
                if (activeSessions === 0) {
                    await prisma.user.update({ where: { id: session.userId }, data: { lastSeen: new Date() } });
                }
            }
        }

        cookies.delete(SESSION_COOKIE, { path: '/' });

        return json({ success: true });
    } catch (error) {
        console.error('Logout failed:', error);
        return json({ success: false, error: 'Logout failed' }, { status: 500 });
    }
};
