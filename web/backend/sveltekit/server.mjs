import { createHash, randomUUID } from 'node:crypto';
import { createServer } from 'node:http';
import { parse } from 'node:url';
import { Pool } from 'pg';
import { WebSocket, WebSocketServer } from 'ws';
import { handler } from './build/handler.js';

// The adapter-node handler serves normal HTTP requests; this pool is also used
// to authenticate WebSocket upgrades and refresh session activity.
const pool = new Pool({
	host: process.env.POSTGRES_HOST || 'db',
	port: Number(process.env.POSTGRES_PORT || 5432),
	user: process.env.POSTGRES_USER,
	password: process.env.POSTGRES_PASSWORD,
	database: process.env.POSTGRES_DB
});

const server = createServer(handler);
// Attach WebSockets to the same HTTP server so nginx can proxy both protocols
// through the existing app port.
const webSocketServer = new WebSocketServer({ noServer: true });
const WEB_SOCKET_HEARTBEAT_INTERVAL_MS = 25_000;
// Tracks connections in this Node process, grouped by user so multiple tabs
// still count as one online user.
const connectionsByUser = new Map();

// The browser sends the raw session cookie; the database stores only its hash.
function getSessionToken(request) {
	const cookieHeader = request.headers.cookie;
	if (!cookieHeader) return null;

	for (const cookie of cookieHeader.split(';')) {
		const separator = cookie.indexOf('=');
		if (separator < 0 || cookie.slice(0, separator).trim() !== 'session') continue;

		try {
			return decodeURIComponent(cookie.slice(separator + 1).trim());
		} catch {
			return null;
		}
	}

	return null;
}

function rejectUpgrade(socket, statusCode, statusText) {
	if (socket.destroyed) return;
	socket.write(`HTTP/1.1 ${statusCode} ${statusText}\r\nConnection: close\r\n\r\n`);
	socket.destroy();
}

// Only allow browser connections from the same origin. nginx forwards the
// original host (including its port) and the public request protocol.
function hasSameOrigin(request) {
	const origin = request.headers.origin;
	const forwardedHost = request.headers['x-forwarded-host'];
	const forwardedProto = request.headers['x-forwarded-proto'];
	const host = (forwardedHost || request.headers.host || '').split(',')[0].trim();
	const protocol = (forwardedProto || (request.socket.encrypted ? 'https' : 'http')).split(',')[0].trim();
	if (!origin || !host || !['http', 'https'].includes(protocol)) return false;

	try {
		return new URL(origin).origin === new URL(`${protocol}://${host}`).origin;
	} catch {
		return false;
	}
}

// Notify only connected users who are friends with this user. The client uses
// this message to update friend status and show online alerts.
async function broadcastPresence(userId, online) {
	try {
		const friends = await pool.query(
			`SELECT friend.id AS friend_id, friend.display_name
			 FROM "friendship" AS relationship
			 JOIN "users" AS friend
			   ON friend.id = CASE
			     WHEN relationship."user_Low_id" = $1::uuid THEN relationship."user_High_id"
			     ELSE relationship."user_Low_id"
			   END
			 WHERE relationship."relationStatus" = 'ACCEPTED'
			   AND (relationship."user_Low_id" = $1::uuid OR relationship."user_High_id" = $1::uuid)`,
			[userId]
		);

		for (const { friend_id: friendId, display_name: displayName } of friends.rows) {
			const message = JSON.stringify({
				type: 'friend-presence',
				userId,
				displayName,
				online,
				lastSeen: online ? null : new Date().toISOString()
			});
			for (const friendSocket of connectionsByUser.get(friendId) ?? []) {
				if (friendSocket.readyState === WebSocket.OPEN) friendSocket.send(message);
			}
		}
	} catch (error) {
		console.error('Could not broadcast friend presence:', error);
	}
}

// Upgrade only the authenticated /ws endpoint. Origin and session checks run
// before the HTTP connection is switched to the WebSocket protocol.
server.on('upgrade', async (request, socket, head) => {
	if (parse(request.url || '').pathname !== '/ws') {
		rejectUpgrade(socket, 404, 'Not Found');
		return;
	}
	if (!hasSameOrigin(request)) {
		rejectUpgrade(socket, 403, 'Forbidden');
		return;
	}

	const token = getSessionToken(request);
	if (!token) {
		rejectUpgrade(socket, 401, 'Unauthorized');
		return;
	}

	try {
		// Match the app's SHA-256 session-token hashing and reject expired sessions.
		const sessionId = createHash('sha256').update(token).digest('hex');
		const result = await pool.query(
			'SELECT id, user_id FROM sessions WHERE id = $1 AND expires_at > NOW()',
			[sessionId]
		);

		if (socket.destroyed) return;
		if (result.rowCount === 0) {
			rejectUpgrade(socket, 401, 'Unauthorized');
			return;
		}
		const activity = await pool.query(
			'UPDATE sessions SET last_online = NOW() WHERE id = $1 AND expires_at > NOW()',
			[result.rows[0].id]
		);
		if (socket.destroyed) return;
		if (activity.rowCount === 0) {
			rejectUpgrade(socket, 401, 'Unauthorized');
			return;
		}
		const connectionId = randomUUID();
		await pool.query(
			'INSERT INTO presence_connections (id, user_id, heartbeat_at) VALUES ($1, $2, NOW())',
			[connectionId, result.rows[0].user_id]
		);
		if (socket.destroyed) {
			await pool.query('DELETE FROM presence_connections WHERE id = $1', [connectionId]);
			return;
		}

		// Store identity on the socket so later pongs and close events can update
		// the right session and presence-connection row.
		webSocketServer.handleUpgrade(request, socket, head, (webSocket) => {
			webSocket.userId = result.rows[0].user_id;
			webSocket.sessionId = result.rows[0].id;
			webSocket.connectionId = connectionId;
			webSocketServer.emit('connection', webSocket, request);
		});
	} catch (error) {
		console.error('WebSocket authentication failed:', error);
		rejectUpgrade(socket, 500, 'Internal Server Error');
	}
});

webSocketServer.on('connection', (webSocket) => {
	webSocket.isAlive = true;
	let userConnections = connectionsByUser.get(webSocket.userId);
	if (!userConnections) {
		userConnections = new Set();
		connectionsByUser.set(webSocket.userId, userConnections);
	}
	const becameOnline = userConnections.size === 0;
	userConnections.add(webSocket);
	// Broadcast only on the transition from no open tabs to the first open tab.
	if (becameOnline) void broadcastPresence(webSocket.userId, true);

	// Browsers automatically answer WebSocket ping frames with pong frames. A
	// pong refreshes last_online; an expired or revoked session is closed.
	webSocket.on('pong', async () => {
		webSocket.isAlive = true;
		try {
			const result = await pool.query(
				`WITH valid_session AS (
					UPDATE sessions SET last_online = NOW() WHERE id = $1 AND expires_at > NOW() RETURNING id
				)
				UPDATE presence_connections SET heartbeat_at = NOW()
				WHERE id = $2 AND EXISTS (SELECT 1 FROM valid_session)`,
				[webSocket.sessionId, webSocket.connectionId]
			);
			if (result.rowCount === 0) webSocket.close(4001, 'Session expired');
		} catch (error) {
			console.error('WebSocket heartbeat update failed:', error);
			webSocket.terminate();
		}
	});

	// Mark the user offline only after their final tab/connection closes.
	webSocket.on('close', async () => {
		try {
			await pool.query('DELETE FROM presence_connections WHERE id = $1', [webSocket.connectionId]);
		} catch (error) {
			console.error('Could not remove closed WebSocket presence:', error);
		}

		const activeConnections = connectionsByUser.get(webSocket.userId);
		if (!activeConnections) return;
		activeConnections.delete(webSocket);
		if (activeConnections.size === 0) {
			connectionsByUser.delete(webSocket.userId);
			void broadcastPresence(webSocket.userId, false);
		}
	});
});

// Ping each connection periodically. If the previous ping got no pong, the
// connection is presumed dead and terminated so its close handler can run.
const webSocketHeartbeat = setInterval(() => {
	// Remove rows left behind if the process or network disappeared before a
	// WebSocket close event could delete its presence record.
	void pool
		.query("DELETE FROM presence_connections WHERE heartbeat_at <= NOW() - INTERVAL '60 seconds'")
		.catch((error) => console.error('Could not remove stale WebSocket presence:', error));

	for (const webSocket of webSocketServer.clients) {
		if (webSocket.isAlive === false) {
			webSocket.terminate();
			continue;
		}
		if (webSocket.readyState === WebSocket.OPEN) {
			webSocket.isAlive = false;
			webSocket.ping();
		}
	}
}, WEB_SOCKET_HEARTBEAT_INTERVAL_MS);

const port = Number(process.env.PORT || 3000);
const host = process.env.HOST || '0.0.0.0';
server.listen(port, host, () => {
	console.log(`App listening on http://${host}:${port}`);
});

// Stop the timer and release database connections during server shutdown.
server.on('close', () => {
	clearInterval(webSocketHeartbeat);
	void pool.end();
});
