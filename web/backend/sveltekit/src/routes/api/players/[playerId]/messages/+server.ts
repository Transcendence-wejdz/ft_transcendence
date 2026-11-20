// /api/players/[playerId]/messages: the chat between the logged-in player and a friend.
// POST sends a message, GET reads the history one page at a time.
import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import { sendToUser } from '$lib/server/realtime';
import { UUID_PATTERN, chatMessageSelect, chatRefusal, failure, toChatMessage } from '$lib/server/chat';
import { MESSAGE_MAX_LENGTH, type ApiSuccess, type MessagesPage, type SentMessage } from '$lib/types';
import type { RequestHandler } from './$types';

// Messages per page of history: enough to fill the screen, small enough to load fast.
const PAGE_SIZE = 30;

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

// ?before=<message id>: the page of messages older than that one. Without it, the latest page.
export const GET: RequestHandler = async ({ locals, params, url }) => {
	if (!locals.user) return failure(401, 'not_authenticated');

	const before = url.searchParams.get('before');
	if (before !== null && !UUID_PATTERN.test(before)) return failure(400, 'invalid_body');

	const refusal = await chatRefusal(locals.user.id, params.playerId);
	if (refusal) return failure(refusal.status, refusal.code);

	// One extra row tells whether an older page exists.
	const rows = await prisma.message.findMany({
		where: {
			OR: [
				{ senderId: locals.user.id, receiverId: params.playerId },
				{ senderId: params.playerId, receiverId: locals.user.id }
			]
		},
		orderBy: [{ sentAt: 'desc' }, { id: 'desc' }],
		take: PAGE_SIZE + 1,
		...(before ? { cursor: { id: before }, skip: 1 } : {}),
		select: chatMessageSelect
	});

	const items = rows.slice(0, PAGE_SIZE).map(toChatMessage);
	const nextCursor = rows.length > PAGE_SIZE ? items[items.length - 1].id : null;

	return json({ success: true, items, nextCursor } satisfies ApiSuccess<MessagesPage>);
};
