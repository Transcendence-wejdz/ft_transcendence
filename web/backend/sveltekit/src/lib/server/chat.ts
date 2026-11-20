// Server side of the chat: who may chat with whom, the message queries and live delivery.
import { json } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import { sendToUser } from '$lib/server/realtime.js';
import { isUuid } from '$lib/server/validation.js';
import type { ApiErrorCode, ApiFailure, ChatMessage, MessagesPage, Uuid } from '$lib/types';

// Messages per page of history: enough to fill the screen, small enough to load fast.
const PAGE_SIZE = 30;

// Prisma select for the message rows the chat routes send back.
const messageSelect = { id: true, senderId: true, receiverId: true, text: true, sentAt: true } as const;

type MessageRow = { id: Uuid; senderId: Uuid; receiverId: Uuid; text: string; sentAt: Date };

// ---- Responses ----

// A failed response as types.ts describes it: the status, then { success: false, code }.
export function failure(status: number, code: ApiErrorCode) {
	return json({ success: false, code } satisfies ApiFailure, { status });
}

// The parsed JSON body, or undefined when the body is not valid JSON.
export async function readJson(request: Request): Promise<unknown> {
	try {
		return await request.json();
	} catch {
		return undefined;
	}
}

// ---- Access rules ----

export type ChatRefusal = { status: 403 | 404; code: ApiErrorCode };

// Why userId may not chat with peerId, or null when they may.
// The block is checked first and both ways, so a blocked player only ever learns `blocked`.
export async function chatRefusal(userId: Uuid, peerId: string): Promise<ChatRefusal | null> {
	if (!isUuid(peerId)) return { status: 404, code: 'player_not_found' };

	const [exists, blocked, friends] = await Promise.all([
		playerExists(peerId),
		isBlockedEitherWay(userId, peerId),
		areFriends(userId, peerId)
	]);
	if (!exists) return { status: 404, code: 'player_not_found' };
	if (blocked) return { status: 403, code: 'blocked' };
	if (!friends) return { status: 403, code: 'not_friends' };
	return null;
}

async function playerExists(id: Uuid): Promise<boolean> {
	return (await prisma.user.count({ where: { id } })) > 0;
}

async function isBlockedEitherWay(userId: Uuid, peerId: Uuid): Promise<boolean> {
	const userBlockedPeer = { blockerId: userId, blockedId: peerId };
	const peerBlockedUser = { blockerId: peerId, blockedId: userId };
	const blocks = await prisma.block.count({ where: { OR: [userBlockedPeer, peerBlockedUser] } });
	return blocks > 0;
}

// A friendship row stores each pair once, the lower id first.
async function areFriends(userId: Uuid, peerId: Uuid): Promise<boolean> {
	const [userLowId, userHighId] = userId < peerId ? [userId, peerId] : [peerId, userId];
	const friendship = await prisma.friendship.findUnique({
		where: { userLowId_userHighId: { userLowId, userHighId } },
		select: { relationStatus: true }
	});
	return friendship?.relationStatus === 'ACCEPTED';
}

// ---- Messages ----

function toChatMessage(row: MessageRow): ChatMessage {
	return { ...row, sentAt: row.sentAt.toISOString() };
}

// Both directions of the conversation between two players.
function between(userId: Uuid, peerId: Uuid) {
	const sent = { senderId: userId, receiverId: peerId };
	const received = { senderId: peerId, receiverId: userId };
	return { OR: [sent, received] };
}

// Newest first. The id breaks ties between messages sent in the same millisecond.
function newestFirst() {
	return [{ sentAt: 'desc' as const }, { id: 'desc' as const }];
}

// Writes the message to the database.
export async function saveMessage(senderId: Uuid, receiverId: Uuid, text: string): Promise<ChatMessage> {
	const row = await prisma.message.create({ data: { senderId, receiverId, text }, select: messageSelect });
	return toChatMessage(row);
}

// One page of history, newest first. before: the id of the oldest message already shown.
export async function findMessagesPage(userId: Uuid, peerId: Uuid, before: Uuid | null): Promise<MessagesPage> {
	// One extra row tells whether an older page exists.
	const rows = await prisma.message.findMany({
		where: between(userId, peerId),
		orderBy: newestFirst(),
		take: PAGE_SIZE + 1,
		...(before ? { cursor: { id: before }, skip: 1 } : {}),
		select: messageSelect
	});
	const items = rows.slice(0, PAGE_SIZE).map(toChatMessage);
	return { items, nextCursor: rows.length > PAGE_SIZE ? items[items.length - 1].id : null };
}

// ---- Live delivery ----

// Pushes a new message to the receiver's open tabs and to the sender's other tabs.
// The sending tab already shows it and skips the same id.
export function deliverMessage(message: ChatMessage) {
	const event = { type: 'chat-message', message } as const;
	sendToUser(message.receiverId, event);
	sendToUser(message.senderId, event);
}
