// POST /api/players/[playerId]/messages/read: the logged-in player has read the chat with this friend.
// Called by /chat/[id] when it opens, and when a message arrives while it is open (#118).
import { json } from '@sveltejs/kit';
import { chatRefusal, failure, markConversationRead } from '$lib/server/chat.js';
import type { ApiSuccess } from '$lib/types';
import type { RequestHandler } from './$types';

export const POST: RequestHandler = async ({ locals, params }) => {
	if (!locals.user) return failure(401, 'not_authenticated');

	const refusal = await chatRefusal(locals.user.id, params.playerId);
	if (refusal) return failure(refusal.status, refusal.code);

	await markConversationRead(locals.user.id, params.playerId);
	return json({ success: true } satisfies ApiSuccess);
};
