// Chat rules shared by the /chat pages and the chat API, so both refuse the same messages.
import { MESSAGE_MAX_LENGTH, type CHAT_ERROR_CODES } from '$lib/types';

type MessageTextError = (typeof CHAT_ERROR_CODES)[number];

// The text as it is sent and saved: spaces around it carry no meaning.
export function cleanMessageText(text: string): string {
	return text.trim();
}

// Why a cleaned text cannot be sent, or null when it can.
export function messageTextError(text: string): MessageTextError | null {
	if (!text) return 'message_empty';
	if (text.length > MESSAGE_MAX_LENGTH) return 'message_too_long';
	return null;
}
