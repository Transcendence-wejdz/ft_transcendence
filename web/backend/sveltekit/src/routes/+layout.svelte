<script lang="ts">
	import { page } from '$app/state';
	import '$lib/styles/tokens.css';
	import '@fontsource/pirata-one';
	import '@fontsource/pixelify-sans/400.css';
	import '@fontsource/pixelify-sans/600.css';
	import '@fontsource/pixelify-sans/700.css';
	import '@fontsource/inter/400.css';
	import '@fontsource/inter/500.css';
	import '@fontsource/inter/700.css';


	let { data, children } = $props();
	const HEARTBEAT_INTERVAL_MS = 5_000;
	let onlineAlerts = $state<{ id: string; name: string }[]>([]);

	$effect(() => {
		if (!data.user) return;

		let previousOnline = new Set<string>();
		let hasInitialSnapshot = false;
		let polling = false;

		async function sendHeartbeat() {
			try {
				const response = await fetch('/api/friends-list/presence', {
					method: 'POST'
				});

				if (!response.ok) {
					console.error('Heartbeat request failed.');
				}
			} catch (cause) {
				console.error('Heartbeat network error:', cause);
			}
		}

		void sendHeartbeat();
		const intervalId = setInterval(() => {
			void sendHeartbeat();
		}, HEARTBEAT_INTERVAL_MS);

		async function checkFriends() {
			if (polling) return;
			polling = true;
			try {
				const response = await fetch('/api/friends-list', { cache: 'no-store' });
				if (!response.ok) return;
				const result = await response.json();
				if (!result.success || !Array.isArray(result.friends)) return;

				const onlineFriends = result.friends.filter((friend: { online?: boolean }) => friend.online);
				const currentOnline = new Set<string>(onlineFriends.map((friend: { id: string }) => friend.id));
				if (hasInitialSnapshot) {
					for (const friend of onlineFriends) {
						if (!previousOnline.has(friend.id)) {
							const alert = { id: `${friend.id}-${Date.now()}`, name: friend.displayName };
							onlineAlerts = [...onlineAlerts, alert];
							setTimeout(() => {
								onlineAlerts = onlineAlerts.filter((item) => item.id !== alert.id);
							}, 5_000);
						}
					}
				}
				previousOnline = currentOnline;
				hasInitialSnapshot = true;
			} catch (cause) {
				console.error('Friends presence check failed:', cause);
			} finally {
				polling = false;
			}
		}

		void checkFriends();
		const friendsIntervalId = setInterval(() => void checkFriends(), HEARTBEAT_INTERVAL_MS);

		return () => {
			clearInterval(intervalId);
			clearInterval(friendsIntervalId);
		};
	});

	async function logout() {
		const response = await fetch('/api/logout', {
			method: 'POST'
		});

		if (response.ok) {
			window.location.assign('/login');
		}
	}
</script>

<!-- Full page loads in and out of /game so the Unity instance never outlives its page and other pages refetch fresh data. -->
<div class="app-shell" class:game-route={page.url.pathname === '/game'}
	 data-sveltekit-reload={page.url.pathname === '/game' ? '' : 'off'}>
	<header class="topbar">
		<a class="brand" href="/" aria-label="Transcendence home">
			<span class="brand-mark">T</span>
			<span>Transcendence</span>
		</a>

		<nav class="main-nav" aria-label="Main navigation">
			<a href="/game" data-sveltekit-reload>Play</a>
			<a href="/settings">Settings</a>
			<a href="/project">Project</a>
			<a href="/team">Team</a>
		</nav>

		<div class="account-nav">
			{#if data.user}
				<span class="welcome">Hi, {data.user.display_name}</span>
				<button class="button button-small button-quiet" onclick={logout}>Log out</button>
			{:else}
				<a class="login-link" href="/login">Log in</a>
				<a class="button button-small" href="/register">Sign up</a>
			{/if}
		</div>
	</header>

	<main class="page-content">
		{@render children()}
	</main>
</div>

{#if onlineAlerts.length > 0}
	<aside class="online-alerts" aria-label="Friend activity" aria-live="polite">
		{#each onlineAlerts as alert (alert.id)}
			<div class="online-alert">
				<span class="online-dot" aria-hidden="true"></span>
				<span><strong>{alert.name}</strong> just connected</span>
				<button aria-label="Dismiss notification" onclick={() => onlineAlerts = onlineAlerts.filter((item) => item.id !== alert.id)}>×</button>
			</div>
		{/each}
	</aside>
{/if}

<style>
	:global(*) {
		box-sizing: border-box;
	}

	:global(body) {
		margin: 0;
		min-width: 320px;
		min-height: 100vh;
		color: #edf2ff;
		background:
			radial-gradient(ellipse at 15% 0%, rgba(48, 76, 130, 0.24), transparent 38rem),
			#0c1020;
		font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
	}

	:global(button), :global(input) {
		font: inherit;
	}

	:global(a) {
		color: inherit;
	}

	.app-shell {
		min-height: 100vh;
	}

	.topbar {
		min-height: 76px;
		display: flex;
		align-items: center;
		gap: 2rem;
		padding: 0 clamp(1rem, 5vw, 4.5rem);
		border-bottom: 1px solid rgba(189, 204, 242, 0.1);
		background: rgba(12, 16, 32, 0.78);
		backdrop-filter: blur(14px);
	}

	.brand {
		display: inline-flex;
		align-items: center;
		gap: 0.7rem;
		color: #f4f6ff;
		font-size: 0.98rem;
		font-weight: 750;
		letter-spacing: 0.02em;
		text-decoration: none;
		white-space: nowrap;
	}

	.brand-mark {
		display: grid;
		width: 2rem;
		height: 2rem;
		place-items: center;
		border-radius: 0.7rem;
		color: #161222;
		background: #f5b85c;
		font-weight: 900;
	}

	.main-nav, .account-nav {
		display: flex;
		align-items: center;
		gap: 1.25rem;
	}

	.main-nav {
		margin-left: auto;
	}

	.main-nav a, .login-link {
		color: #aeb8d3;
		font-size: 0.9rem;
		text-decoration: none;
		transition: color 150ms ease;
	}

	.main-nav a:hover, .login-link:hover {
		color: #fff;
	}

	.account-nav {
		gap: 1rem;
	}

	.online-alerts {
		position: fixed;
		z-index: 30;
		top: 1rem;
		right: 1rem;
		display: grid;
		gap: 0.65rem;
		width: min(24rem, calc(100vw - 2rem));
	}

	.online-alert {
		display: flex;
		align-items: center;
		gap: 0.7rem;
		padding: 0.9rem 1rem;
		border: 1px solid rgba(119, 224, 163, 0.35);
		border-radius: 0.85rem;
		color: #eaf7ef;
		background: #182b2a;
		box-shadow: 0 12px 36px rgba(0, 0, 0, 0.35);
		animation: alert-enter 180ms ease-out;
	}

	.online-alert strong { color: #9de5b7; }
	.online-dot { width: 0.55rem; height: 0.55rem; flex: 0 0 auto; border-radius: 50%; background: #69d991; }
	.online-alert button { margin-left: auto; border: 0; color: #c8d6d2; background: transparent; font-size: 1.25rem; cursor: pointer; }
	@keyframes alert-enter { from { opacity: 0; transform: translateY(-0.4rem); } to { opacity: 1; transform: translateY(0); } }

	.welcome {
		color: #aeb8d3;
		font-size: 0.88rem;
	}

	:global(.button) {
		display: inline-flex;
		min-height: 2.8rem;
		align-items: center;
		justify-content: center;
		padding: 0.75rem 1.15rem;
		border: 1px solid #f5b85c;
		border-radius: 0.75rem;
		color: #171321;
		background: #f5b85c;
		font-weight: 750;
		text-decoration: none;
		cursor: pointer;
		transition: transform 150ms ease, background 150ms ease;
	}

	:global(.button:hover) {
		transform: translateY(-1px);
		background: #ffca78;
	}

	:global(.button:disabled) {
		cursor: wait;
		opacity: 0.65;
		transform: none;
	}

	:global(.button-small) {
		min-height: 2.35rem;
		padding: 0.55rem 0.9rem;
		font-size: 0.85rem;
	}

	:global(.button-quiet) {
		border-color: rgba(189, 204, 242, 0.2);
		color: #e4e9fa;
		background: rgba(255, 255, 255, 0.05);
	}

	:global(.page-content) {
		width: min(100% - 2rem, 1100px);
		margin: 0 auto;
		padding: clamp(2rem, 7vh, 5rem) 0;
	}

	.game-route :global(.page-content) {
		flex: 1;
		width: 100%;
		min-height: 0;
		height: auto;
		overflow: hidden;
		padding: 0;
	}

	.game-route {
		display: flex;
		height: 100vh;
		height: 100dvh;
		min-height: 0;
		flex-direction: column;
		overflow: hidden;
	}

	.game-route .topbar {
		position: fixed;
		top: 0;
		right: 0;
		left: 0;
		z-index: 10;
		transform: translateY(-100%);
		transition: transform 200ms ease 300ms;
	}

	.game-route .topbar::after {
		content: '';
		position: absolute;
		right: 0;
		bottom: -12px;
		left: 0;
		height: 12px;
	}

	.game-route .topbar:hover, .game-route .topbar:focus-within {
		transform: translateY(0);
		transition-delay: 0ms;
	}

	@media (max-width: 720px) {
		.topbar {
			flex-wrap: wrap;
			gap: 0.8rem 1rem;
			padding-top: 0.8rem;
			padding-bottom: 0.8rem;
		}

		.main-nav {
			order: 3;
			width: 100%;
			justify-content: space-between;
			gap: 0.5rem;
		}

		.main-nav a {
			font-size: 0.82rem;
		}

		.account-nav {
			margin-left: auto;
		}

	}
</style>
