import type { ChatMessage, IsoDate, Uuid } from '$lib/types';

export type FriendPresenceEvent = {
	type: 'friend-presence';
	userId: Uuid;
	displayName: string;
	online: boolean;
	lastSeen: IsoDate | null;
};

export type ChatMessageEvent = {
	type: 'chat-message';
	message: ChatMessage;
};

export type ServerEvent = FriendPresenceEvent | ChatMessageEvent;
