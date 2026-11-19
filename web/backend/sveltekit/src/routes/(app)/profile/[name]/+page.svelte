<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { toast } from 'svelte-sonner';
	import Card from '$lib/components/ui/Card.svelte';
	import Tag from '$lib/components/ui/Tag.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Avatar, { type Team } from '$lib/components/ui/Avatar.svelte';
	import warrior from '$lib/assets/emblems/warrior.svg';
	import archer from '$lib/assets/emblems/archer.svg';
	import lancer from '$lib/assets/emblems/lancer.svg';
	import monk from '$lib/assets/emblems/monk.svg';
	import pawn from '$lib/assets/emblems/pawn.svg';
	import type { GameMode, ModeStats, PlayerClass } from '$lib/types';

	const CLASS_EMBLEMS: Record<PlayerClass, string> = { Warrior: warrior, Archer: archer, Lancer: lancer, Monk: monk, Pawn: pawn };
	const MODES: GameMode[] = ['solo', 'coop'];
	const MAX_UPGRADE_LEVEL = 10;

	// English until Paraglide reaches develop (#119): then getLocale() and m.…()
	const LOCALE = 'en';
	const MODE_LABELS: Record<GameMode, string> = { solo: 'Solo', coop: 'Coop' };

	// Same names and order as the game (LevelUpBonus.GetDisplayName and DisplayOrder)
	const UPGRADES = [
		{ type: 'Health', label: 'Health' },
		{ type: 'HealthRegen', label: 'Health Regen' },
		{ type: 'ManaMax', label: 'Mana' },
		{ type: 'ManaRegen', label: 'Mana Regen' },
		{ type: 'Damage', label: 'Damage' },
		{ type: 'AttackSpeed', label: 'Attack Speed' },
		{ type: 'SpecialCooldown', label: 'Special Cooldown' },
		{ type: 'Size', label: 'Attack Size' },
		{ type: 'AttackMobility', label: 'Attack Mobility' },
		{ type: 'MoveSpeed', label: 'Move Speed' }
	];

	let { data } = $props();
	const profile = $derived(data.profile);
	const team = $derived(profile.color.toLowerCase() as Team);

	const numbers = new Intl.NumberFormat(LOCALE);

	// Best values keep the best mode, counts add both modes up
	const statCards = $derived.by(() => {
		const { solo, coop } = profile.stats;
		const cards: { key: keyof ModeStats; label: string; total: number }[] = [
			{ key: 'bestScore', label: 'Best score', total: Math.max(solo.bestScore, coop.bestScore) },
			{ key: 'bestWave', label: 'Best wave', total: Math.max(solo.bestWave, coop.bestWave) },
			{ key: 'kills', label: 'Enemies killed', total: solo.kills + coop.kills },
			{ key: 'games', label: 'Games played', total: solo.games + coop.games }
		];
		return cards;
	});

	const joined = $derived(
		profile.createdAt
			? new Intl.DateTimeFormat(LOCALE, { dateStyle: 'long', timeZone: 'UTC' }).format(new Date(profile.createdAt))
			: null
	);
	const lastSeen = $derived(profile.lastSeenAt ? timeAgo(profile.lastSeenAt) : null);

	// "2 hr. ago", "yesterday", "now": the largest unit that fits
	function timeAgo(iso: string): string {
		const seconds = Math.round((new Date(iso).getTime() - Date.now()) / 1000);
		const format = new Intl.RelativeTimeFormat(LOCALE, { numeric: 'auto', style: 'short' });
		const units: [Intl.RelativeTimeFormatUnit, number][] = [['week', 604_800], ['day', 86_400], ['hour', 3_600], ['minute', 60]];
		for (const [unit, size] of units) {
			if (Math.abs(seconds) >= size) return format.format(Math.round(seconds / size), unit);
		}
		return format.format(0, 'second');
	}

	// 11:42, or 1:05:09 past an hour
	function duration(totalSeconds: number): string {
		const hours = Math.floor(totalSeconds / 3_600);
		const minutes = Math.floor((totalSeconds % 3_600) / 60);
		const seconds = String(totalSeconds % 60).padStart(2, '0');
		return hours ? `${hours}:${String(minutes).padStart(2, '0')}:${seconds}` : `${minutes}:${seconds}`;
	}

	// Which friend action is running, to disable the buttons and spin the right one
	let pending = $state<'add' | 'accept' | 'remove' | null>(null);

	async function friendAction(action: NonNullable<typeof pending>, url: string, init: RequestInit) {
		pending = action;
		try {
			const response = await fetch(url, { ...init, headers: { 'Content-Type': 'application/json' } });
			const result = await response.json().catch(() => null);
			if (!response.ok || !result?.success) {
				toast.error(result?.error ?? 'Something went wrong. Try again.');
				return;
			}
			await invalidateAll();
		} catch {
			toast.error('Network error. Check your connection.');
		} finally {
			pending = null;
		}
	}

	const addFriend = () =>
		friendAction('add', '/api/friends-list', { method: 'POST', body: JSON.stringify({ displayName: profile.displayName }) });
	const acceptRequest = () =>
		friendAction('accept', `/api/friends-list/relationships/${profile.relationshipId}`, {
			method: 'PATCH',
			body: JSON.stringify({ action: 'accept' })
		});
	const removeRelationship = () =>
		friendAction('remove', `/api/friends-list/relationships/${profile.relationshipId}`, { method: 'DELETE' });
</script>

<svelte:head><title>Transcendence — {profile.displayName}</title></svelte:head>

<Card variant="parchment" as="section" aria-label="Player">
	<div class="header">
		<div class="banner" class:dark={team === 'black'} style:background="var(--color-team-{team})">
			<img src={CLASS_EMBLEMS[profile.class]} alt="" width="54" height="54" />
			<span>{profile.class}</span>
		</div>

		<div class="avatar">
			<Avatar size="lg" name={profile.displayName} {team} src={profile.avatarUrl ?? undefined} />
		</div>

		<div class="identity">
			<h1>{profile.displayName}</h1>
			<div class="tags">
				{#if profile.online === true}
					<Tag dot="var(--color-online)">Online</Tag>
				{:else if profile.online === false}
					<Tag dot="#8f8775">Offline{lastSeen ? ` · ${lastSeen}` : ''}</Tag>
				{/if}
				<Tag dot="var(--color-team-{team})">{profile.color} · {profile.class}</Tag>
				{#if joined}
					<Tag>Joined {joined}</Tag>
				{/if}
			</div>
			<!-- Level and XP bar: #106 -->
		</div>

		<div class="actions">
			{#if profile.relationship === 'self'}
				<Button href="/settings">Edit profile</Button>
			{:else if profile.relationship === 'friends'}
				<Tag style="min-height: var(--control-sm)">
					<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="square" aria-hidden="true">
						<path d="M5 12l5 5 9-10" />
					</svg>
					Friends
				</Tag>
				<Button variant="secondary" busy={pending === 'remove'} disabled={pending !== null} onclick={removeRelationship}>
					Remove friend
				</Button>
			{:else if profile.relationship === 'request_sent'}
				<!-- The API cannot cancel a sent request yet: DELETE only removes friends or received requests -->
				<Tag style="min-height: var(--control-sm)">Request sent</Tag>
			{:else if profile.relationship === 'request_received'}
				<Button busy={pending === 'accept'} disabled={pending !== null} onclick={acceptRequest}>Accept request</Button>
				<Button variant="secondary" busy={pending === 'remove'} disabled={pending !== null} onclick={removeRelationship}>
					Decline
				</Button>
			{:else}
				<Button busy={pending === 'add'} disabled={pending !== null} onclick={addFriend}>
					<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="square" aria-hidden="true">
						<path d="M12 5v14M5 12h14" />
					</svg>
					Add friend
				</Button>
			{/if}
		</div>
	</div>
</Card>

<section class="section" aria-labelledby="stats-title">
	<h2 id="stats-title">Stats</h2>
	<div class="stats">
		{#each statCards as card (card.key)}
			<Card>
				<div class="stat">
					<div>
						<div class="stat-value">{numbers.format(card.total)}</div>
						<div class="stat-label">{card.label}</div>
					</div>
					<div class="split">
						{#each MODES as mode (mode)}
							<div class="split-row">
								<span class="pill mode-{mode}">{MODE_LABELS[mode]}</span>
								<b>{numbers.format(profile.stats[mode][card.key])}</b>
							</div>
						{/each}
					</div>
				</div>
			</Card>
		{/each}
	</div>
</section>

<div class="duo">
	<!-- Rows become links to /leaderboard/{mode} once #106 adds the page -->
	<section class="ranking" aria-labelledby="ranking-title">
		<h2 id="ranking-title">Ranking</h2>
		<Card>
			{#each MODES as mode (mode)}
				{@const rank = profile.ranks[mode]}
				<div class="rank-row">
					<span class="pill mode-{mode}">{MODE_LABELS[mode]}</span>
					<span class="rank-label">
						{rank
							? `of ${numbers.format(rank.players)} ${rank.players === 1 ? 'player' : 'players'} · best score`
							: 'No finished game yet'}
					</span>
					<span class="rank-value">{rank ? `#${numbers.format(rank.rank)}` : '—'}</span>
				</div>
			{/each}
		</Card>
	</section>

	<section class="upgrades" aria-labelledby="upgrades-title">
		<h2 id="upgrades-title">Upgrades</h2>
		<Card style="padding: 0.5rem 1.125rem">
			<ul class="upgrade-list">
				{#each UPGRADES as upgrade (upgrade.type)}
					{@const level = profile.upgrades[upgrade.type] ?? 0}
					<li>
						<span class="upgrade-head">
							<span>{upgrade.label}</span>
							<span class="upgrade-level">{level} / {MAX_UPGRADE_LEVEL}</span>
						</span>
						<span class="segments" role="img" aria-label="Level {level} of {MAX_UPGRADE_LEVEL}">
							{#each { length: MAX_UPGRADE_LEVEL }, i (i)}
								<span class="segment" class:filled={i < level}></span>
							{/each}
						</span>
					</li>
				{/each}
			</ul>
		</Card>
	</section>
</div>

<section class="section" aria-labelledby="history-title">
	<h2 id="history-title">Last 10 games</h2>
	{#if profile.lastRuns.length}
		<Card style="padding: 0; overflow-x: auto">
			<table class="history">
				<thead>
					<tr>
						<th scope="col">Result</th>
						<th scope="col">Mode</th>
						<th scope="col">Score</th>
						<th scope="col">Wave</th>
						<th scope="col" class="optional">Kills</th>
						<th scope="col" class="optional">Length</th>
						<th scope="col">Date</th>
					</tr>
				</thead>
				<tbody>
					{#each profile.lastRuns as run (run.id)}
						<tr>
							<td><span class="result" class:victory={run.victory}>{run.victory ? 'Victory' : 'Defeat'}</span></td>
							<td><span class="pill mode-{run.mode}">{MODE_LABELS[run.mode]}</span></td>
							<td class="score">{numbers.format(run.score)}</td>
							<td class="wave">{numbers.format(run.wave)}</td>
							<td class="optional muted">{numbers.format(run.kills)}</td>
							<td class="optional muted">{duration(run.seconds)}</td>
							<td class="muted">{timeAgo(run.finishedAt)}</td>
						</tr>
					{/each}
				</tbody>
			</table>
		</Card>
	{:else}
		<div class="empty">
			<h3>No games yet</h3>
			<p>
				{profile.relationship === 'self'
					? 'Your first run will show up here with its score and wave.'
					: `${profile.displayName} has not finished a game yet.`}
			</p>
			{#if profile.relationship === 'self'}
				<Button href="/game" data-sveltekit-reload>Start a run</Button>
			{/if}
		</div>
	{/if}
</section>

<style>
	.header {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 1.75rem;
	}

	.banner {
		align-self: flex-start;
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: var(--space-sm);
		width: 6.5rem;
		min-height: 12.25rem;
		margin-top: calc(-1 * var(--space-lg));
		padding: 1.25rem var(--space-xs) 2.5rem;
		border: var(--border-width) solid var(--color-outline);
		border-top: 0;
		color: var(--color-outline);
		box-shadow: inset 0 -6px 0 rgb(0 0 0 / 0.2);
		clip-path: polygon(0 0, 100% 0, 100% 100%, 50% 86%, 0 100%);
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: var(--text-sm);
		letter-spacing: 0.08em;
		text-transform: uppercase;
	}

	.banner.dark {
		color: var(--color-text);
	}

	.banner img {
		image-rendering: pixelated;
	}

	.avatar,
	.identity,
	.actions {
		margin-top: 1.75rem;
	}

	.identity {
		flex: 1 1 18.75rem;
		min-width: 0;
	}

	h1 {
		margin: 0;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: clamp(3rem, 6vw, 4.25rem);
		line-height: 1;
		overflow-wrap: anywhere;
	}

	.tags {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-xs);
		margin-top: 0.75rem;
	}

	.actions {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-sm);
	}

	.section {
		margin-top: 2rem;
	}

	h2 {
		margin: 0 0 1rem;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 2.125rem;
	}

	.stats {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(13.75rem, 1fr));
		gap: 1rem;
	}

	.stat {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
		text-align: center;
	}

	.stat-value {
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: 1.875rem;
		line-height: 1;
		color: var(--color-gold);
	}

	.stat-label {
		margin-top: 0.375rem;
		color: var(--color-text-secondary);
		font-size: var(--text-sm);
	}

	.split {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
		padding-top: 0.75rem;
		border-top: 2px solid var(--color-border-subtle);
		font-size: var(--text-sm);
	}

	.split-row {
		display: flex;
		justify-content: center;
		align-items: center;
		gap: 0.875rem;
	}

	.split b {
		font-family: var(--font-ui);
		color: var(--color-text);
	}

	.pill {
		display: inline-flex;
		justify-content: center;
		min-width: 3rem;
		padding: 2px 8px;
		border: 2px solid var(--color-outline);
		border-radius: 4px;
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: 0.8125rem;
		color: var(--color-outline);
	}

	.mode-solo {
		background: var(--color-gold);
	}

	.mode-coop {
		background: var(--color-online);
	}

	.duo {
		display: flex;
		flex-wrap: wrap;
		align-items: flex-start;
		gap: 1.75rem;
		margin-top: 2rem;
	}

	.ranking {
		flex: 1 1 18.75rem;
		min-width: 0;
	}

	.rank-row {
		display: flex;
		align-items: center;
		gap: 0.875rem;
	}

	.rank-row:not(:last-child) {
		padding-bottom: 0.875rem;
	}

	.rank-row + .rank-row {
		padding-top: 0.875rem;
		border-top: 2px solid var(--color-border-subtle);
	}

	.rank-label {
		flex: 1;
		color: var(--color-text-secondary);
		font-size: var(--text-sm);
	}

	.rank-value {
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: 1.875rem;
		color: var(--color-gold);
	}

	.upgrades {
		flex: 999 1 32.5rem;
		min-width: 0;
	}

	.upgrade-list {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(13.75rem, 1fr));
		column-gap: 1.75rem;
		margin: 0;
		padding: 0;
		list-style: none;
	}

	.upgrade-list li {
		display: flex;
		flex-direction: column;
		gap: 0.375rem;
		padding: 0.625rem 0;
		border-bottom: 2px solid var(--color-border-subtle);
	}

	.upgrade-head {
		display: flex;
		justify-content: space-between;
		gap: var(--space-xs);
		font-size: var(--text-sm);
	}

	.upgrade-level {
		color: var(--color-text-secondary);
	}

	.segments {
		display: grid;
		grid-template-columns: repeat(10, 1fr);
		gap: 3px;
	}

	.segment {
		height: 10px;
		border: 2px solid var(--color-drop);
		border-radius: 2px;
		background: var(--color-bg-page);
	}

	.segment.filled {
		background: var(--color-gold);
	}

	.history {
		width: 100%;
		min-width: 45rem;
		border-collapse: collapse;
		font-size: 0.9375rem;
	}

	th,
	td {
		text-align: center;
	}

	th {
		padding: 0.875rem 0.625rem;
		font-family: var(--font-ui);
		font-weight: 600;
		font-size: var(--text-sm);
		color: var(--color-text-secondary);
	}

	td {
		padding: 0.75rem 0.625rem;
	}

	tbody tr {
		border-top: 2px solid var(--color-border-subtle);
	}

	.result {
		display: inline-flex;
		padding: 2px 10px;
		border: 2px solid var(--color-outline);
		border-radius: 4px;
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: 0.8125rem;
		background: #3a2229;
		color: #ffb3b7;
	}

	.result.victory {
		background: var(--color-online);
		color: var(--color-outline);
	}

	.score {
		font-family: var(--font-ui);
		font-weight: 700;
		color: var(--color-gold);
	}

	.wave {
		font-family: var(--font-ui);
	}

	.muted {
		color: var(--color-text-secondary);
	}

	.empty {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.625rem;
		padding: 1.5rem;
		border: 3px dashed #4a4560;
		border-radius: var(--radius-card);
		text-align: center;
	}

	.empty h3 {
		margin: 0;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 1.75rem;
	}

	.empty p {
		margin: 0;
		color: var(--color-text-secondary);
	}

	@media (max-width: 640px) {
		.history {
			min-width: 0;
		}

		.optional {
			display: none;
		}
	}
</style>
