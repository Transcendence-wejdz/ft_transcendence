<script lang="ts">
	import { page } from '$app/state';
	import { m } from '$lib/paraglide/messages.js';
	import Ribbon from '$lib/components/ui/Ribbon.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import Tag from '$lib/components/ui/Tag.svelte';
	import type { Team } from '$lib/components/ui/Avatar.svelte';
	import warrior from '$lib/assets/emblems/warrior.svg';
	import archer from '$lib/assets/emblems/archer.svg';
	import lancer from '$lib/assets/emblems/lancer.svg';
	import monk from '$lib/assets/emblems/monk.svg';
	import pawn from '$lib/assets/emblems/pawn.svg';
	import Button from '$lib/components/ui/Button.svelte';

	const CLASS_EMBLEMS = { Warrior: warrior, Archer: archer, Lancer: lancer, Monk: monk, Pawn: pawn };

	type Member = {
		name: string;
		login: string;
		github: string;
		role: string;
		team: Team;
		unitClass: keyof typeof CLASS_EMBLEMS;
		photo?: string;
	};

	const members: Member[] = [
		{ name: 'Saru', login: 'zsonie', github: 'SaruM4N3', role: `${m.about_role_product_owner()} · ${m.about_role_developer()}`, team: 'blue', unitClass: 'Warrior', photo: '/team/zsonie.jpg' },
		{ name: 'Eric', login: 'erbuffet', github: 'Nikad0', role: `${m.about_role_backend_lead()} · ${m.about_role_developer()}`, team: 'red', unitClass: 'Archer', photo: '/team/erbuffet.jpg' },
		{ name: 'Jules', login: 'jureix-c', github: 'E-B3rry', role: `${m.about_role_tech_lead()} · ${m.about_role_developer()}`, team: 'purple', unitClass: 'Pawn', photo: '/team/jureix-c.png' },
		{ name: 'Damien', login: 'dabuchhe', github: 'dabuchhe', role: `${m.about_role_scrum_master()} · ${m.about_role_developer()}`, team: 'yellow', unitClass: 'Monk', photo: '/team/dabuchhe.png' },
		{ name: 'William', login: 'willda-s', github: 'willda-s', role: `${m.about_role_frontend_lead()} · ${m.about_role_developer()}`, team: 'black', unitClass: 'Lancer', photo: '/team/willda-s.jpg' }
	];

	const flow = [
		{ name: m.about_flow_browser(), detail: m.about_flow_player() },
		{ name: 'nginx', detail: m.about_flow_proxy() },
		{ name: 'SvelteKit', detail: m.about_flow_pages() },
		{ name: 'PostgreSQL', detail: m.about_flow_orm() },
		{ name: 'Unity WebGL', detail: 'Relay · Netcode', game: true }
	];

	const stack = [
		{ id: 'frontend', label: m.about_stack_frontend(), name: 'SvelteKit', text: m.about_stack_frontend_text() },
		{ id: 'game', label: m.about_stack_game(), name: 'Unity WebGL', text: m.about_stack_game_text() },
		{ id: 'backend', label: m.about_stack_backend(), name: 'SvelteKit + Prisma', text: m.about_stack_backend_text() },
		{ id: 'art', label: m.about_stack_art(), name: 'Tiny Swords', text: m.about_stack_art_text() }
	];

	type Module = {
		name: string;
		type: 'Major' | 'Minor';
		points: number;
		detail: string;
	};

	const modules: Module[] = [
		{
			name: 'Web: framework for front-end and back-end',
			type: 'Major',
			points: 2,
			detail: 'SvelteKit serves both the pages and the API routes, with server-side rendering.'
		},
		{
			name: 'Web: real-time features',
			type: 'Major',
			points: 2,
			detail:
				'Presence, chat and notifications pushed live over WebSockets, with clean handling of disconnections.'
		},
		{
			name: 'Web: user interaction',
			type: 'Major',
			points: 2,
			detail: 'Chat between players, public profiles and a friends list.'
		},
		{
			name: 'Web: ORM for the database',
			type: 'Minor',
			points: 1,
			detail: 'Prisma gives typed queries and versioned SQL migrations for PostgreSQL.'
		},
		{
			name: 'Accessibility: multiple languages',
			type: 'Minor',
			points: 1,
			detail: 'The whole site in at least 3 languages, with a language switcher in the top bar.'
		},
		{
			name: 'Accessibility: additional browsers',
			type: 'Minor',
			points: 1,
			detail: 'Every feature tested and fixed in at least 2 browsers besides Chrome.'
		},
		{
			name: 'User management: standard users and authentication',
			type: 'Major',
			points: 2,
			detail:
				'Sign up and log in with Argon2id-hashed passwords and database sessions. Players edit their name, class, color and avatar, shared with the game.'
		},
		{
			name: 'Gaming: complete web-based game',
			type: 'Major',
			points: 2,
			detail:
				'A wave-survival game made in Unity and played live in the browser: waves of enemies, level-up choices, revives and a game over screen.'
		},
		{
			name: 'Gaming: remote players',
			type: 'Major',
			points: 2,
			detail:
				'Players on different computers join the same session with a code through Unity Relay, and can rejoin after a disconnection.'
		},
		{
			name: 'Gaming: multiplayer, more than two players',
			type: 'Major',
			points: 2,
			detail: 'Up to 4 players in the same session, kept in sync with Netcode for GameObjects.'
		},
		{
			name: 'Gaming: game customization options',
			type: 'Minor',
			points: 1,
			detail: '5 classes × 4 colors, each class with its own abilities.'
		},
		{
			name: 'Gaming: gamification system',
			type: 'Minor',
			points: 1,
			detail:
				'Gold earned in every run to buy permanent upgrades, an XP and level system, and leaderboards, all saved in the database.'
		}
	];

	const totalPoints = modules.reduce((sum, module) => sum + module.points, 0);
</script>

<svelte:head><title>Transcendence — {m.about_ribbon()}</title></svelte:head>

<section class="hero">
	<div class="intro">
		<Ribbon>{m.about_ribbon()}</Ribbon>
		<h1>ft_transcendence</h1>
		<p>{m.about_intro()}</p>
	</div>
	<nav class="jump-links" aria-label={m.about_jump_nav()}>
		<a href="#project">{m.about_jump_project()}</a>
		<a href="#stack">{m.about_jump_stack()}</a>
		<a href="#team">{m.about_jump_team()}</a>
	</nav>
</section>

<section id="project" class="section">
	<h2>{m.about_flow_title()}</h2>
	<Card variant="parchment">
		<div class="flow">
			{#each flow as node, i (node.name)}
				{#if i > 0}
					<span class="arrow" aria-hidden="true">→</span>
				{/if}
				<div class="node" class:game={node.game}>
					<strong>{node.name}</strong>
					<span>{node.detail}</span>
				</div>
			{/each}
		</div>
	</Card>
</section>

<section id="stack" class="section">
	<h2>{m.about_stack_title()}</h2>
	<div class="stack-grid">
		{#each stack as item (item.id)}
			<Card as="article">
				<span class="stack-label">{item.label}</span>
				<h3>{item.name}</h3>
				<p>{item.text}</p>
			</Card>
		{/each}
	</div>
	<Card variant="parchment">
		<details class="scroll">
			<summary>
				<h3 class="modules-title">{m.about_modules_title()}</h3>
				<span class="scroll-count">{m.about_modules_count({ modules: modules.length, points: totalPoints })}</span>
				<span class="scroll-chevron" aria-hidden="true">▾</span>
			</summary>
			<ul class="modules">
				{#each modules as module (module.name)}
					<li>
						<details>
							<summary>
								<span class="module-name">{module.name}</span>
								<Tag>{module.type} · {module.points} pt{module.points > 1 ? 's' : ''}</Tag>
							</summary>
							<p>{module.detail}</p>
						</details>
					</li>
				{/each}
			</ul>
		</details>
	</Card>
</section>
<section id="team" class="section">
	<Ribbon>{m.about_team_ribbon()}</Ribbon>
	<h2 class="team-title">{m.about_team_title()}</h2>
	<div class="team-grid">
		{#each members as member (member.login)}
			<Card variant="parchment" as="article">
				<div class="member">
					<div class="banner" style:background="var(--color-team-{member.team})">
						<img src={CLASS_EMBLEMS[member.unitClass]} alt={member.unitClass} title={member.unitClass} width="54" height="54" />
					</div>
					{#if member.photo}
						<img class="photo" src={member.photo} alt="" />
					{:else}
						<span class="photo" aria-hidden="true">{member.name.charAt(0)}</span>
					{/if}
					<h3>{member.name}</h3>
					<span class="login">{member.login}</span>
					<p>{member.role}</p>
					<div class="links">
						<a href="https://github.com/{member.github}" aria-label={m.about_member_github({ name: member.name })}>
							<svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" aria-hidden="true">
								<path d="M9 19c-4 1.5-4-2-6-2.5M15 21v-3.5c0-1 .1-1.4-.5-2 2.8-.3 5.5-1.4 5.5-6a4.6 4.6 0 0 0-1.3-3.2 4.2 4.2 0 0 0-.1-3.2s-1.1-.3-3.5 1.3a12 12 0 0 0-6.2 0C6.5 2.8 5.4 3.1 5.4 3.1a4.2 4.2 0 0 0-.1 3.2A4.6 4.6 0 0 0 4 9.5c0 4.6 2.7 5.7 5.5 6-.6.6-.6 1.2-.5 2V21" />
							</svg>
						</a>
						<a href="https://profile.intra.42.fr/users/{member.login}" aria-label={m.about_member_intra({ name: member.name })}>42</a>
					</div>
				</div>
			</Card>
		{/each}
	</div>
</section>
<Card as="section">
	<div class="cta">
		<div>
			<h2>{m.about_cta_title()}</h2>
			<p>
				{page.data.user ? m.about_cta_player() : m.about_cta_guest()}
			</p>
		</div>
		<div class="cta-actions">
			{#if page.data.user}
				<Button href="/game" data-sveltekit-reload>{m.about_cta_play()}</Button>
			{:else}
				<Button href="/register">{m.about_cta_register()}</Button>
			{/if}
		</div>
	</div>
</Card>

<style>
	.hero {
		display: flex;
		flex-wrap: wrap;
		align-items: flex-end;
		gap: 2rem;
		margin-bottom: 3rem;
	}

	.intro {
		flex: 1 1 32.5rem;
		min-width: 0;
	}

	h1 {
		margin: 1.125rem 0 1rem;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: clamp(3.25rem, 8vw, 5.5rem);
		line-height: 0.95;
	}

	.intro p {
		max-width: 36.25rem;
		margin: 0;
		color: var(--color-text-secondary);
		font-size: 1.125rem;
		line-height: 1.65;
	}

	.jump-links {
		display: flex;
		flex-wrap: wrap;
		gap: var(--space-sm);
	}

	.jump-links a {
		display: inline-flex;
		align-items: center;
		min-height: var(--control-sm);
		padding: 0 var(--space-md);
		border: var(--border-width) solid var(--color-drop);
		border-radius: var(--radius);
		background: var(--color-bg-raised);
		color: var(--color-text);
		font-family: var(--font-ui);
		font-size: var(--text-md);
		text-decoration: none;
	}

	.jump-links a:hover {
		color: #fff;
		background: var(--color-bg-raised-hover);
	}

	.section {
		margin-bottom: 3.5rem;
	}

	h2 {
		margin: 0 0 1.25rem;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 2.75rem;
	}

	.flow {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.75rem;
	}

	.node {
		flex: 1 1 8.75rem;
		padding: var(--space-md);
		background: var(--color-input-bg);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		text-align: center;
	}

	.node strong {
		display: block;
		font-family: var(--font-ui);
		font-size: 1.125rem;
	}

	.node span {
		color: var(--color-parchment-text-muted);
		font-size: var(--text-sm);
	}

	.node.game {
		background: #d9f1ee;
	}

	.node.game span {
		color: #24504e;
	}

	.arrow {
		font-family: var(--font-ui);
		font-size: 1.625rem;
		color: var(--color-ribbon);
	}

	.stack-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(15.625rem, 1fr));
		gap: 1.25rem;
		margin-bottom: 1.25rem;
	}

	.stack-label {
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: 0.8125rem;
		letter-spacing: 0.14em;
		text-transform: uppercase;
		color: var(--color-gold);
	}

	h3 {
		margin: var(--space-xs) 0;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 1.875rem;
	}

	.stack-grid p {
		margin: 0;
		color: var(--color-text-secondary);
		line-height: 1.6;
	}

	.scroll > summary {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-xs) var(--space-md);
		list-style: none;
		cursor: pointer;
	}

	.scroll > summary::-webkit-details-marker {
		display: none;
	}

	.modules-title {
		margin: 0;
	}

	.scroll-count {
		flex: 1;
		font-family: var(--font-ui);
		color: var(--color-parchment-text-muted);
	}

	.scroll-chevron {
		font-family: var(--font-ui);
		font-size: 1.5rem;
		color: var(--color-ribbon);
		transition: transform 200ms ease;
	}

	.scroll[open] .scroll-chevron {
		transform: rotate(180deg);
	}

	/* Unroll animation where supported (Chrome 131+), instant toggle elsewhere */
	.scroll {
		interpolate-size: allow-keywords;
	}

	.scroll::details-content {
		height: 0;
		overflow: clip;
		transition: height 300ms ease, content-visibility 300ms allow-discrete;
	}

	.scroll[open]::details-content {
		height: auto;
	}

	@media (prefers-reduced-motion: reduce) {
		.scroll::details-content,
		.scroll-chevron {
			transition: none;
		}
	}

	.modules {
		display: grid;
		gap: var(--space-sm);
		margin: 0.875rem 0 0;
		padding: 0;
		list-style: none;
	}

	.modules li {
		background: var(--color-input-bg);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
	}

	.modules summary {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: var(--space-xs) var(--space-sm);
		padding: 0.75rem 0.875rem;
		cursor: pointer;
	}

	.modules summary::before {
		content: '▸';
		color: var(--color-ribbon);
		transition: transform 150ms ease;
	}

	.modules details[open] summary::before {
		transform: rotate(90deg);
	}

	summary:focus-visible {
		outline: 3px solid var(--color-focus);
		outline-offset: 2px;
	}

	.module-name {
		flex: 1;
		min-width: 0;
	}

	.modules p {
		margin: 0;
		padding: 0 0.875rem 0.75rem;
		color: var(--color-parchment-text-muted);
		line-height: 1.55;
	}

	.team-title {
		margin: 1rem 0 1.5rem;
	}

	.team-grid {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(11.875rem, 1fr));
		gap: 1.25rem;
	}

	.member {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: var(--space-sm);
		text-align: center;
	}

	.banner {
		display: grid;
		place-items: center;
		width: 4.75rem;
		height: 5.75rem;
		margin-top: calc(-1 * var(--space-lg));
		padding-bottom: 0.875rem;
		border: var(--border-width) solid var(--color-outline);
		border-top: 0;
		clip-path: polygon(0 0, 100% 0, 100% 100%, 50% 82%, 0 100%);
	}

	.banner img {
		image-rendering: pixelated;
	}

	.photo {
		display: grid;
		place-items: center;
		width: 5.75rem;
		height: 5.75rem;
		margin-top: 0.25rem;
		border: var(--border-width) solid var(--color-outline);
		border-radius: 10px;
		background: var(--color-parchment-shadow);
		object-fit: cover;
		font-family: var(--font-title);
		font-size: 2.5rem;
		color: var(--color-parchment-text-muted);
	}

	.member h3 {
		margin: 0.25rem 0 0;
		font-size: 2rem;
		line-height: 1;
	}

	.login {
		font-family: var(--font-ui);
		font-size: 0.9375rem;
		color: var(--color-parchment-text-muted);
	}

	.member p {
		margin: 0;
		color: var(--color-parchment-text-muted);
		line-height: 1.5;
	}

	.links {
		display: flex;
		gap: var(--space-xs);
		margin-top: 0.25rem;
	}

	.links a {
		display: grid;
		place-items: center;
		width: var(--control-sm);
		height: var(--control-sm);
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius);
		background: var(--color-input-bg);
		color: var(--color-parchment-text);
		font-family: var(--font-ui);
		font-weight: 700;
		font-size: 0.9375rem;
		text-decoration: none;
	}

	.links a:hover {
		background: #fff;
	}

	.links a:focus-visible {
		outline: 3px solid var(--color-focus);
		outline-offset: 2px;
	}

	.cta {
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		justify-content: space-between;
		gap: 1.25rem;
	}

	.cta h2 {
		margin: 0;
		font-size: 2.125rem;
	}

	.cta p {
		margin: 0.375rem 0 0;
		color: var(--color-text-secondary);
	}

	.cta-actions {
		display: flex;
		flex-wrap: wrap;
		gap: 0.75rem;
	}
</style>
