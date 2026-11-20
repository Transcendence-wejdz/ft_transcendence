// Shared by the chat routes: who may chat with whom, and the API shape of a message.
import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import type { ApiErrorCode, ApiFailure, ChatMessage, PlayerColor, UserSummary, Uuid } from '$lib/types';

const UUID_PATTERN = /^[0-9a-f]{8}-(?:[0-9a-f]{4}-){3}[0-9a-f]{12}$/i;

// Prisma selects for the rows the chat routes send back.
export const userSummarySelect = { id: true, displayName: true, color: true, avatarUrl: true } as const;
export const chatMessageSelect = { id: true, senderId: true, receiverId: true, text: true, sentAt: true } as const;

type UserSummaryRow = { id: string; displayName: string; color: string; avatarUrl: string | null };
type ChatMessageRow = { id: string; senderId: string; receiverId: string; text: string; sentAt: Date };

// A failed response as types.ts describes it: the status, then { success: false, code }.
export function failure(status: number, code: ApiErrorCode) {
	return json({ success: false, code } satisfies ApiFailure, { status });
}

export function toUserSummary(row: UserSummaryRow): UserSummary {
	return { ...row, color: row.color as PlayerColor };
}

export function toChatMessage(row: ChatMessageRow): ChatMessage {
	return { ...row, sentAt: row.sentAt.toISOString() };
}

// Why userId may not chat with peerId, or null when they may.
// The block is checked first and both ways, so a blocked player only ever learns `blocked`.
export async function chatRefusal(userId: Uuid, peerId: string): Promise<{ status: number; code: ApiErrorCode } | null> {
	if (!UUID_PATTERN.test(peerId)) return { status: 404, code: 'player_not_found' };

	const [userLowId, userHighId] = userId < peerId ? [userId, peerId] : [peerId, userId];
	const [peer, block, friendship] = await Promise.all([
		prisma.user.findUnique({ where: { id: peerId }, select: { id: true } }),
		prisma.block.findFirst({
			where: {
				OR: [
					{ blockerId: userId, blockedId: peerId },
					{ blockerId: peerId, blockedId: userId }
				]
			},
			select: { id: true }
		}),
		prisma.friendship.findUnique({
			where: { userLowId_userHighId: { userLowId, userHighId } },
			select: { relationStatus: true }
		})
	]);

	if (!peer) return { status: 404, code: 'player_not_found' };
	if (block) return { status: 403, code: 'blocked' };
	if (friendship?.relationStatus !== 'ACCEPTED') return { status: 403, code: 'not_friends' };
	return null;
}
