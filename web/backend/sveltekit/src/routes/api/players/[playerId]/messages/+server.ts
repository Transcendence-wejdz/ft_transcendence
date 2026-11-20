// POST /api/players/[playerId]/messages: send a chat message to a friend.
import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import { sendToUser } from '$lib/server/realtime';
import { chatMessageSelect, chatRefusal, failure, toChatMessage } from '$lib/server/chat';
import { MESSAGE_MAX_LENGTH, type ApiSuccess, type SentMessage } from '$lib/types';
import type { RequestHandler } from './$types';

export const POST: RequestHandler = async ({ locals, params, request }) => {
	if (!locals.user) return failure(401, 'not_authenticated');

	let body: unknown;
	try {
		body = await request.json();
	} catch {
		return failure(400, 'invalid_json');
	}

	const text = (body as { text?: unknown } | null)?.text;
	if (typeof text !== 'string') return failure(400, 'invalid_body');

	const trimmed = text.trim();
	if (!trimmed) return failure(400, 'message_empty');
	if (trimmed.length > MESSAGE_MAX_LENGTH) return failure(400, 'message_too_long');

	const refusal = await chatRefusal(locals.user.id, params.playerId);
	if (refusal) return failure(refusal.status, refusal.code);

	const row = await prisma.message.create({
		data: { senderId: locals.user.id, receiverId: params.playerId, text: trimmed },
		select: chatMessageSelect
	});
	const message = toChatMessage(row);

	// The receiver's open tabs, and the sender's other tabs (the sending tab skips this id).
	sendToUser(params.playerId, { type: 'chat-message', message });
	sendToUser(locals.user.id, { type: 'chat-message', message });

	return json({ success: true, message } satisfies ApiSuccess<SentMessage>, { status: 201 });
};
