<script lang="ts">
	import { page } from '$app/state';
	import { m } from '$lib/paraglide/messages.js';
	import Ribbon from '$lib/components/ui/Ribbon.svelte';
	import Card from '$lib/components/ui/Card.svelte';
	import Tag from '$lib/components/ui/Tag.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import type { Team } from '$lib/components/ui/Avatar.svelte';
	import warrior from '$lib/assets/emblems/warrior.svg';
	import archer from '$lib/assets/emblems/archer.svg';
	import lancer from '$lib/assets/emblems/lancer.svg';
	import monk from '$lib/assets/emblems/monk.svg';
	import pawn from '$lib/assets/emblems/pawn.svg';

	// Units walking on the island, in map pixels. delay: walk cycle, then march offset
	const SPRITES = [
		{ kind: 'sheep', size: 128, left: 900, top: 1400 },
		{ kind: 'sheep', size: 128, left: 1500, top: 700, delay: '0s, -7s' },
		{ kind: 'sheep', size: 128, left: 640, top: 1720, delay: '0s, -11s' },
		{ kind: 'lancer-blue', size: 320, left: 470, top: 905, delay: '0s, -8s' },
		{ kind: 'warrior-blue', size: 192, left: 560, top: 1020 },
		{ kind: 'archer-blue', size: 192, left: 500, top: 1100, delay: '0s, -4s' },
		{ kind: 'warrior-blue', size: 192, left: 540, top: 1130, delay: '0s, -6s' },
		{ kind: 'monk-blue', size: 192, left: 520, top: 1170, delay: '0s, -2s' },
		{ kind: 'lancer-red', size: 320, left: 1590, top: 915, delay: '0s, -1s' },
		{ kind: 'warrior-red', size: 192, left: 1660, top: 1030, delay: '0s, -3s' },
		{ kind: 'archer-red', size: 192, left: 1700, top: 1110, delay: '0s, -7s' },
		{ kind: 'warrior-red', size: 192, left: 1640, top: 1140, delay: '0s, -10s' },
		{ kind: 'monk-red', size: 192, left: 1680, top: 1180, delay: '0s, -9s' },
		{ kind: 'fight-blue', size: 192, left: 1060, top: 1050 },
		{ kind: 'fight-red', size: 192, left: 1150, top: 1050, delay: '-0.27s' }
	];

	const CLOUDS = [
		{ src: '/images/home/cloud-1.png', top: '4%', duration: '90s', delay: '-20s' },
		{ src: '/images/home/cloud-2.png', top: '38%', duration: '70s', delay: '-50s' },
		{ src: '/images/home/cloud-3.png', top: '62%', duration: '110s', delay: '-5s' },
		{ src: '/images/home/cloud-4.png', top: '18%', duration: '95s', delay: '-75s' }
	];

	const CLASSES: { name: () => string; team: Team; emblem: string; text: () => string }[] = [
		{ name: m.class_warrior, team: 'blue', emblem: warrior, text: m.home_class_warrior },
		{ name: m.class_archer, team: 'red', emblem: archer, text: m.home_class_archer },
		{ name: m.class_lancer, team: 'purple', emblem: lancer, text: m.home_class_lancer },
		{ name: m.class_monk, team: 'yellow', emblem: monk, text: m.home_class_monk },
		{ name: m.class_pawn, team: 'black', emblem: pawn, text: m.home_class_pawn }
	];

	const MODES: { name: string; text: () => string; soon?: boolean }[] = [
		{ name: 'Solo', text: m.home_mode_solo },
		{ name: 'Coop', text: m.home_mode_coop },
		{ name: '2v2', text: m.home_mode_2v2 },
		{ name: '3v1', text: m.home_mode_3v1, soon: true }
	];
</script>

<svelte:head><title>Transcendence</title></svelte:head>

<!-- The island behind the whole page: a camera travelling over the map, with units and clouds -->
<div class="stage" aria-hidden="true">
	<div class="camera">
		{#each SPRITES as sprite, i (i)}
			<div
				class="sprite size-{sprite.size} {sprite.kind}"
				style:left="{sprite.left}px"
				style:top="{sprite.top}px"
				style:animation-delay={sprite.delay}
			></div>
		{/each}
	</div>
	{#each CLOUDS as cloud (cloud.src)}
		<img
			class="cloud"
			src={cloud.src}
			alt=""
			style:top={cloud.top}
			style:animation-duration={cloud.duration}
			style:animation-delay={cloud.delay}
		/>
	{/each}
</div>

<section class="hero">
	<div class="board hero-board">
		<Ribbon style="white-space: normal">{m.home_ribbon()}</Ribbon>
		<h1>{m.home_title_start()}<br /><span>{m.home_title_end()}</span></h1>
		<p class="intro">{m.home_intro()}</p>
		<Button href="/game" data-sveltekit-reload>
			{m.home_play()}
			<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" stroke-linecap="square" aria-hidden="true">
				<path d="M5 12h13M13 6l6 6-6 6" />
			</svg>
		</Button>
		<a class="about-link" href="/about">{m.home_about_link()} →</a>
	</div>
</section>

<section class="board block" aria-labelledby="classes-title">
	<Ribbon>{m.home_classes_ribbon()}</Ribbon>
	<h2 id="classes-title">{m.home_classes_title()}</h2>
	<div class="classes">
		{#each CLASSES as unitClass (unitClass.emblem)}
			<Card variant="parchment" as="article">
				<div class="class-card">
					<span class="emblem" style:background="var(--color-team-{unitClass.team})">
						<img src={unitClass.emblem} alt="" width="36" height="36" />
					</span>
					<h3>{unitClass.name()}</h3>
					<p>{unitClass.text()}</p>
				</div>
			</Card>
		{/each}
	</div>
</section>

<section class="board block" aria-labelledby="modes-title">
	<Ribbon>{m.home_modes_ribbon()}</Ribbon>
	<h2 id="modes-title">{m.home_modes_title()}</h2>
	<div class="modes">
		{#each MODES as mode (mode.name)}
			<Card>
				<div class="mode" class:soon={mode.soon}>
					<span class="mode-name">{mode.name}</span>
					<p>{mode.text()}</p>
					{#if mode.soon}
						<span class="soon-tag"><Tag>{m.home_coming_soon()}</Tag></span>
					{/if}
				</div>
			</Card>
		{/each}
	</div>
</section>

<Card variant="parchment" as="section">
	<div class="cta">
		<div>
			<h2>{m.home_cta_title()}</h2>
			<p>{page.data.user ? m.home_cta_player() : m.home_cta_guest()}</p>
		</div>
		<div class="cta-actions">
			{#if page.data.user}
				<Button href="/game" data-sveltekit-reload>{m.home_play()}</Button>
			{:else}
				<Button href="/register">{m.home_cta_register()}</Button>
				<Button variant="secondary" href="/login">{m.home_cta_login()}</Button>
			{/if}
		</div>
	</div>
</Card>

<style>
	/* ---- The island ---- */

	.stage {
		position: fixed;
		inset: 0;
		z-index: -1;
		overflow: hidden;
		background: #47aba9;
	}

	.camera {
		position: absolute;
		left: 50%;
		top: 50%;
		width: 2440px;
		height: 2200px;
		background: url('/images/home/island-map.webp') 0 0 / 2440px 2200px no-repeat;
		image-rendering: pixelated;
		transform-origin: 0 0;
		transform: translate(-1680px, -1470px) scale(1.4);
		animation: travel 70s ease-in-out infinite;
		will-change: transform;
	}

	@keyframes travel {
		0%, 8% { transform: translate(-1680px, -1470px) scale(1.4); }
		20%, 27% { transform: translate(-3211px, -1118px) scale(2.6); }
		37%, 43% { transform: translate(-2304px, -1248px) scale(2.4); }
		53%, 60% { transform: translate(-2080px, -4108px) scale(2.6); }
		70%, 78% { transform: translate(-3600px, -3540px) scale(3); }
		88%, 93% { transform: translate(-4512px, -3000px) scale(2.4); }
		100% { transform: translate(-1680px, -1470px) scale(1.4); }
	}

	.sprite {
		position: absolute;
		background-repeat: no-repeat;
		image-rendering: pixelated;
	}

	.size-128 { width: 128px; height: 128px; }
	.size-192 { width: 192px; height: 192px; }
	.size-320 { width: 320px; height: 320px; }

	/* One keyframe per sheet width: the strip slides one frame at a time with steps() */
	@keyframes frames-512 { to { background-position: -512px 0; } }
	@keyframes frames-768 { to { background-position: -768px 0; } }
	@keyframes frames-1152 { to { background-position: -1152px 0; } }
	@keyframes frames-1920 { to { background-position: -1920px 0; } }

	@keyframes march-right {
		0% { transform: translateX(0); opacity: 0; }
		6%, 90% { opacity: 1; }
		100% { transform: translateX(470px); opacity: 0; }
	}

	@keyframes march-left {
		0% { transform: translateX(0) scaleX(-1); opacity: 0; }
		6%, 90% { opacity: 1; }
		100% { transform: translateX(-470px) scaleX(-1); opacity: 0; }
	}

	@keyframes graze {
		0% { transform: translateX(0); }
		45% { transform: translateX(140px); }
		50% { transform: translateX(140px) scaleX(-1); }
		95% { transform: translateX(0) scaleX(-1); }
		100% { transform: translateX(0); }
	}

	.warrior-blue { background-image: url('/images/home/warrior-blue-run.png'); animation: frames-1152 0.6s steps(6) infinite, march-right 12s linear infinite; }
	.warrior-red { background-image: url('/images/home/warrior-red-run.png'); animation: frames-1152 0.6s steps(6) infinite, march-left 12s linear infinite; }
	.archer-blue { background-image: url('/images/home/archer-blue-run.png'); animation: frames-768 0.5s steps(4) infinite, march-right 12s linear infinite; }
	.archer-red { background-image: url('/images/home/archer-red-run.png'); animation: frames-768 0.5s steps(4) infinite, march-left 12s linear infinite; }
	.lancer-blue { background-image: url('/images/home/lancer-blue-run.png'); animation: frames-1920 0.6s steps(6) infinite, march-right 12s linear infinite; }
	.lancer-red { background-image: url('/images/home/lancer-red-run.png'); animation: frames-1920 0.6s steps(6) infinite, march-left 12s linear infinite; }
	.monk-blue { background-image: url('/images/home/monk-blue-run.png'); animation: frames-768 0.5s steps(4) infinite, march-right 12s linear infinite; }
	.monk-red { background-image: url('/images/home/monk-red-run.png'); animation: frames-768 0.5s steps(4) infinite, march-left 12s linear infinite; }
	.fight-blue { background-image: url('/images/home/warrior-blue-attack.png'); animation: frames-768 0.55s steps(4) infinite; }
	.fight-red { background-image: url('/images/home/warrior-red-attack.png'); animation: frames-768 0.55s steps(4) infinite; transform: scaleX(-1); }
	.sheep { background-image: url('/images/home/sheep.png'); animation: frames-512 0.7s steps(4) infinite, graze 16s linear infinite; }

	.cloud {
		position: absolute;
		left: 0;
		width: 1152px;
		height: 512px;
		max-width: none;
		image-rendering: pixelated;
		opacity: 0.95;
		animation: drift 80s linear infinite;
	}

	@keyframes drift {
		from { transform: translateX(-1200px); }
		to { transform: translateX(100vw); }
	}

	@media (prefers-reduced-motion: reduce) {
		.camera,
		.sprite,
		.cloud {
			animation: none;
		}
	}

	/* ---- Content ---- */

	.board {
		background: rgb(29 27 38 / 0.94);
		border: var(--border-width) solid var(--color-drop);
		border-radius: 10px;
		box-shadow: 0 8px 0 rgb(11 10 16 / 0.55);
	}

	.hero {
		display: flex;
		flex-wrap: wrap;
		justify-content: center;
		align-items: stretch;
		gap: 3rem;
		padding-bottom: 4rem;
	}

	.hero-board {
		flex: 1 1 26.25rem;
		min-width: 0;
		max-width: 35rem;
		padding: 2.25rem clamp(1.25rem, 4vw, 2.5rem);
		container-type: inline-size;
		text-align: center;
	}

	h1 {
		margin: 1.5rem 0 0;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: clamp(2.5rem, 22cqi, 7.25rem);
		line-height: 0.92;
		letter-spacing: 0.01em;
	}

	h1 span {
		color: var(--color-gold);
	}

	.intro {
		max-width: 27.5rem;
		margin: 1.5rem auto 3rem;
		color: var(--color-text-secondary);
		font-size: 1.25rem;
		line-height: 1.65;
	}

	.about-link {
		display: block;
		width: fit-content;
		margin: 0.75rem auto 0;
		padding: 0.625rem 0;
		color: var(--color-gold);
		font-family: var(--font-ui);
		text-decoration: none;
	}

	.about-link:hover {
		color: var(--color-link-hover);
	}

	.block {
		margin-bottom: 2.5rem;
		padding: 2rem clamp(1.25rem, 3vw, 2.25rem) 2.25rem;
		text-align: center;
	}

	h2 {
		margin: 1rem 0 1.5rem;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: clamp(2.25rem, 5vw, 3.25rem);
		line-height: 1.1;
	}

	.classes {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(11rem, 1fr));
		gap: 1.25rem;
	}

	.class-card {
		display: flex;
		flex-direction: column;
		align-items: center;
		gap: 0.75rem;
	}

	.emblem {
		display: grid;
		place-items: center;
		width: 3.5rem;
		height: 3.5rem;
		border: var(--border-width) solid var(--color-outline);
		border-radius: var(--radius-card);
	}

	.emblem img {
		image-rendering: pixelated;
	}

	h3 {
		margin: 0;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 2rem;
	}

	.class-card p {
		margin: 0;
		color: var(--color-parchment-text-muted);
		line-height: 1.55;
	}

	.modes {
		display: grid;
		grid-template-columns: repeat(auto-fit, minmax(13.75rem, 1fr));
		gap: 1.25rem;
	}

	.soon-tag {
		display: block;
		margin-top: 0.875rem;
	}

	.mode-name {
		display: block;
		font-family: var(--font-title);
		font-weight: 400;
		font-size: 2.5rem;
		line-height: 1.1;
		color: var(--color-gold);
	}

	.mode p {
		margin: 0.5rem 0 0;
		font-size: 1.125rem;
		color: var(--color-text-secondary);
		line-height: 1.6;
	}

	.mode.soon .mode-name,
	.mode.soon p {
		opacity: 0.6;
	}

	.cta {
		display: flex;
		flex-direction: column;
		align-items: center;
		text-align: center;
		gap: 1.5rem;
		padding: 1rem clamp(0rem, 2vw, 1.5rem);
	}

	.cta h2 {
		margin: 0;
		font-size: clamp(2.25rem, 4.5vw, 2.875rem);
	}

	.cta p {
		margin: 0.5rem 0 0;
		color: var(--color-parchment-text-muted);
		font-size: 1.0625rem;
	}

	.cta-actions {
		display: flex;
		flex-wrap: wrap;
		justify-content: center;
		gap: 0.75rem;
	}
</style>
