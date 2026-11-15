import { error } from '@sveltejs/kit';
import { prisma } from '$lib/server/db.js';
import { readUpgrades } from '$lib/server/upgrades.js';
import type { GameMode, ModeRank, ModeStats, PlayerClass, PlayerColor, PublicProfile, RelationshipStatus } from '$lib/types';
import type { PageServerLoad } from './$types';

const MODES: GameMode[] = ['solo', 'coop'];
const LAST_RUNS = 10;
const PRESENCE_LEASE_MS = 60_000; // same lease as the friends list
const FINISHED = { finishedAt: { not: null } };

export const load: PageServerLoad = async ({ params, locals }) => {
	const user = await prisma.user.findUnique({
		where: { displayName: params.name },
		select: { id: true, displayName: true, color: true, class: true, avatarUrl: true, createdAt: true, lastSeen: true, upgrades: true }
	});
	if (!user) error(404);

	// (app)/ guarantees a session
	const meId = locals.user!.id;
	const isMe = user.id === meId;
	const [userLowId, userHighId] = meId < user.id ? [meId, user.id] : [user.id, meId];

	const [connections, lastSession, friendship, perMode, runs, ranks] = await Promise.all([
		prisma.presenceConnection.count({
			where: { userId: user.id, heartbeatAt: { gt: new Date(Date.now() - PRESENCE_LEASE_MS) } }
		}),
		prisma.session.findFirst({
			where: { userId: user.id },
			orderBy: { lastOnline: 'desc' },
			select: { lastOnline: true }
		}),
		isMe
			? null
			: prisma.friendship.findUnique({
					where: { userLowId_userHighId: { userLowId, userHighId } },
					select: { id: true, requesterId: true, relationStatus: true }
				}),
		prisma.gameRun.groupBy({
			by: ['mode'],
			where: { userId: user.id, ...FINISHED },
			_max: { score: true, wave: true },
			_sum: { kills: true },
			_count: { _all: true }
		}),
		prisma.gameRun.findMany({
			where: { userId: user.id, ...FINISHED },
			orderBy: { finishedAt: 'desc' },
			take: LAST_RUNS,
			select: { id: true, mode: true, score: true, wave: true, kills: true, seconds: true, victory: true, finishedAt: true }
		}),
		Promise.all(MODES.map((mode) => rankIn(mode, user.id)))
	]);

	const stats = Object.fromEntries(
		MODES.map((mode) => {
			const row = perMode.find((r) => r.mode === mode);
			return [mode, {
				bestScore: row?._max.score ?? 0,
				bestWave: row?._max.wave ?? 0,
				kills: row?._sum.kills ?? 0,
				games: row?._count._all ?? 0
			}];
		})
	) as Record<GameMode, ModeStats>;

	const lastSeen = latest(user.lastSeen, lastSession?.lastOnline ?? null);

	const profile: PublicProfile = {
		id: user.id,
		displayName: user.displayName,
		color: user.color as PlayerColor,
		avatarUrl: user.avatarUrl,
		class: user.class as PlayerClass,
		createdAt: user.createdAt?.toISOString() ?? null,
		online: connections > 0,
		lastSeenAt: lastSeen?.toISOString() ?? null,
		relationship: isMe ? 'self' : relationshipOf(meId, friendship),
		relationshipId: friendship?.id ?? null,
		stats,
		ranks: { solo: ranks[0], coop: ranks[1] },
		upgrades: readUpgrades(user.upgrades),
		lastRuns: runs.map((run) => ({
			id: run.id,
			mode: run.mode as GameMode,
			score: run.score ?? 0,
			wave: run.wave ?? 0,
			kills: run.kills ?? 0,
			seconds: run.seconds ?? 0,
			victory: run.victory ?? false,
			finishedAt: run.finishedAt!.toISOString()
		}))
	};

	return { profile };
};

// Rank by best score: 1 + the number of players with a strictly better best score.
async function rankIn(mode: GameMode, userId: string): Promise<ModeRank | null> {
	const bests = await prisma.gameRun.groupBy({
		by: ['userId'],
		where: { mode, ...FINISHED, score: { not: null } },
		_max: { score: true }
	});
	const mine = bests.find((best) => best.userId === userId)?._max.score;
	if (mine == null) return null;
	return {
		rank: bests.filter((best) => (best._max.score ?? 0) > mine).length + 1,
		players: bests.length
	};
}

function relationshipOf(
	meId: string,
	row: { requesterId: string; relationStatus: 'PENDING' | 'ACCEPTED' } | null
): RelationshipStatus {
	if (!row) return 'none';
	if (row.relationStatus === 'ACCEPTED') return 'friends';
	return row.requesterId === meId ? 'request_sent' : 'request_received';
}

function latest(a: Date | null, b: Date | null): Date | null {
	if (!a) return b;
	if (!b) return a;
	return a > b ? a : b;
}
