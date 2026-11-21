// GET /api/conversations: every row of /chat, loaded once.
import { json } from '@sveltejs/kit';
import { failure, listConversations } from '$lib/server/chat.js';
import type { ApiSuccess, ConversationsList } from '$lib/types';
import type { RequestHandler } from './$types';

export const GET: RequestHandler = async ({ locals }) => {
	if (!locals.user) return failure(401, 'not_authenticated');

	const conversations = await listConversations(locals.user.id);
	return json({ success: true, conversations } satisfies ApiSuccess<ConversationsList>);
};
