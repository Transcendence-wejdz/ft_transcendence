// Pushes /ws events from API routes. server.mjs runs the WebSocket server in the
// same Node process and shares its open connections on globalThis.
import type { ServerEvent } from '$lib/realtime/protocol';
import type { Uuid } from '$lib/types';

type Connections = Map<Uuid, Set<{ readyState: number; send(data: string): void }>>;

const OPEN = 1; // WebSocket.OPEN, without importing ws into the app bundle

// Sends the event to every open tab of this user. Does nothing when the user has
// no tab open: the message is already saved and loads with the history.
export function sendToUser(userId: Uuid, event: ServerEvent) {
	const connections = (globalThis as typeof globalThis & { realtimeConnections?: Connections })
		.realtimeConnections;
	const data = JSON.stringify(event);
	for (const socket of connections?.get(userId) ?? []) {
		if (socket.readyState === OPEN) socket.send(data);
	}
}

