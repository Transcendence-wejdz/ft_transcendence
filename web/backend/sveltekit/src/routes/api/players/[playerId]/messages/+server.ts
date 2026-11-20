// /api/players/[playerId]/messages: the chat between the logged-in player and a friend.
// POST sends a message, GET reads the history one page at a time.
import { json } from '@sveltejs/kit';
import { cleanMessageText, messageTextError } from '$lib/chat';
import { chatRefusal, deliverMessage, failure, findMessagesPage, readJson, saveMessage } from '$lib/server/chat.js';
import { isUuid } from '$lib/server/validation.js';
import type { ApiSuccess, MessagesPage, SentMessage } from '$lib/types';
import type { RequestHandler } from './$types';

export const POST: RequestHandler = async ({ locals, params, request }) => {
	if (!locals.user) return failure(401, 'not_authenticated');

	const body = await readJson(request);
	if (body === undefined) return failure(400, 'invalid_json');

	const text = readMessageText(body);
	if (text === null) return failure(400, 'invalid_body');

	const textError = messageTextError(text);
	if (textError) return failure(400, textError);

	const refusal = await chatRefusal(locals.user.id, params.playerId);
	if (refusal) return failure(refusal.status, refusal.code);

	const message = await saveMessage(locals.user.id, params.playerId, text);
	deliverMessage(message);
	return json({ success: true, message } satisfies ApiSuccess<SentMessage>, { status: 201 });
};

// ?before=<message id>: the page of messages older than that one. Without it, the latest page.
export const GET: RequestHandler = async ({ locals, params, url }) => {
	if (!locals.user) return failure(401, 'not_authenticated');

	const before = url.searchParams.get('before');
	if (before !== null && !isUuid(before)) return failure(400, 'invalid_body');

	const refusal = await chatRefusal(locals.user.id, params.playerId);
	if (refusal) return failure(refusal.status, refusal.code);

	const page = await findMessagesPage(locals.user.id, params.playerId, before);
	return json({ success: true, ...page } satisfies ApiSuccess<MessagesPage>);
};

// The cleaned text of a { text } body, or null when the body has no text string.
function readMessageText(body: unknown): string | null {
	const text = (body as { text?: unknown } | null)?.text;
	return typeof text === 'string' ? cleanMessageText(text) : null;
}
